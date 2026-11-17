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
            captureAt = -1; ready = false;
        }

        public void CreateGUI()
        {
            rootVisualElement.Add(new HelpBox("Solo lectura. F12 captura sin quitar el foco de Game. Mantené izquierdo o derecho y mové el mouse al capturar. La captura diferida permite volver a Game durante 3 segundos. No se escribe en Console ni en archivos.", HelpBoxMessageType.Info));
            rootVisualElement.Add(new Button(() => Arm(0)) { text = "Capturar próxima actualización de input" });
            rootVisualElement.Add(new Button(() => Arm(3)) { text = "Capturar en 3 segundos (volver a Game)" });
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
            if (state == PlayModeStateChange.ExitingPlayMode) { captureAt = -1; ready = false; }
        }

        void AfterInput()
        {
            if (!EditorApplication.isPlaying || EditorApplication.isPaused ||
                InputState.currentUpdateType != InputUpdateType.Dynamic) return;
            bool key = Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame;
            if (!key && (captureAt < 0 || EditorApplication.timeSinceStartup < captureAt)) return;
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
        }

        void Refresh()
        {
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
