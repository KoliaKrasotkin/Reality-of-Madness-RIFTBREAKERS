using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mirror;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

namespace ROM.EditorTools
{
    // Manual original input; automated observation only. No actors, input overrides or teleports.
    public sealed class CiudadDelSolInteriorWarriorWalkWindow : EditorWindow
    {
        const string City = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string NavData = "Assets/Scenes/Dungeon_Isla_del_Sol/NavMesh-NavMesh_Exterior_CiudadDelSol.asset";
        const string Population = "Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset";
        const double SamplePeriod = .1;
        readonly List<PlayerInstance> players = new List<PlayerInstance>();
        readonly RaycastHit[] hits = new RaycastHit[128];
        readonly Collider[] contacts = new Collider[128];
        Vector3[] route;
        Record record;
        Label status;
        bool running, draw = true;
        double nextTick, lastTick, nextSave;
        string output = "", message = "Esperando Play y llegada física a A03.";

        [Serializable] sealed class Participant
        {
            public uint playerNetId, characterNetId;
            public int connection, next = 1, samples, sideContactSamples, supportFailures, navFailures, nearEdgeSamples, stallWarnings;
            public float radius, height, step, skin, distance, minimumEdge = float.MaxValue;
            public bool complete;
            public Vector3 lastPosition;
            public Vector3 initialPosition;
            public double stalledFor;
            public double[] gateAt = { -1, -1, -1, -1 };
        }
        [Serializable] sealed class Event
        {
            public double seconds;
            public uint character;
            public string kind, detail;
            public Vector3 position;
        }
        [Serializable] sealed class Sample
        {
            public double seconds;
            public uint character;
            public Vector3 position, input;
            public int waypoint, collisionFlags;
            public bool grounded, support, nav;
            public bool physicalNotChecked;
            public float edge;
            public string[] contactObjects;
        }
        [Serializable] sealed class Record
        {
            public string startedUTC, endedUTC, result, unity, sceneBefore, sceneAfter, navBefore, navAfter, populationBefore, populationAfter;
            public string sourceCapture, sourceSHA256;
            public int initialCheckpoint;
            public bool playMode, realConnections, controlsUntouched = true, savedScene = false, protectedFilesUnchanged;
            public int requested;
            public double start, elapsed;
            public Vector3[] checkpoints;
            public List<Participant> participants = new List<Participant>();
            public List<Event> events = new List<Event>();
            public List<Sample> samples = new List<Sample>();
            public double[] fourCrossingWindows = { -1, -1, -1, -1 };
        }
        sealed class ResumeSpec
        {
            public string path, hash;
            public int checkpoint;
        }

        [MenuItem("ROM/Dungeon Ciudad del Sol/Interior - Etapa 1/Prueba funcional Warrior (manual asistida)")]
        public static void Open() => GetWindow<CiudadDelSolInteriorWarriorWalkWindow>("A03–R21 · Warrior");

