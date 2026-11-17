using System;
using System.Reflection;
using System.Text;
using MultiFPS;
using ROM.CiudadDelSol;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace ROM.EditorTools
{
    internal sealed partial class CiudadDelSolNavMeshDiagnostics
    {
        sealed class RouteSnapshot
        {
            internal string Name, Detail;
            internal Vector3 From, To;
            internal Vector3[] Corners = Array.Empty<Vector3>();
            internal readonly System.Collections.Generic.List<DiagnosticStep> Steps = new System.Collections.Generic.List<DiagnosticStep>();
            internal bool OfficialResult;
            internal Vector3[] CheckedLine = Array.Empty<Vector3>();
        }

        sealed class DiagnosticStep
        {
            internal Vector3 From, To;
            internal bool Valid, HasEvidence, HasEdgeFrom, HasEdgeTo, ForwardHit, ReverseHit;
            internal NavMeshHit EdgeFrom, EdgeTo, HitFrom, HitTo;
            internal Collider Support;
            internal Vector3 FloorPoint, FloorNormal;
            internal string Detail;
        }

        readonly System.Collections.Generic.List<RouteSnapshot> encounterRoutes = new System.Collections.Generic.List<RouteSnapshot>();
        readonly string[] encounterIds = { "portico-sureste", "archivos-suroeste" };
        DropdownField encounterChoice, routeChoice, stepChoice;
        TextField encounterText;
        Label captureStatus;
        Toggle showEncounter;
        Scene snapshotScene;
        bool snapshotPlayMode;
        int captureGeneration;

        static MethodInfo CheckMethod(string name) => typeof(CiudadDelSolEncounterValidation)
            .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic) ?? throw new MissingMethodException(name);

        static bool Check(string name, object[] args)
        {
            try { return (bool)CheckMethod(name).Invoke(null, args); }
            catch (TargetInvocationException exception) { throw exception.InnerException ?? exception; }
        }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Población/Diagnosticar las dos rutas bloqueadas")]
        static void OpenEncounterDiagnostics() => GetWindow<CiudadDelSolNavMeshDiagnostics>("Cortes de NavMesh");

        void ClearEncounterCapture(PlayModeStateChange state)
        {
            captureGeneration++;
            encounterRoutes.Clear();
            ShowRouteSnapshot();
            if (captureStatus != null) captureStatus.text = "Cambio de Play Mode: captura descartada. Volvé a capturar manualmente.";
        }

        void AddEncounterDiagnostics(VisualElement parent)
        {
            var panel = new Foldout { text = "Población: dos rutas bloqueadas (captura manual)", value = true };
            panel.Add(new Label("Solo lectura. No hornea, no crea actores ni aprueba encuentros. Verde = paso NavMesh comprobado; rojo = rechazo; gris = sin evaluar.")
                { style = { whiteSpace = WhiteSpace.Normal } });
            encounterChoice = new DropdownField("Encuentro", new System.Collections.Generic.List<string>(encounterIds), 0);
            panel.Add(encounterChoice);
            var buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            buttons.Add(new Button(CaptureEncounter) { text = "Capturar ida y vuelta" });
            buttons.Add(new Button(() => { captureGeneration++; encounterRoutes.Clear(); ShowRouteSnapshot(); SceneView.RepaintAll(); }) { text = "Limpiar / cancelar" });
            buttons.Add(new Button(() => { EditorGUIUtility.systemCopyBuffer = SnapshotReport(); }) { text = "Copiar informe completo" });
            panel.Add(buttons);
            showEncounter = new Toggle("Dibujar captura en Scene") { value = true };
            showEncounter.RegisterValueChangedCallback(_ => SceneView.RepaintAll());
            panel.Add(showEncounter);
            captureStatus = new Label("Sin captura. La escena debe estar cargada con su NavMesh disponible.") { style = { whiteSpace = WhiteSpace.Normal } };
            panel.Add(captureStatus);
            routeChoice = new DropdownField("Ruta", new System.Collections.Generic.List<string>(), -1);
            routeChoice.RegisterValueChangedCallback(_ => ShowRouteSnapshot());
            panel.Add(routeChoice);
            stepChoice = new DropdownField("Muestra", new System.Collections.Generic.List<string>(), -1);
            stepChoice.RegisterValueChangedCallback(_ => ShowStepSnapshot());
            panel.Add(stepChoice);
            buttons = new VisualElement { style = { flexDirection = FlexDirection.Row } };
            buttons.Add(new Button(() => { var step = SelectedStep; if (step != null) Focus(step.From); else if (SelectedRoute != null) Focus(SelectedRoute.From); }) { text = "Enfocar muestra / origen" });
            buttons.Add(new Button(() => { if (SelectedRoute != null) Focus(SelectedRoute.To); }) { text = "Enfocar destino" });
            buttons.Add(new Button(() => { var support = SelectedStep?.Support; if (SnapshotCurrent && support) { Selection.activeGameObject = support.gameObject; EditorGUIUtility.PingObject(support); } }) { text = "Seleccionar apoyo" });
            panel.Add(buttons);
            encounterText = new TextField { multiline = true, isReadOnly = true, style = { height = 220 } };
            panel.Add(encounterText);
            parent.Add(panel);
        }

        RouteSnapshot SelectedRoute => routeChoice != null && routeChoice.index >= 0 && routeChoice.index < encounterRoutes.Count ? encounterRoutes[routeChoice.index] : null;
        DiagnosticStep SelectedStep => SelectedRoute != null && stepChoice != null && stepChoice.index >= 0 && stepChoice.index < SelectedRoute.Steps.Count ? SelectedRoute.Steps[stepChoice.index] : null;
        bool SnapshotCurrent => snapshotScene.IsValid() && snapshotScene.isLoaded && snapshotPlayMode == EditorApplication.isPlaying;

        void CaptureEncounter()
        {
            captureGeneration++;
            encounterRoutes.Clear();
            ShowRouteSnapshot();
            try
            {
                var scene = SceneManager.GetSceneByPath(CiudadDelSolPopulationDefinition.ScenePath);
                if (!scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("Ciudad del Sol no está cargada. La herramienta no carga escenas ni inicia Play.");
                var config = AssetDatabase.LoadAssetAtPath<CiudadDelSolPopulationDefinition>("Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset");
                if (!config) throw new InvalidOperationException("No se encuentra la configuración exterior original.");
                var posts = Array.FindAll(config.Posts, post => post.EncounterId == encounterChoice.value);
                if (posts.Length != 2) throw new InvalidOperationException("Se esperaban exactamente dos puestos del encuentro; no se diagnostica otra población.");
                snapshotScene = scene; snapshotPlayMode = EditorApplication.isPlaying;
                int generation = captureGeneration, next = 0;
                void CaptureNext()
                {
                    if (!this || generation != captureGeneration || !SnapshotCurrent) return;
                    try
                    {
                        var post = posts[next / 2]; bool reverse = next % 2 != 0;
                        var captured = CaptureRoute(scene, config, post, reverse);
                        var checkedPoints = new System.Collections.Generic.List<Vector3>();
                        if (captured.Steps.Count > 0) checkedPoints.Add(captured.Steps[0].From);
                        foreach (var step in captured.Steps) { if (!step.Valid) break; checkedPoints.Add(step.To); }
                        captured.CheckedLine = checkedPoints.ToArray();
                        encounterRoutes.Add(captured);
                        next++;
                        captureStatus.text = $"Captura manual {next}/4. Estado de {(snapshotPlayMode ? "Play Mode" : "Edit Mode")}; sin modificar aprobaciones.";
                        routeChoice.choices = encounterRoutes.ConvertAll(item => item.Name);
                        routeChoice.SetValueWithoutNotify(encounterRoutes[encounterRoutes.Count - 1].Name);
                        ShowRouteSnapshot();
                        if (next < 4) EditorApplication.delayCall += CaptureNext;
                    }
                    catch (Exception exception) { captureStatus.text = "Captura detenida: " + exception.Message; }
                }
                EditorApplication.delayCall += CaptureNext;
            }
            catch (Exception exception) { captureStatus.text = "No se capturó: " + exception.Message; }
        }

        static RouteSnapshot CaptureRoute(Scene scene, CiudadDelSolPopulationDefinition config, CiudadEnemyPost post, bool reverse)
        {
            var result = new RouteSnapshot { Name = post.Id + (reverse ? " · regreso" : " · ida") };
            if (!post.Enemy || !post.Enemy.Prefab) throw new InvalidOperationException("Puesto sin prefab nativo: " + post.Id);
            var agent = post.Enemy.Prefab.GetComponent<NavMeshAgent>();
            if (!agent) throw new InvalidOperationException("Prefab sin NavMeshAgent: " + post.Id);
            var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
            var reference = CiudadDelSolSpawnSafety.Resolve(scene, post);
            if (!CiudadDelSolSpawnSafety.Placement(scene, post, reference, out var spawn, out var placementIssue, null, true))
            { result.Detail = "Placement original: " + placementIssue; return result; }
            object[] project = { scene, config, config.AccessibilityOrigin, filter, null, null };
            if (!Check("Project", project)) { result.Detail = "Project original: " + project[5]; return result; }
            var entrance = (Vector3)project[4];
            result.From = reverse ? spawn : entrance; result.To = reverse ? entrance : spawn;
            object[] route = { scene, config, post, result.From, result.To, false, null };
            result.OfficialResult = Check("Route", route);
            var path = new NavMeshPath();
            bool calculated = NavMesh.CalculatePath(result.From, result.To, filter, path);
            result.Corners = path.corners;
            result.Detail = $"{result.Name}\nCaptura UTC={DateTime.UtcNow:O}; escena={scene.path}; agente={agent.agentTypeID}, areaMask={agent.areaMask}\n" +
                $"Route ORIGINAL={result.OfficialResult}: {route[6]}\nCalculatePath={calculated}; estado={path.status}; esquinas={result.Corners.Length}\n" +
                $"Origen={result.From:F6}; destino={result.To:F6}\n" +
                "No es una aprobación del encuentro. El trazado gris no ha sido evaluado; verde solo acredita el paso local del NavMesh, no toda la física.\n";
            for (int k = 0; k < result.Corners.Length; k++) result.Detail += $"Esquina {k}: {result.Corners[k]:F6}\n";
            if (!calculated || path.status != NavMeshPathStatus.PathComplete || result.Corners.Length < 2) return result;
            var settings = NavMesh.GetSettingsByID(agent.agentTypeID);
            object[] initialProject = { scene, config, result.From, filter, null, null };
            if (!Check("Project", initialProject)) { result.Detail += "Inicio del muestreo: " + initialProject[5]; return result; }
            var previous = (Vector3)initialProject[4];
            for (int i = 1; i < result.Corners.Length; i++)
            {
                var a = result.Corners[i - 1]; var b = result.Corners[i];
                int count = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(new Vector2(a.x, a.z), new Vector2(b.x, b.z)) / .1f));
                for (int j = 1; j <= count; j++)
                {
                    var chord = Vector3.Lerp(a, b, (float)j / count);
                    var requested = new Vector3(chord.x, previous.y, chord.z);
                    float run = Vector2.Distance(new Vector2(previous.x, previous.z), new Vector2(requested.x, requested.z));
                    float reach = Mathf.Sqrt(settings.agentClimb * settings.agentClimb + run * run) + .03f;
                    bool sampled = NavMesh.SamplePosition(requested, out var sample, reach, filter);
                    var p = sampled ? sample.position : requested;
                    bool projected = sampled && Vector2.Distance(new Vector2(p.x, p.z), new Vector2(requested.x, requested.z)) <= .025f &&
                        Mathf.Abs(p.y - previous.y) <= settings.agentClimb + .001f;
                    object[] connected = { previous, p, a, b, filter, false, false, null };
                    bool valid = projected && Check("ConnectedStep", connected);
                    var step = new DiagnosticStep { From = previous, To = p, Valid = valid };
                    // Capture detailed evidence only around failures and original corners, not thousands of strings per route.
                    bool nearCorner = Vector3.Distance(previous, a) <= .001f || j == count;
                    if (!valid || nearCorner)
                    {
                        step.HasEvidence = true;
                        if (scene.GetPhysicsScene().Raycast(previous + Vector3.up * (settings.agentClimb + .03f), Vector3.down, out var surface, 2 * (settings.agentClimb + .03f), GameManager.environmentLayer, QueryTriggerInteraction.Ignore))
                        { step.Support = surface.collider; step.FloorPoint = surface.point; step.FloorNormal = surface.normal; }
                        step.HasEdgeFrom = NavMesh.FindClosestEdge(previous, out step.EdgeFrom, filter);
                        step.HasEdgeTo = NavMesh.FindClosestEdge(p, out step.EdgeTo, filter);
                        step.ForwardHit = NavMesh.Raycast(previous, p, out step.HitFrom, filter);
                        step.ReverseHit = NavMesh.Raycast(p, previous, out step.HitTo, filter);
                        step.Detail = $"Tramo {i}/{result.Corners.Length - 1}, muestra {j}/{count}\nConsulta={requested:F6}, radio={reach:F6}, SamplePosition={sampled}, proyectada={p:F6}, proyección compatible={projected}\n" +
                            $"ConnectedStep ORIGINAL={valid}; esquina={connected[5]}, pruebaPolígono={connected[6]}: {connected[7]}\n" +
                            RayEvidence(previous, p, filter, "Ida") + RayEvidence(p, previous, filter, "Vuelta") +
                            EdgeEvidence(previous, filter, "Origen") + EdgeEvidence(p, filter, "Destino") +
                            PhysicalEvidence(scene, post, previous, settings) + PhysicalEvidence(scene, post, Vector3.Lerp(previous, p, .5f), settings) + PhysicalEvidence(scene, post, p, settings);
                    }
                    result.Steps.Add(step);
                    if (!valid) return result;
                    previous = p;
                }
            }
            return result;
        }

        static string RayEvidence(Vector3 from, Vector3 to, NavMeshQueryFilter filter, string label)
        {
            bool blocked = NavMesh.Raycast(from, to, out var hit, filter);
            return $"Raycast {label}: impacto={blocked}; punto={hit.position:F6}; máscara={hit.mask}; normal={hit.normal:F6}; distanciaOrigen={Vector3.Distance(from, hit.position):F6}; distanciaDestino={Vector3.Distance(to, hit.position):F6}\n";
        }

        static string EdgeEvidence(Vector3 p, NavMeshQueryFilter filter, string label)
        {
            bool found = NavMesh.FindClosestEdge(p, out var edge, filter);
            bool sampled = NavMesh.SamplePosition(p, out var exact, .001f, filter);
            return $"Arista {label}: encontrada={found}; distancia={edge.distance:F6}; punto={edge.position:F6}; normal={edge.normal:F6}; máscara={edge.mask}\n" +
                $"SamplePosition {label} banda original 0.001m: {sampled}; punto={exact.position:F6}; diferencia={Vector3.Distance(p, exact.position):F6}\n";
        }

        static string PhysicalEvidence(Scene scene, CiudadEnemyPost post, Vector3 p, NavMeshBuildSettings settings)
        {
            var physics = scene.GetPhysicsScene();
            if (!physics.Raycast(p + Vector3.up * (settings.agentClimb + .03f), Vector3.down, out var hit,
                2 * (settings.agentClimb + .03f), GameManager.environmentLayer, QueryTriggerInteraction.Ignore)) return $"Física {p:F6}: sin apoyo\n";
            var agent = post.Enemy.Prefab.GetComponent<NavMeshAgent>(); var body = post.Enemy.Prefab.Body;
            var pose = p + Vector3.up * agent.baseOffset;
            float top = pose.y + body.center.y + body.height * .5f + .03f;
            float bottom = Mathf.Min(pose.y + body.center.y - body.height * .5f + settings.agentClimb + .03f, top - body.radius * 2);
            var offset = new Vector3(body.center.x, 0, body.center.z); var hits = new Collider[8];
            int count = physics.OverlapCapsule(new Vector3(pose.x, bottom + body.radius, pose.z) + offset,
                new Vector3(pose.x, top - body.radius, pose.z) + offset, body.radius, hits, GameManager.environmentLayer, QueryTriggerInteraction.Ignore);
            string collisions = "";
            for (int i = 0; i < count; i++) collisions += "\n  " + CiudadDelSolSpawnSafety.ColliderPath(hits[i]);
            string prefab = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(hit.collider.gameObject);
            return $"Física {p:F6}: suelo={hit.point:F6}; diferenciaY={Mathf.Abs(p.y - hit.point.y):F6}; normal={hit.normal:F6}; pendiente={Vector3.Angle(hit.normal, Vector3.up):F4}°\n" +
                CiudadDelSolSpawnSafety.SupportDescription(hit) + $"\nPrefab={prefab}; cápsula radio={body.radius:F3}, altura={body.height:F3}, colisiones={count} (8=saturación){collisions}\n";
        }

        void ShowRouteSnapshot()
        {
            if (stepChoice == null) return;
            var route = SelectedRoute;
            var choices = new System.Collections.Generic.List<string>();
            if (route != null) for (int i = 0; i < route.Steps.Count; i++)
                choices.Add($"{i + 1}: {(route.Steps[i].Valid ? "Nav OK" : "RECHAZO")} {route.Steps[i].From:F3}");
            stepChoice.choices = choices;
            int index = route == null ? -1 : route.Steps.FindIndex(step => !step.Valid);
            if (index < 0 && route != null && route.Steps.Count > 0) index = 0;
            stepChoice.SetValueWithoutNotify(index >= 0 ? stepChoice.choices[index] : null);
            ShowStepSnapshot();
        }

        void ShowStepSnapshot()
        {
            if (encounterText != null) encounterText.SetValueWithoutNotify((SnapshotCurrent ? "" : "CAPTURA AUSENTE O ANTIGUA: recapturar en la sesión actual.\n") +
                (SelectedRoute?.Detail ?? "Sin ruta capturada") + "\n" + (SelectedStep?.Detail ?? "Sin evidencia detallada para esta muestra; seleccioná una esquina o un rechazo."));
            SceneView.RepaintAll();
        }

        string SnapshotReport()
        {
            var text = new StringBuilder("CIUDAD DEL SOL — captura manual de rutas, sin aprobar encuentros\n");
            foreach (var route in encounterRoutes)
            {
                text.AppendLine(route.Detail);
                foreach (var step in route.Steps) if (step.Detail != null) text.AppendLine(step.Detail);
            }
            return text.ToString();
        }

        void DrawEncounterSnapshot()
        {
            if (!SnapshotCurrent || showEncounter == null || !showEncounter.value || Event.current.type != EventType.Repaint) return;
            var route = SelectedRoute; if (route == null) return;
            var saved = Handles.color;
            try
            {
                Handles.color = Color.gray;
                if (route.Corners.Length > 1) Handles.DrawAAPolyLine(2, route.Corners);
                Handles.color = Color.green;
                if (route.CheckedLine.Length > 1) Handles.DrawAAPolyLine(3, route.CheckedLine);
                if (route.Steps.Count > 0 && !route.Steps[route.Steps.Count - 1].Valid)
                {
                    Handles.color = Color.red;
                    var failed = route.Steps[route.Steps.Count - 1];
                    Handles.DrawAAPolyLine(5, failed.From, failed.To);
                }
                Handles.color = Color.cyan;
                Handles.Label(route.From + Vector3.up, "Origen"); Handles.DrawWireDisc(route.From, Vector3.up, .4f);
                Handles.color = Color.yellow;
                Handles.Label(route.To + Vector3.up, "Destino"); Handles.DrawWireDisc(route.To, Vector3.up, .4f);
                for (int i = 0; i < route.Corners.Length; i++)
                {
                    Handles.DrawWireDisc(route.Corners[i], Vector3.up, .15f);
                    Handles.Label(route.Corners[i], "C" + i);
                }
                var selectedStep = SelectedStep;
                if (selectedStep == null) return;
                Handles.color = selectedStep.Valid ? Color.green : Color.red;
                Handles.DrawWireDisc(selectedStep.From, Vector3.up, .45f);
                Handles.DrawWireDisc(selectedStep.To, Vector3.up, .3f);
                Handles.Label(selectedStep.From + Vector3.up * .5f, selectedStep.Valid ? "Muestra NavMesh" : "CONTINUIDAD NO DEMOSTRADA");
                if (!selectedStep.HasEvidence) return;
                if (selectedStep.Support)
                {
                    Handles.color = Color.white;
                    Handles.DrawLine(selectedStep.From, selectedStep.FloorPoint);
                    Handles.DrawLine(selectedStep.FloorPoint, selectedStep.FloorPoint + selectedStep.FloorNormal * .6f);
                    Handles.Label(selectedStep.FloorPoint + Vector3.up * .9f, $"ΔY suelo={Mathf.Abs(selectedStep.From.y - selectedStep.FloorPoint.y):F6}");
                }
                Handles.color = Color.magenta;
                if (selectedStep.HasEdgeFrom) { Handles.DrawWireDisc(selectedStep.EdgeFrom.position, Vector3.up, .2f); Handles.DrawLine(selectedStep.EdgeFrom.position, selectedStep.EdgeFrom.position + selectedStep.EdgeFrom.normal * .7f); }
                if (selectedStep.HasEdgeTo) { Handles.DrawWireDisc(selectedStep.EdgeTo.position, Vector3.up, .2f); Handles.DrawLine(selectedStep.EdgeTo.position, selectedStep.EdgeTo.position + selectedStep.EdgeTo.normal * .7f); }
                Handles.color = new Color(1f, .5f, 0);
                if (selectedStep.ForwardHit) { Handles.DrawWireDisc(selectedStep.HitFrom.position, Vector3.up, .25f); Handles.Label(selectedStep.HitFrom.position + Vector3.up * 1.2f, "Límite ida"); }
                if (selectedStep.ReverseHit) { Handles.DrawWireDisc(selectedStep.HitTo.position, Vector3.up, .3f); Handles.Label(selectedStep.HitTo.position + Vector3.up * 1.5f, "Límite vuelta"); }
            }
            finally { Handles.color = saved; }
        }
    }
}
