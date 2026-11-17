using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UIElements;
using UnityEngine.LowLevel;

namespace ROM.EditorTools
{
    // Observation only: no scene components, cursor writes or input injection.
    public sealed class CiudadDelSolMouseDiagnosticsWindow : EditorWindow
    {
        const BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        TextField output;
        Label status;
        string report = "Sin capturas. Abrí esta ventana antes del ensayo; usá F12 desde Game cuando falle el mouse.";
        double captureAt = -1;
        bool ready;
        string inputSample;
        int inputFrame;
        bool armedRight, tracingRight, finishTrace;
        int startFrame, observedFrames;
        double traceStarted;
        StringBuilder trace;
        string pressInput;
        struct MouseDiagnosticBeforeUpdate { }
        struct MouseDiagnosticAfterUpdate { }
        struct MouseDiagnosticAfterLateUpdate { }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Prueba directa Warrior/Diagnóstico del mouse")]
        static void Open() => GetWindow<CiudadDelSolMouseDiagnosticsWindow>("Mouse · Ciudad del Sol");

        void OnEnable()
        {
            InputSystem.onAfterUpdate += AfterInput;
            EditorApplication.update += Refresh;
            EditorApplication.playModeStateChanged += PlayChanged;
        }

        void OnDisable()
        {
            InputSystem.onAfterUpdate -= AfterInput;
            EditorApplication.update -= Refresh;
            EditorApplication.playModeStateChanged -= PlayChanged;
            StopTrace();
            captureAt = -1; ready = false;
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(new HelpBox("Solo lectura. F12 captura sin quitar el foco de Game. Mantené izquierdo o derecho y mové el mouse al capturar. La captura diferida permite volver a Game durante 3 segundos. No se escribe en Console ni en archivos.", HelpBoxMessageType.Info));
            rootVisualElement.Add(new Button(() => Arm(0)) { text = "Capturar próxima actualización de input" });
            rootVisualElement.Add(new Button(() => Arm(3)) { text = "Capturar en 3 segundos (volver a Game)" });
            rootVisualElement.Add(new Button(ArmRight) { text = "Armar UNA captura automática del próximo clic derecho" });
            rootVisualElement.Add(new Button(StopTrace) { text = "Cancelar captura automática" });
            rootVisualElement.Add(new Button(() => EditorGUIUtility.systemCopyBuffer = report) { text = "Copiar informe" });
            status = new Label("Sin captura pendiente");
            rootVisualElement.Add(status);
            var scroll = new ScrollView(); scroll.style.flexGrow = 1;
            output = new TextField { multiline = true, isReadOnly = true, value = report };
            output.style.whiteSpace = WhiteSpace.Normal;
            scroll.Add(output); rootVisualElement.Add(scroll);
        }

        void Arm(double delay)
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            {
                status.text = "Se requiere Play Mode sin pausa. Esta ventana no lo inicia."; return;
            }
            captureAt = EditorApplication.timeSinceStartup + delay;
            status.text = "Captura solicitada; volvé a Game. También podés usar F12.";
        }