        void OnEnable()
        {
            var north = CiudadDelSolInteriorStageOne.NorthGuide();
            route = north.Concat(north.Reverse().Skip(1)).ToArray();
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload += Reloading;
            SceneView.duringSceneGui += Draw;
        }
        void OnDisable()
        {
            Finish("VENTANA_CERRADA; PRUEBA_INCOMPLETA");
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= PlayChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= Reloading;
            SceneView.duringSceneGui -= Draw;
        }
        void Reloading() => Finish("RECARGA_SCRIPTS; PRUEBA_INCOMPLETA");
        void PlayChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingPlayMode) Finish("SALIDA_PLAY; PRUEBA_INCOMPLETA");
        }
        public void CreateGUI()
        {
            var root = rootVisualElement; root.Clear();
            root.style.paddingLeft = root.style.paddingRight = 10;
            root.Add(new HelpBox("Usá el bootstrap habitual. Llegá caminando a A03 (156, −16, 99), sin teleport. Esta ventana NO mueve Warriors, crea jugadores ni inicia Host/Play. Controlá con WASD en Game.", HelpBoxMessageType.Info));
            root.Add(new Button(() => Begin(1)) { text = "Registrar ida y vuelta · Warrior local real" });
            root.Add(new Button(() => Begin(4)) { text = "Registrar 4 Warriors · conexiones reales" });
            root.Add(new Button(AnalyzeSelected) { text = "Analizar captura existente sin modificarla" });
            root.Add(new Button(ResumeSelected) { text = "Continuar solo desde el último punto acreditado" });
            root.Add(new Button(() => Finish("CANCELADA_POR_USUARIO")) { text = "Finalizar y guardar informe" });
            var toggle = new Toggle("Mostrar guía y puntos en Scene") { value = draw };
            toggle.RegisterValueChangedCallback(e => { draw = e.newValue; SceneView.RepaintAll(); }); root.Add(toggle);
            root.Add(new Button(() => { if (Directory.Exists(output)) EditorUtility.RevealInFinder(Path.GetFullPath(output)); }) { text = "Mostrar informe JSON" });
            status = new Label(); status.style.whiteSpace = WhiteSpace.Normal; root.Add(status); Refresh();
            root.Add(new HelpBox("Cuatro recorridos no prueban por sí solos cruces simultáneos. El informe mide ventanas de paso por las dos bocas; confirmar cámara, contactos y bloqueo con los cuatro clientes. No se aprueba arquitectura automáticamente.", HelpBoxMessageType.Warning));
            root.Add(new HelpBox("El Host de desarrollo actual admite solo un jugador y no escucha clientes remotos. La prueba de cuatro exige otra sesión real ya habilitada; esta herramienta no cambia esos límites ni configura Mirror.", HelpBoxMessageType.Info));
        }

        void Begin(int count, ResumeSpec resume = null)
        {
            try
            {
                if (running) throw new InvalidOperationException("Ya hay una recepción activa; finalizala primero.");
                if (!EditorApplication.isPlaying || !NetworkServer.active || !NetworkClient.active)
                    throw new InvalidOperationException("Abrí Play con el Host original de desarrollo. La herramienta no lo inicia.");
                if (count == 4 && !NetworkServer.listen)
                    throw new InvalidOperationException("El Host no escucha clientes remotos (NetworkServer.listen=false). Se conserva el bootstrap: no es posible validar cuatro conexiones con esta sesión local.");
                players.Clear();
                foreach (var identity in NetworkServer.spawned.Values)
                {
                    var p = identity ? identity.GetComponent<PlayerInstance>() : null;
                    if (p && !p.BOT && p.connectionToClient != null && p.connectionToClient.isAuthenticated &&
                        p.connectionToClient.identity == p.netIdentity && p.MyCharacter && p.MyCharacter.GetComponent<WarriorAvatar>() &&
                        p.MyCharacter.Health && p.MyCharacter.Health.CurrentHealth > 0 && p.MyCharacter.gameObject.scene.path == City)
                        players.Add(p);
                }
                if (count == 1) players.RemoveAll(p => !p.isLocalPlayer);
                if (players.Count != count || players.Select(p => p.connectionToClient.connectionId).Distinct().Count() != count)
                    throw new InvalidOperationException("Se necesitan exactamente " + count + " Warriors vivos de conexiones humanas distintas en Ciudad del Sol. No se crean clones ni bots.");
                var anchor = route[resume == null ? 0 : resume.checkpoint];
                // Same strict 1 m 3D criterion as every checkpoint, including the first.
                float startRadius = 1f;
                if (players.Any(p => Vector3.Distance(p.MyCharacter.transform.position, anchor) >= startRadius))
                    throw new InvalidOperationException("Llegá caminando a " + anchor + ", a menos de " + startRadius + " m. No se mueve automáticamente al personaje.");
                if (players.Any(p => !p.MyCharacter.GetComponent<CharacterController>() || !p.MyCharacter.GetComponent<CharacterController>().enabled))
                    throw new InvalidOperationException("Falta un CharacterController real habilitado.");
                record = new Record { requested = count, realConnections = true, playMode = true, unity = Application.unityVersion,
                    startedUTC = DateTime.UtcNow.ToString("o"), start = EditorApplication.timeSinceStartup, checkpoints = route,
                    result = "REGISTRO_EN_CURSO; NO_COMPLETO", initialCheckpoint = resume == null ? 0 : resume.checkpoint,
                    sourceCapture = resume == null ? "" : resume.path, sourceSHA256 = resume == null ? "" : resume.hash,
                    sceneBefore = Hash(City), navBefore = Hash(NavData), populationBefore = Hash(Population) };
                foreach (var p in players)
                {
                    var cc = p.MyCharacter.GetComponent<CharacterController>();
                    record.participants.Add(new Participant { playerNetId = p.netId, characterNetId = p.MyCharacter.netId,
                        connection = p.connectionToClient.connectionId, lastPosition = p.MyCharacter.transform.position, initialPosition = p.MyCharacter.transform.position,
                        next = resume == null ? 1 : resume.checkpoint + 1,
                        radius = cc.radius, height = cc.height, step = cc.stepOffset, skin = cc.skinWidth });
                }
                output = Path.Combine("Backups/CiudadDelSol/Interior", "WarriorWalk_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
                Directory.CreateDirectory(output); nextTick = lastTick = EditorApplication.timeSinceStartup; nextSave = nextTick + 30;
                running = true; message = "Registro activo. Volvé a Game y seguí la guía. Máximo 15 minutos."; Refresh(); Save();
            }
            catch (Exception e) { message = e.Message; Refresh(); }
        }

        void Tick()
        {
            if (!running || EditorApplication.timeSinceStartup < nextTick) return;
            double now = EditorApplication.timeSinceStartup, dt = now - lastTick;
            nextTick = now + SamplePeriod; lastTick = now;
            if (EditorApplication.isPaused) return;
            try
            {
                if (!EditorApplication.isPlaying || !NetworkServer.active) { Finish("HOST_NO_ACTIVO"); return; }
                if (now - record.start > 900) { Finish("LIMITE_15_MINUTOS; INCOMPLETA"); return; }
                if (dt > .5) Add(0, "MUESTREO_INTERRUMPIDO", Vector3.zero, "Pausa/editor lento: " + dt.ToString("F2") + " s; no se certifica continuidad durante ese intervalo.");
                for (int i = 0; i < players.Count; i++)
                {
                    var p = players[i]; var r = record.participants[i];
                    if (!p || p.connectionToClient == null || p.connectionToClient.connectionId != r.connection || !p.MyCharacter || p.MyCharacter.netId != r.characterNetId || !p.MyCharacter.Health || p.MyCharacter.Health.CurrentHealth <= 0)
                    { Finish("MUERTE_DESCONEXION_O_REEMPLAZO; INCOMPLETA"); return; }
                    Observe(p.MyCharacter, r, dt);
                    if (!running) return;
                }
                record.elapsed = now - record.start;
                if (record.participants.All(p => p.complete))
                {
                    if (record.requested == 4)
                        for (int g = 0; g < 4; g++)
                        {
                            var times = record.participants.Select(p => p.gateAt[g]).ToArray();
                            if (times.All(t => t >= 0)) record.fourCrossingWindows[g] = times.Max() - times.Min();
                        }
                    Finish(record.initialCheckpoint>0 ? "TRAMO_PUNTUAL_OBSERVADO; ANALISIS_CON_FUENTE_ORIGINAL" : "RECORRIDOS_REALES_OBSERVADOS; REVISION_FISICA_Y_COOPERATIVA_PENDIENTE");
                }
                if (running && now >= nextSave) { Save(); nextSave = now + 30; }
                Refresh();
            }
            catch (Exception e) { Add(0, "ERROR_OBSERVADOR", Vector3.zero, e.ToString()); Finish("ERROR; INCOMPLETA"); }
        }

        void Observe(CharacterInstance actor, Participant r, double dt)
        {
            var position = actor.transform.position; var previous = r.lastPosition; var cc = actor.GetComponent<CharacterController>();
            if (!cc || !cc.enabled || actor.gameObject.scene.path != City) { Finish("CONTROLADOR_O_ESCENA_CAMBIO"); return; }
            if ((actor.transform.lossyScale - Vector3.one).sqrMagnitude > .000001f || Mathf.Abs(cc.radius-r.radius) > .0001f || Mathf.Abs(cc.height-r.height) > .0001f)
            { Add(actor.netId, "CAPSULA_O_ESCALA_CAMBIO", position, "La recepción requiere las dimensiones originales constantes."); Finish("DIMENSIONES_CAMBIARON; INCOMPLETA"); return; }
            float moved = Vector3.Distance(previous, position); r.distance += moved; r.samples++;
            if (moved > Mathf.Max(5f, (float)dt * 20)) { Add(actor.netId, "SALTO_DE_POSICION", position, moved + " m; no se acepta teleport como recorrido."); Finish("MOVIMIENTO_DISCONTINUO; INCOMPLETA"); return; }
            bool outsideXZ = position.x < 102 || position.x > 180 || position.z < 90 || position.z > 144;
            bool wrongHeight = position.y < -16.5f || position.y > -15.4f;
            if (outsideXZ || wrongHeight)
            {
                // Keep the terminal observation, explicitly without claiming physical queries.
                record.samples.Add(new Sample { seconds = EditorApplication.timeSinceStartup-record.start, character = actor.netId,
                    position = position, waypoint = r.next, physicalNotChecked = true, edge = -1 });
                Add(actor.netId, wrongHeight ? "CAIDA_O_COTA_INCOMPATIBLE" : "FUERA_AMBITO_XZ", position,
                    "Registro detenido. Último punto acreditado " + (r.next-1) + "; siguiente " + r.next + " " + route[Mathf.Min(r.next,route.Length-1)] +
                    ". Salir de X/Z no demuestra caída. Se puede continuar desde el último punto acreditado con esta captura.");
                Finish(wrongHeight ? "COTA_FUERA_RANGO; EVIDENCIA_PARCIAL" : "SALIDA_AMBITO_XZ; EVIDENCIA_PARCIAL"); return;
            }
            var feet = actor.transform.TransformPoint(cc.center) - Vector3.up * cc.height * .5f;
            int mask = 0; for (int layer = 0; layer < 32; layer++) if (!Physics.GetIgnoreLayerCollision(actor.gameObject.layer, layer)) mask |= 1 << layer;
            bool supported = Ground(feet, cc, mask);
            for (int k = 0; k < 8 && supported; k++)
                supported &= Ground(feet + new Vector3(Mathf.Cos(k * Mathf.PI / 4), 0, Mathf.Sin(k * Mathf.PI / 4)) * cc.radius, cc, mask);
            if (!supported && r.supportFailures++ == 0) Add(actor.netId, "HUELLA_SIN_APOYO", position, "Revisar escalón/borde; se registra, no se fuerza ni corrige la geometría.");
            var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
            bool nav = NavMesh.SamplePosition(feet, out var point, .35f, filter) && Mathf.Abs(point.position.y - feet.y) <= .35f;
            float edgeDistance = -1;
            if (!nav) { if (r.navFailures++ == 0) Add(actor.netId, "SIN_NAVMESH_LOCAL", position, "No equivale por sí solo a una caída física."); }
            else if (NavMesh.FindClosestEdge(point.position, out var edge, filter))
            {
                edgeDistance = edge.distance; r.minimumEdge = Mathf.Min(r.minimumEdge, edgeDistance);
                if (edgeDistance < .57f && r.nearEdgeSamples++ == 0) Add(actor.netId, "CERCA_LIMITE_NAVMESH", position, "Borde NavMesh no equivale a vacío físico; distancia " + edgeDistance);
            }
            if ((cc.collisionFlags & CollisionFlags.Sides) != 0) r.sideContactSamples++;
            int n = Physics.OverlapCapsuleNonAlloc(feet + Vector3.up * cc.radius, feet + Vector3.up * (cc.height - cc.radius), cc.radius, contacts, mask, QueryTriggerInteraction.Ignore);
            var nearby = new List<string>();
            for (int k = 0; k < n; k++) if (contacts[k] && !contacts[k].transform.IsChildOf(actor.transform)) nearby.Add(Hierarchy(contacts[k].transform));
            if (n == contacts.Length) Add(actor.netId, "CONSULTA_SATURADA", position, "No se interpreta como espacio libre.");
            if (!r.complete && actor.Input.Movement.sqrMagnitude > .1f && moved < .005f)
            {
                r.stalledFor += dt;
                if (r.stalledFor >= 5) { r.stallWarnings++; Add(actor.netId, "POSIBLE_ATASCO", position, "Cinco segundos con entrada y sin progreso; contactos: " + string.Join("; ", nearby)); r.stalledFor = 0; }
            }
            else r.stalledFor = 0;
            double seconds = EditorApplication.timeSinceStartup - record.start;
            for (int g = 0; g < 4; g++)
            {
                float x = g == 0 || g == 3 ? 167.47f : 132f;
                float z = g == 0 || g == 3 ? 99 : 117;
                bool positive = g == 0 || g == 2;
                if (r.gateAt[g] < 0 && Mathf.Abs(position.z - z) <= 3 &&
                    (positive ? previous.x < x && position.x >= x : previous.x > x && position.x <= x))
                    r.gateAt[g] = seconds;
            }
            if (!r.complete && Vector3.Distance(position, route[r.next]) < 1)
            {
                Add(actor.netId, "PUNTO_ALCANZADO", position, "Punto " + r.next + " en " + seconds.ToString("F1") + " s");
                r.next++; r.complete = r.next >= route.Length;
            }
            record.samples.Add(new Sample { seconds = seconds, character = actor.netId, position = position,
                input = new Vector3(actor.Input.Movement.x, 0, actor.Input.Movement.y), waypoint = r.next,
                collisionFlags = (int)cc.collisionFlags, grounded = cc.isGrounded, support = supported, nav = nav, edge = edgeDistance, contactObjects = nearby.Distinct().ToArray() });
            r.lastPosition = position;
        }

        bool Ground(Vector3 feet, CharacterController cc, int mask)
        {
            int n = Physics.RaycastNonAlloc(feet + Vector3.up * .2f, Vector3.down, hits, cc.stepOffset + cc.skinWidth + .3f, mask, QueryTriggerInteraction.Ignore);
            if (n == hits.Length) return false;
            for (int i = 0; i < n; i++)
                if (hits[i].collider && hits[i].collider.gameObject.scene.path == City && !hits[i].collider.GetComponentInParent<Health>() &&
                    Mathf.Abs(hits[i].point.y + 16) <= .03f && Vector3.Angle(hits[i].normal, Vector3.up) <= cc.slopeLimit)
                    return true;
            return false;
        }
        void Add(uint actor, string kind, Vector3 pos, string detail)
        {
            if (record.events.Count < 5000) record.events.Add(new Event { seconds = EditorApplication.timeSinceStartup - record.start, character = actor, kind = kind, position = pos, detail = detail });
        }
        void Finish(string result)
        {
            if (!running) return; running = false;
            record.result = result; record.endedUTC = DateTime.UtcNow.ToString("o"); record.elapsed = EditorApplication.timeSinceStartup - record.start;
            try
            {
                record.sceneAfter = Hash(City); record.navAfter = Hash(NavData); record.populationAfter = Hash(Population);
                record.protectedFilesUnchanged = record.sceneBefore == record.sceneAfter && record.navBefore == record.navAfter && record.populationBefore == record.populationAfter;
                Save();
                var evidence = WriteAnalysis(Path.Combine(output,"Recorrido_Warriors.json"), output);
                message = result + "\n" + evidence.status + "\nInforme: " + Path.GetFullPath(output); Debug.Log("Ciudad del Sol A03–R21: " + message);
            }
            catch (Exception e) { message = "No se pudo escribir el informe: " + e; Debug.LogError(message); }
            players.Clear(); Refresh();
        }
        void Save() => File.WriteAllText(Path.Combine(output, "Recorrido_Warriors.json"), JsonUtility.ToJson(record, true));

        void AnalyzeSelected()
        {
            try
            {
                if (running) throw new InvalidOperationException("Finalizá el registro activo antes de analizar otra captura.");
                var path = EditorUtility.OpenFilePanel("Captura original: solo lectura", "Backups/CiudadDelSol/Interior", "json");
                if (string.IsNullOrEmpty(path)) return;
                output = Path.Combine("Backups/CiudadDelSol/Interior", "AnalisisWalk_" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffZ"));
                var evidence = WriteAnalysis(path,output);
                message = evidence.status + "\n" + string.Join("\n",evidence.people.Select(p => "Warrior " + p.character + ": siguiente punto " + p.next + "; muestras " + p.sampleCount)) + "\n" + Path.GetFullPath(output); Refresh();
            }
            catch (Exception e) { message = e.Message; Refresh(); }
        }
        void ResumeSelected()
        {
            try
            {
                if (running) throw new InvalidOperationException("Finalizá primero el registro activo.");
                var path = EditorUtility.OpenFilePanel("Captura original para completar solo el tramo faltante", "Backups/CiudadDelSol/Interior", "json");
                if (string.IsNullOrEmpty(path)) return;
                var analysis = AnalyzeFile(path,out var capture);
                if (!analysis.routeValid || capture.requested != 1 || analysis.people.Count != 1 || analysis.people[0].complete ||
                    analysis.people[0].next < 2 || !string.IsNullOrEmpty(capture.sourceCapture))
                    throw new InvalidOperationException("Se requiere una captura individual original con prefijo acreditado y puntos faltantes. No se encadenan continuaciones automáticamente.");
                if (!capture.protectedFilesUnchanged || capture.sceneBefore != Hash(City) || capture.sceneAfter != capture.sceneBefore ||
                    capture.navBefore != Hash(NavData) || capture.navAfter != capture.navBefore ||
                    capture.populationBefore != Hash(Population) || capture.populationAfter != capture.populationBefore)
                    throw new InvalidOperationException("Los archivos protegidos no coinciden con la captura original. Se conserva la evidencia; no se combina automáticamente.");
                Begin(1,new ResumeSpec { path=Path.GetFullPath(path), hash=Hash(path), checkpoint=analysis.people[0].next-1 });
            }
            catch (Exception e) { message=e.Message; Refresh(); }
        }
        static CiudadDelSolWalkEvidence.Report AnalyzeFile(string path,out CiudadDelSolWalkEvidence.Capture capture,int depth=0)
        {
            if(depth>1)throw new InvalidOperationException("No se aceptan cadenas de capturas sin revisión.");
            capture=JsonUtility.FromJson<CiudadDelSolWalkEvidence.Capture>(File.ReadAllText(path));
            CiudadDelSolWalkEvidence.Report prior=null;
            if(capture.initialCheckpoint!=0)
            {
                if(string.IsNullOrEmpty(capture.sourceCapture)||!File.Exists(capture.sourceCapture)||Hash(capture.sourceCapture)!=capture.sourceSHA256)
                    throw new InvalidOperationException("No se encuentra la fuente exacta o cambió su SHA-256; no se acredita el prefijo.");
                prior=AnalyzeFile(capture.sourceCapture,out var source,depth+1);
                if(capture.sceneBefore!=source.sceneBefore||capture.navBefore!=source.navBefore||capture.populationBefore!=source.populationBefore)
                    throw new InvalidOperationException("La continuación y la fuente no corresponden a los mismos datos protegidos.");
            }
            return CiudadDelSolWalkEvidence.Analyze(capture,prior);
        }
        static CiudadDelSolWalkEvidence.Report WriteAnalysis(string source,string destination)
        {
            var result=AnalyzeFile(source,out var capture); Directory.CreateDirectory(destination);
            File.WriteAllText(Path.Combine(destination,"Analisis_evidencia.json"),JsonUtility.ToJson(result,true));
            File.WriteAllText(Path.Combine(destination,"Fuente_SHA256.txt"),Path.GetFullPath(source)+"\n"+Hash(source));
            var lines=new List<string>{"# A03–R21 — análisis de evidencia",result.status,"Resultado original: "+capture.result,
                "Causa de cierre: "+result.stopReason,
                "No se modifica la captura original ni se aprueba arquitectura automáticamente."};
            foreach(var p in result.people)
            {
                lines.Add("\n## Warrior "+p.character+"\nMuestras: "+p.sampleCount+". Próximo punto: "+p.next+". Apoyo fallido: "+p.supportFailures+". NavMesh ausente: "+p.navFailures+". Huecos de muestreo: "+p.gaps.Count+".");
                foreach(var hit in p.hits)lines.Add("- Punto "+hit.checkpoint+": "+hit.seconds.ToString("F3")+" s; distancia "+hit.distance.ToString("F3")+" m; "+hit.evidence+".");
                if(!p.complete)lines.Add("No acreditado el punto "+p.next+"; mínima distancia posterior registrada: "+p.nextClosestDistance.ToString("F3")+" m.");
            }
            lines.AddRange(result.warnings);lines.Add(result.limitation);
            File.WriteAllLines(Path.Combine(destination,"Analisis_evidencia.md"),lines);return result;
        }
        void Refresh()
        {
            if (status == null) return;
            status.text = message + (running ? "\n" + string.Join("\n", record.participants.Select(p => "Conexión " + p.connection + " · Warrior " + p.characterNetId + " · " +
                (p.complete ? "ida/vuelta registradas" : "próximo " + p.next + ": " + route[p.next].ToString("F2")) + " · " + p.distance.ToString("F1") + " m · alertas apoyo " + p.supportFailures + ", Nav " + p.navFailures + ", atasco " + p.stallWarnings)) : "");
        }
        void Draw(SceneView view)
        {
            if (!draw || route == null) return;
            Handles.color = Color.cyan;
            var guide = CiudadDelSolInteriorStageOne.NorthGuide();
            Handles.DrawAAPolyLine(4, guide.Select(p => p + Vector3.up * .15f).ToArray());
            for (int i = 0; i < guide.Length; i++) { Handles.DrawWireDisc(guide[i] + Vector3.up * .15f, Vector3.up, .8f); Handles.Label(guide[i] + Vector3.up * 1.5f, "A03–R21 " + i + " " + guide[i]); }
        }
        static string Hierarchy(Transform t) => t.parent ? Hierarchy(t.parent) + "/" + t.name : t.name;
        static string Hash(string path) { using (var sha = SHA256.Create()) using (var file = File.OpenRead(path)) return BitConverter.ToString(sha.ComputeHash(file)).Replace("-", ""); }
    }
}