        void PlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) { captureAt = -1; ready = false; StopTrace(); }
        }

        void ArmRight()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused)
            { status.text = "Se requiere Play Mode sin pausa."; return; }
            StopTrace(); captureAt = -1; ready = false;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            if (!InsertHooks(ref loop))
            { status.text = "No se encontró ScriptRunBehaviourUpdate; captura no armada."; return; }
            PlayerLoop.SetPlayerLoop(loop);
            armedRight = true;
            status.text = "Armada: volvé a Game, soltá derecho y presionalo de nuevo moviendo el mouse. Se captura una sola pulsación.";
        }

        void StopTrace()
        {
            armedRight = tracingRight = finishTrace = false;
            var loop = PlayerLoop.GetCurrentPlayerLoop();
            if (RemoveHooks(ref loop)) PlayerLoop.SetPlayerLoop(loop);
        }

        static bool RemoveHooks(ref PlayerLoopSystem loop)
        {
            if (loop.subSystemList == null) return false;
            bool changed = false;
            var children = new List<PlayerLoopSystem>();
            foreach (var child in loop.subSystemList)
            {
                if (child.type == typeof(MouseDiagnosticBeforeUpdate) || child.type == typeof(MouseDiagnosticAfterUpdate) || child.type == typeof(MouseDiagnosticAfterLateUpdate))
                { changed = true; continue; }
                var next = child; changed |= RemoveHooks(ref next); children.Add(next);
            }
            if (changed) loop.subSystemList = children.ToArray();
            return changed;
        }

        bool InsertHooks(ref PlayerLoopSystem loop)
        {
            if (loop.subSystemList == null) return false;
            bool inserted = false;
            var children = new List<PlayerLoopSystem>();
            foreach (var child in loop.subSystemList)
            {
                if (child.type == typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate))
                    children.Add(new PlayerLoopSystem { type = typeof(MouseDiagnosticBeforeUpdate), updateDelegate = BeforeBehaviourUpdate });
                var next = child; inserted |= InsertHooks(ref next); children.Add(next);
                if (child.type == typeof(UnityEngine.PlayerLoop.Update.ScriptRunBehaviourUpdate))
                {
                    children.Add(new PlayerLoopSystem { type = typeof(MouseDiagnosticAfterUpdate), updateDelegate = AfterBehaviourUpdate });
                    inserted = true;
                }
                if (child.type == typeof(UnityEngine.PlayerLoop.PreLateUpdate.ScriptRunBehaviourLateUpdate))
                    children.Add(new PlayerLoopSystem { type = typeof(MouseDiagnosticAfterLateUpdate), updateDelegate = AfterBehaviourLateUpdate });
            }
            loop.subSystemList = children.ToArray(); return inserted;
        }

        void BeforeBehaviourUpdate() => TracePhase("ANTES de MonoBehaviour.Update", false);
        void AfterBehaviourUpdate() => TracePhase("DESPUÉS de MonoBehaviour.Update", true);
        void AfterBehaviourLateUpdate()
        {
            TracePhase("DESPUÉS de LateUpdate", false);
            if (tracingRight && (++observedFrames >= 20 || EditorApplication.timeSinceStartup - traceStarted >= 2)) finishTrace = true;
        }

        void TracePhase(string phaseName, bool explain)
        {
            if (!tracingRight || finishTrace) return;
            try
            {
                var input = RiftMMOController.Instance;
                var mouse = Mouse.current;
                trace.AppendLine($"\n{phaseName}, frame {Time.frameCount} (inicio {startFrame}): delta={mouse?.delta.ReadValue()}; posición={mouse?.position.ReadValue()}; derecho={mouse?.rightButton.isPressed}; foco={Application.isFocused}");
                trace.AppendLine($"Available={input && input.Available}; Steer={ (input ? PrivateValue(input, "steer") : null)}; Dragging={input && input.Dragging}; readFrame={(input ? PrivateValue(input, "readFrame") : null)}; ventana={focusedWindow?.GetType().Name}");
                var cachedFrame = typeof(RiftMMOController).GetField("pointerFrame", PrivateStatic)?.GetValue(null);
                var cachedValue = typeof(RiftMMOController).GetField("pointerBlocked", PrivateStatic)?.GetValue(null);
                var pointer = typeof(RiftMMOController).GetField("pointerData", PrivateStatic)?.GetValue(null) as PointerEventData;
                var sourceSystem = typeof(RiftMMOController).GetField("pointerSystem", PrivateStatic)?.GetValue(null) as EventSystem;
                trace.AppendLine($"ORIGINAL almacenado: PointerOnUI={cachedValue}; frame={cachedFrame}; posición usada={pointer?.position}; EventSystem={Path(sourceSystem ? sourceSystem.gameObject : null)}");
                var originalHits = typeof(RiftMMOController).GetField("uiHits", PrivateStatic)?.GetValue(null) as List<RaycastResult>;
                if (originalHits != null)
                {
                    trace.AppendLine($"Raycast ORIGINAL: {originalHits.Count} resultados");
                    foreach (var hit in originalHits)
                        trace.AppendLine($"  ORIGINAL {Path(hit.gameObject)}; bloquea={BlocksMMO(hit.gameObject)}; depth={hit.depth}");
                }
                // This does not call PointerOnUI: its cache and original hit list stay untouched.
                AppendUI(trace);
                var camera = GameplayCamera._instance;
                var actor = ClientFrontend.OwnedCharacterInstance;
                trace.AppendLine($"Cámara={Path(camera ? camera.gameObject : null)}; activa={camera && camera.isActiveAndEnabled}; actor MMO coincide={input && actor && input.Actor == actor}; observado coincide={actor && ClientFrontend.ObservedCharacterInstance == actor}");
                if (explain && Time.frameCount == startFrame)
                {
                    trace.AppendLine("DECISIÓN OBSERVADA / condiciones del código:");
                    bool processed = input && Equals(PrivateValue(input, "readFrame"), Time.frameCount);
                    if (!processed) trace.AppendLine("  El controlador no procesó este frame: revisar enabled, Actor y callbacks; no atribuirlo a UI.");
                    else if (input.Dragging) trace.AppendLine("  El arrastre sí comenzó; investigar delta, cámara y sobrescrituras posteriores.");
                    else
                    {
                        trace.AppendLine("  No comenzó el arrastre. Comparar las condiciones capturadas ANTES y DESPUÉS; no se ejecuta Pressed/Held desde este diagnóstico.");
                        if (!input.Available) trace.AppendLine("  Available=false: puerta de entrada cerrada.");
                        if (Equals(cachedFrame, Time.frameCount) && Equals(cachedValue, true)) trace.AppendLine("  UI=true en la caché del frame procesado: bloquea la condición Pressed(Steer) && !ui; ver ORIGINAL arriba.");
                        var player = ClientFrontend.ClientPlayerInstance;
                        var combat = player ? player.GetComponent<MultiFPS.Gameplay.RiftCombat.AbilityController>() : null;
                        if (combat && combat.GroundAiming) trace.AppendLine("  GroundAiming=true: fuerza ui=true y limpia steer/orbit.");
                        trace.AppendLine($"  Rebinding={RiftControls.Rebinding}; texto={RiftControls.TextEntryFocused}; capturedPath={typeof(RiftControls).GetField("capturedPath", PrivateStatic)?.GetValue(null)}; modificadores={RiftControls.CurrentModifiers}. Acciones y bindings en PULSACIÓN.");
                    }
                }
            }
            catch (Exception error) { trace.AppendLine("Observación incompleta: " + error); finishTrace = true; }
        }

        static bool BlocksMMO(GameObject go) => go && (go.GetComponentInParent<UnityEngine.UI.Selectable>() || go.GetComponentInParent<UnityEngine.UI.ScrollRect>() || go.GetComponent<UnityEngine.UI.Graphic>());

        void AfterInput()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused ||
                InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            bool key = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
            bool rightStart = armedRight && Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
            if (!rightStart && (tracingRight || (!key && (captureAt < 0 || EditorApplication.timeSinceStartup < captureAt)))) return;
            captureAt = -1;
            var mouse = Mouse.current;
            inputFrame = Time.frameCount;
            var text = new StringBuilder();
            text.AppendLine($"Input System Dynamic, frame {inputFrame}, {DateTime.Now:O}");
            text.AppendLine($"Foco aplicación={Application.isFocused}; ventana Editor enfocada={focusedWindow?.GetType().Name ?? "ninguna"}");
            text.AppendLine($"Mouse presente={mouse != null}; enabled={mouse?.enabled}; delta={mouse?.delta.ReadValue()}; posición={mouse?.position.ReadValue()}");
            if (mouse != null)
            {
                text.AppendLine($"Izquierdo: pulsación={mouse.leftButton.wasPressedThisFrame}, sostenido={mouse.leftButton.isPressed}, liberación={mouse.leftButton.wasReleasedThisFrame}");
                text.AppendLine($"Derecho: pulsación={mouse.rightButton.wasPressedThisFrame}, sostenido={mouse.rightButton.isPressed}, liberación={mouse.rightButton.wasReleasedThisFrame}");
            }
            // Inspect already-created actions; never call Pressed/Held, which have input bookkeeping side effects.
            foreach (string id in new[] { "Orbit", "Steer", "Select" })
            {
                var entries = typeof(RiftControls).GetField("byId", PrivateStatic)?.GetValue(null) as System.Collections.IDictionary;
                var entry = entries != null && entries.Contains(id) ? entries[id] as RiftControls.Entry : null;
                var actions = entry == null ? null : typeof(RiftControls.Entry).GetField("Actions", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(entry) as InputAction[];
                text.AppendLine($"Acción {id} (sin consumir):");
                if (actions == null) { text.AppendLine("  No inicializada / no disponible."); continue; }
                foreach (var action in actions)
                    if (action != null) text.AppendLine($"  {action.name}: enabled={action.enabled}; pulsación={action.WasPressedThisFrame()}; sostenido={action.IsPressed()}; controles={action.controls.Count}");
                foreach (var binding in entry.Bindings) text.AppendLine($"  Binding={binding.Path}; modificadores={binding.Mods}");
            }
            text.AppendLine($"Rebinding={RiftControls.Rebinding}; texto enfocado={RiftControls.TextEntryFocused}; modificadores actuales={RiftControls.CurrentModifiers}");
            inputSample = text.ToString(); ready = true;
            if (rightStart)
            {
                armedRight = false; tracingRight = true; ready = false;
                startFrame = inputFrame; observedFrames = 0; traceStarted = EditorApplication.timeSinceStartup;
                pressInput = inputSample;
                trace = new StringBuilder("CAPTURA ÚNICA DE PULSACIÓN DERECHA\n" + pressInput);
                trace.AppendLine("Fases observacionales alrededor de Update/LateUpdate: no interceptan ReadCharacter ni alteran sus decisiones. ORIGINAL es su caché real, con frame; el independiente se ejecuta inmediatamente al lado de cada observación, no dentro del getter original.");
                TracePhase("PULSACIÓN: InputSystem.onAfterUpdate", false);
            }
        }

        void Refresh()
        {
            if (tracingRight && (finishTrace || !EditorApplication.isPlaying || EditorApplication.timeSinceStartup - traceStarted > 3))
            {
                report = trace.ToString(); StopTrace();
                output?.SetValueWithoutNotify(report);
                if (status != null) status.text = "Captura automática completa: pulsación + hasta 20 frames. Copiá el informe.";
                return;
            }
            if (!ready) return;
            ready = false;
            try { report = BuildReport(); }
            catch (Exception error) { report = inputSample + "\nCaptura incompleta: " + error; }
            output?.SetValueWithoutNotify(report);
            if (status != null) status.text = "Captura conservada. F12 crea otra; Copiar informe permite compararlas.";
        }

        string BuildReport()
        {
            var text = new StringBuilder(inputSample);
            text.AppendLine($"\nESTADO DEL JUEGO, frame {Time.frameCount} (input solicitado en {inputFrame}; las fases pueden diferir)");
            var actor = ClientFrontend.OwnedCharacterInstance;
            var observed = ClientFrontend.ObservedCharacterInstance;
            var controller = RiftMMOController.Instance;
            var camera = GameplayCamera._instance;
            text.AppendLine($"Escena activa={UnityEngine.SceneManagement.SceneManager.GetActiveScene().path}");
            text.AppendLine($"Host: servidor={NetworkServer.active}; cliente={NetworkClient.active}; ready={NetworkClient.ready}");
            text.AppendLine($"Warrior local: {Path(actor ? actor.gameObject : null)}; owned={actor?.isOwned}; servidor={actor?.isServer}; HP={actor?.Health?.CurrentHealth}");
            text.AppendLine($"Warrior observado: {Path(observed ? observed.gameObject : null)}; coincide con local={actor && observed == actor}");
            text.AppendLine($"Controlador MMO: {Path(controller ? controller.gameObject : null)}; enabled={controller && controller.isActiveAndEnabled}");
            if (controller)
            {
                text.AppendLine($"Available={controller.Available}; Dragging={controller.Dragging}; Actor coincide={actor && controller.Actor == actor}");
                text.AppendLine($"Orbit interno={PrivateValue(controller, "orbit")}; Steer interno={PrivateValue(controller, "steer")}");
                text.AppendLine($"Yaw={controller.CameraYaw}; Pitch={controller.CameraPitch}; distancia={controller.CameraDistance}");
            }
            text.AppendLine($"PointerOnUI cache usado por MMO={typeof(RiftMMOController).GetField("pointerBlocked", PrivateStatic)?.GetValue(null)}; frame cache={typeof(RiftMMOController).GetField("pointerFrame", PrivateStatic)?.GetValue(null)}");
            text.AppendLine($"Pause={ClientFrontend.Pause}; Hub={ClientFrontend.Hub}; GamePlayInput={UICursorManager.GamePlayInput()}");
            text.AppendLine($"Cursor={UnityEngine.Cursor.lockState}; visible={UnityEngine.Cursor.visible}; sensibilidad={UserSettings.MouseSensitivity}");
            var player = ClientFrontend.ClientPlayerInstance;
            var combat = player ? player.GetComponent<MultiFPS.Gameplay.RiftCombat.AbilityController>() : null;
            text.AppendLine($"Combate vinculado={combat && combat.Actor == actor}; GroundAiming={combat && combat.GroundAiming}");
            text.AppendLine($"Cámara jugable: {Path(camera ? camera.gameObject : null)}; enabled={camera && camera.isActiveAndEnabled}");
            if (camera)
            {
                var third = camera.GetComponent<RiftThirdPersonCamera>();
                var render = camera.GetComponent<Camera>();
                text.AppendLine($"Camera render enabled={render && render.enabled}; ThirdPerson activo={third && third.isActiveAndEnabled}; conversación={third && third.ConversationActive}; retorno={third && third.ConversationReturning}");
                text.AppendLine($"Target de cámara del Warrior={Path(actor && actor.FPPCameraTarget ? actor.FPPCameraTarget.gameObject : null)}");
            }
            text.AppendLine($"Instancias activas: MMO={UnityEngine.Object.FindObjectsByType<RiftMMOController>(FindObjectsSortMode.None).Length}; GameplayCamera={UnityEngine.Object.FindObjectsByType<GameplayCamera>(FindObjectsSortMode.None).Length}");
            AppendUI(text);
            text.AppendLine("\nInterpretación: delta cero en una captura NO prueba fallo; repetí mientras movés el mouse. Las acciones de arriba son las acciones reales, antes del filtro de UI/disponibilidad. PointerOnUI cache es el último valor calculado por MMO; la lista siguiente es un raycast independiente y no modifica su caché.");
            return text.ToString();
        }

        static object PrivateValue(object owner, string name) => owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(owner);

        static void AppendUI(StringBuilder text)
        {
            var events = EventSystem.current;
            text.AppendLine($"\nUI: EventSystem={Path(events ? events.gameObject : null)}; módulo={events?.currentInputModule?.GetType().Name}; seleccionado={Path(events ? events.currentSelectedGameObject : null)}");
            if (!events || Mouse.current == null) return;
            var hits = new List<RaycastResult>();
            events.RaycastAll(new PointerEventData(events) { position = Mouse.current.position.ReadValue() }, hits);
            text.AppendLine($"Raycasts bajo el puntero: {hits.Count} (orden del EventSystem)");
            bool found = false;
            foreach (var hit in hits)
            {
                var go = hit.gameObject;
                if (!go) continue;
                bool blocking = go.GetComponentInParent<UnityEngine.UI.Selectable>() || go.GetComponentInParent<UnityEngine.UI.ScrollRect>() || go.GetComponent<UnityEngine.UI.Graphic>();
                string label = blocking && !found ? "PRIMER BLOQUEANTE SEGÚN MMO" : blocking ? "bloqueante" : "no bloquea según MMO";
                found |= blocking;
                text.AppendLine($"  [{label}] {Path(go)}; módulo={hit.module?.GetType().Name}; depth={hit.depth}");
                var graphic = go.GetComponent<UnityEngine.UI.Graphic>();
                if (graphic) text.AppendLine($"    Graphic={graphic.GetType().Name}; raycastTarget={graphic.raycastTarget}; alpha={graphic.color.a}");
                foreach (var group in go.GetComponentsInParent<CanvasGroup>(true)) text.AppendLine($"    CanvasGroup {Path(group.gameObject)}: alpha={group.alpha}; blocksRaycasts={group.blocksRaycasts}; interactable={group.interactable}");
            }
            if (!found) text.AppendLine("Ningún objeto intercepta el puntero según la regla actual de RiftMMOController.");
        }

        static string Path(GameObject go)
        {
            if (!go) return "<ninguno>";
            string path = go.name;
            for (var parent = go.transform.parent; parent; parent = parent.parent) path = parent.name + "/" + path;
            return $"{go.scene.name}:/{path} [id {go.GetInstanceID()}]";
        }
    }
}
