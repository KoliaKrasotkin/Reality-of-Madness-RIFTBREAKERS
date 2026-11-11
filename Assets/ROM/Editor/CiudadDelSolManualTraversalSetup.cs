using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.Horde;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Manual setup only: no Play callbacks, automated movement, bootstrap, scene replacement or gameplay changes.
    public static class CiudadDelSolManualTraversalSetup
    {
        const string Menu = "ROM/Dungeon Ciudad del Sol/Nucleo monumental conjunto/Prueba manual Warrior/";
        const string Original = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Warrior = "Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
        const string Key = "ROM.CiudadDelSol.ManualQA.";
        const string RuntimeName = "TEMP_Nucleo_Manual_Warrior_NO_GUARDAR";
        const string ApprovedPrefabGuid = "450b132d8394ca54f86ff67fca648d8a";
        const string ApprovedPrefabHash = "5C0B4E4E8D0888EACD18A677AAC1CEB1425DEAAC49D8E34717A87E5DCC435154";
        const string ApprovedSourceHash = "0E96A4D334A7A6FECCF75AF0124D8DDF5F8641B57620920264EFC20852314506";
        static Scene runtime;
        static GameObject environment;
        static Vector3 offset, returnPosition;
        static float returnYaw;
        static uint ownerId;
        static string runtimeSourceHash;

        static string Hash(string path)
        {
            using (var sha = SHA256.Create()) using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream));
        }
        static void Run(Action action) { try { action(); } catch (Exception e) { Debug.LogError("NUCLEO_MANUAL_DETENIDO: " + e.Message); } }
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static Bounds Envelope(GameObject root)
        {
            var boxes = root.GetComponentsInChildren<Collider>().Where(c => c.enabled).Select(c => c.bounds)
                .Concat(root.GetComponentsInChildren<Renderer>().Where(r => r.enabled).Select(r => r.bounds)).ToArray();
            Require(boxes.Length > 0, "Copia sin geometria activa.");
            var bounds = boxes[0]; foreach (var b in boxes.Skip(1)) bounds.Encapsulate(b); return bounds;
        }
        static string Layout(IEnumerable<GameObject> roots) => string.Join("\n", roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t =>
            t.GetInstanceID() + "|" + t.gameObject.activeSelf + "|" + t.localToWorldMatrix.ToString("F6") + "|" +
            string.Join(",", t.GetComponents<Collider>().Select(c => c.enabled + ":" + c.isTrigger))));

        static string SavedGuidKey => Key + Application.dataPath + ".PrefabGUID";
        static string ExistingPath()
        {
            var session = SessionState.GetString(Key + "Prefab", "");
            if (!string.IsNullOrEmpty(session) && File.Exists(session)) return session;
            var saved = AssetDatabase.GUIDToAssetPath(EditorPrefs.GetString(SavedGuidKey, ApprovedPrefabGuid));
            return File.Exists(saved) ? saved : AssetDatabase.GUIDToAssetPath(ApprovedPrefabGuid);
        }
        static bool ActiveInPrefab(Transform t, Transform root)
        {
            for (; t; t = t.parent) { if (!t.gameObject.activeSelf) return false; if (t == root) return true; }
            return false;
        }
        static GameObject ValidateExisting(string path)
        {
            Require(!EditorApplication.isCompiling, "Esperar a que termine la compilacion.");
            Require(!string.IsNullOrEmpty(path) && path.StartsWith("Assets/", StringComparison.Ordinal) && path.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) && File.Exists(path), "Seleccionar el Environment.prefab existente dentro de Assets.");
            Require(Hash(path).Replace("-", "") == ApprovedPrefabHash, "El archivo no coincide con la copia aprobada. No se regenera ni se acepta otra geometria automaticamente.");
            Require(Hash(Original).Replace("-", "") == ApprovedSourceHash, "La escena original difiere del origen aprobado; revisar antes de recuperar.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Require(prefab && EditorUtility.IsPersistent(prefab) && prefab.name == "Environment", "Prefab de ensayo no reconocido.");
            var generated = prefab.transform.Find("Nucleo_Monumental_Conjunto_TEMPORAL");
            Require(prefab.transform.childCount == 2 && prefab.transform.Find("Contexto_UNICO_por_identidad") && generated && generated.childCount == 3, "Jerarquia conjunta incompleta.");
            foreach (var name in new[]{"01 - Centro Oriental aprobado", "02 - Sureste arcadas aprobadas", "03 - El Sol Invertido V3"})
                Require(generated.Find(name), "Falta sector aprobado: " + name);
            Require(generated.GetComponentsInChildren<Renderer>(true).Count(r => ActiveInPrefab(r.transform, prefab.transform)) == 1979 && generated.GetComponentsInChildren<Collider>(true).Count(c => ActiveInPrefab(c.transform, prefab.transform)) == 1965, "Conteos activos distintos de los aprobados.");
            int meshes = 0, colliders = 0;
            foreach (var t in prefab.GetComponentsInChildren<Transform>(true))
            {
                Require(t.GetComponents<Component>().All(c => c), "Referencia a componente ausente: " + t.name);
                Require(t.GetComponents<MonoBehaviour>().Length == 0, "El entorno contiene scripts no previstos: " + t.name);
                foreach (var f in t.GetComponents<MeshFilter>()) { Require(f.sharedMesh && AssetDatabase.Contains(f.sharedMesh) && f.sharedMesh.vertexCount > 0, "Malla ausente o no persistente: " + t.name); meshes++; }
                foreach (var r in t.GetComponents<Renderer>())
                    Require(r is MeshRenderer && t.GetComponent<MeshFilter>() && r.sharedMaterials.Length > 0 && r.sharedMaterials.All(m => m && AssetDatabase.Contains(m) && m.shader), "Renderer/material/shader incompleto: " + t.name);
                foreach (var c in t.GetComponents<Collider>())
                {
                    if (c is MeshCollider mc) Require(mc.sharedMesh && AssetDatabase.Contains(mc.sharedMesh) && mc.sharedMesh.vertexCount > 0, "MeshCollider sin malla persistente: " + t.name);
                    colliders++;
                }
                foreach (var lod in t.GetComponents<LODGroup>())
                    Require(lod.GetLODs().All(l => l.renderers.All(r => r && r.transform.IsChildOf(prefab.transform))), "LOD con referencias ausentes o externas: " + t.name);
            }
            SessionState.SetString(Key + "Prefab", path); SessionState.SetString(Key + "SourceHash", Hash(Original));
            EditorPrefs.SetString(SavedGuidKey, AssetDatabase.AssetPathToGUID(path));
            Debug.Log("NUCLEO_MANUAL_RECUPERADO_OK: " + path + "; huella SHA256 aprobada;3 sectores;1979 renderers/1965 colliders activos del nucleo; mallas verificadas=" + meshes + "; colliders totales verificados=" + colliders + ". Solo validacion de copia existente; sin generar ni guardar escenas.");
            return prefab;
        }
        [MenuItem(Menu + "0. Recuperar y validar Environment existente")]
        public static void Recover() => Run(() =>
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode, "Recuperar fuera de Play.");
            var selected = AssetDatabase.GetAssetPath(Selection.activeObject);
            var path = string.IsNullOrEmpty(selected) || !selected.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) ? ExistingPath() : selected;
            Require(!string.IsNullOrEmpty(path), "Seleccionar Environment.prefab en Project y ejecutar otra vez esta opcion.");
            ValidateExisting(path);
        });

        [MenuItem(Menu + "1. Conservar copia jugable desde preview aprobada")]
        public static void Prepare() => Run(() =>
        {
            Require(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling, "Preparar fuera de Play y de compilacion.");
            var existing = ExistingPath();
            if (!string.IsNullOrEmpty(existing) && File.Exists(existing)) { ValidateExisting(existing); return; }
            Require(StageUtility.GetCurrentStage().GetType().FullName.Contains("CiudadDelSolNucleoConjuntoPreviewGenerator+JointStage"), "Mantener abierta la copia conjunta aprobada.");
            // Locate native scene objects, independent of managed builder references after a domain reload.
            var candidates = Resources.FindObjectsOfTypeAll<Transform>().Where(t => !t.parent && t.name == "Nucleo_Monumental_Conjunto_TEMPORAL" && EditorSceneManager.IsPreviewScene(t.gameObject.scene)).ToArray();
            Require(candidates.Length == 1, "No hay una unica copia conjunta reconocible; no se regenera automaticamente.");
            var source = candidates[0].gameObject.scene;
            var roots = source.GetRootGameObjects().Where(g => g.name == "Contexto_UNICO_por_identidad" || g.name == "Nucleo_Monumental_Conjunto_TEMPORAL").ToArray();
            Require(roots.Length == 2, "Contexto conjunto incompleto.");
            var generated = candidates[0].gameObject;
            Require(generated.GetComponentsInChildren<Renderer>().Length == 1979 && generated.GetComponentsInChildren<Collider>().Length == 1965, "Conteos distintos de la copia validada; detener y revisar.");
            var before = Layout(roots); var hash = Hash(Original);
            var staging = EditorSceneManager.NewPreviewScene();
            string folder = "Assets/ROM/QA/Temporary/NucleoManual_" + Guid.NewGuid().ToString("N");
            try
            {
                Directory.CreateDirectory(folder); AssetDatabase.Refresh();
                var holder = new GameObject("Nucleo_Manual_Snapshot"); holder.SetActive(false); SceneManager.MoveGameObjectToScene(holder, staging);
                foreach (var root in roots) { var copy = Object.Instantiate(root, holder.transform); copy.name = root.name; }
                // Remove disabled source behaviours before enabling the COPY; no network identities, events or VFX are exported.
                var components = holder.GetComponentsInChildren<Component>(true);
                foreach (var c in components.Reverse())
                    if (c && !(c is Transform) && !(c is MeshRenderer) && !(c is MeshFilter) && !(c is Collider) && !(c is LODGroup)) Object.DestroyImmediate(c);
                var meshes = new Dictionary<Mesh, Mesh>();
                Mesh Persist(Mesh mesh)
                {
                    if (!mesh || AssetDatabase.Contains(mesh)) return mesh;
                    if (meshes.TryGetValue(mesh, out var saved)) return saved;
                    saved = Object.Instantiate(mesh); saved.name = mesh.name;
                    AssetDatabase.CreateAsset(saved, folder + "/Mesh_" + meshes.Count.ToString("D4") + ".asset"); meshes.Add(mesh, saved); return saved;
                }
                foreach (var f in holder.GetComponentsInChildren<MeshFilter>(true)) f.sharedMesh = Persist(f.sharedMesh);
                foreach (var c in holder.GetComponentsInChildren<MeshCollider>(true)) c.sharedMesh = Persist(c.sharedMesh);
                foreach (var r in holder.GetComponentsInChildren<Renderer>(true)) foreach (var m in r.sharedMaterials)
                    Require(!m || AssetDatabase.Contains(m), "Material transitorio no previsto: " + r.name);
                // These flags refer only to copies; editor section cuts must not remove physical geometry in Play.
                SceneVisibilityManager.instance.Show(holder, true);
                holder.SetActive(true); Physics.SyncTransforms();
                var bounds = Envelope(holder);
                var prefabPath = folder + "/Environment.prefab";
                var savedPrefab = PrefabUtility.SaveAsPrefabAsset(holder, prefabPath);
                Require(savedPrefab, "No se pudo conservar la copia independiente.");
                AssetDatabase.SaveAssetIfDirty(savedPrefab); foreach (var mesh in meshes.Values) AssetDatabase.SaveAssetIfDirty(mesh);
                Require(Layout(roots) == before && Hash(Original) == hash, "Cambio inesperado en la fuente; no continuar.");
                SessionState.SetString(Key + "Prefab", prefabPath); SessionState.SetString(Key + "SourceHash", hash);
                File.WriteAllText(folder + "/README.txt", "Copia descartable de ensayo manual. No integrar. Sin bootstrap ni scripts de gameplay.\nOrigen: " + Original + "\nSHA256: " + hash + "\nMallas transitorias conservadas: " + meshes.Count + "\nBounds: " + bounds + "\nCargar solo desde el menu Prueba manual Warrior en un Host local ya iniciado.\n");
                Debug.Log("NUCLEO_MANUAL_COPIA_LISTA: " + prefabPath + "; mallas propias=" + meshes.Count + "; preview y original intactos. No se entro en Play.");
            }
            finally { EditorSceneManager.ClosePreviewScene(staging); }
        });

        static CharacterInstance HostWarrior()
        {
            Require(EditorApplication.isPlaying && !EditorApplication.isPaused, "Iniciar Host local manualmente, aparecer con Warrior y quitar Pausa.");
            Require(NetworkServer.active && NetworkClient.active && NetworkServer.localConnection != null, "Se requiere Host local; no cliente remoto ni servidor solo.");
            Require(NetworkServer.connections.Values.Count(c => c != null && c.identity) == 1, "Ensayo exclusivo de un Host local sin otros jugadores.");
            var actor = ClientFrontend.OwnedCharacterInstance;
            Require(actor && actor.isOwned && actor.isServer && actor.connectionToClient == NetworkServer.localConnection, "Warrior sin autoridad local/servidor correcta.");
            Require(actor.GetComponent<WarriorAvatar>() && !actor.GetComponent<PaladinPrototypePresentation>(), "Seleccionar Warrior real desde Personajes.");
            Require(actor.Health && actor.Health.CurrentHealth > 0, "El Warrior debe estar vivo.");
            var cc = actor.GetComponent<CharacterController>(); var template = AssetDatabase.LoadAssetAtPath<GameObject>(Warrior).GetComponent<CharacterController>();
            Require(cc && cc.enabled && cc.height == template.height && cc.radius == template.radius && cc.center == template.center && cc.slopeLimit == template.slopeLimit && cc.stepOffset == template.stepOffset && cc.skinWidth == template.skinWidth && cc.minMoveDistance == template.minMoveDistance && cc.transform.lossyScale == template.transform.lossyScale, "La capsula no coincide con el prefab Warrior; no se altera su configuracion.");
            var motor = actor.GetComponent<WarriorAvatar>();
            Require(motor.Grounded && motor.Velocity.sqrMagnitude < .01f, "Detener al Warrior sobre suelo firme antes de cargar o teletransportar."); return actor;
        }
        static void OriginalIntact() => Require(Hash(Original) == runtimeSourceHash, "El archivo original cambio desde la preparacion; revisar antes de continuar.");

        [MenuItem(Menu + "2. Cargar ensayo aditivo en Host local")]
        public static void Load() => Run(() =>
        {
            var actor = HostWarrior();
            Require(!runtime.IsValid() && !SceneManager.GetSceneByName(RuntimeName).IsValid(), "El ensayo ya esta cargado; no se duplica.");
            var prefab = ValidateExisting(ExistingPath());
            runtimeSourceHash = SessionState.GetString(Key + "SourceHash", ""); OriginalIntact();
            var active = SceneManager.GetActiveScene();
            var obstacles = Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c => c.enabled && c.gameObject.activeInHierarchy && !EditorSceneManager.IsPreviewScene(c.gameObject.scene)).ToArray();
            float east = obstacles.Length > 0 ? obstacles.Max(c => c.bounds.max.x) : 0;
            var hazards = Object.FindObjectsByType<HordeVoidKillPlane>(FindObjectsSortMode.None).Where(h => h.isActiveAndEnabled).ToArray();
            float killHeight = hazards.Length > 0 ? hazards.Max(h => h.KillHeight) : -38;
            var staging = EditorSceneManager.NewPreviewScene();
            try
            {
                var copy = (GameObject)PrefabUtility.InstantiatePrefab(prefab, staging); var localBounds = Envelope(copy);
                offset = new Vector3(Mathf.Ceil(Mathf.Max(2048, east + 256 - localBounds.min.x)), Mathf.Max(200, Mathf.Ceil(killHeight + 40 - localBounds.min.y)), 0);
                Require(offset.x < 16000 && offset.y < 4000, "El mapa/zona de muerte abarca demasiado espacio; revisar emplazamiento antes de cargar.");
                var world = new Bounds(localBounds.center + offset, localBounds.size); world.Expand(4);
                var conflict = obstacles.FirstOrDefault(c => world.Intersects(c.bounds));
                Require(!conflict, "Superposicion con collider/trigger activo: " + (conflict ? conflict.name : "") + "; no se modifica el mapa.");
                Require(-38 + offset.y > killHeight + 2, "El piso V3 no queda separado del limite de muerte.");
                Require(copy.GetComponentsInChildren<Collider>().Where(c => c.enabled && !c.isTrigger).All(c => (GameManager.environmentLayer & (1 << c.gameObject.layer)) != 0 && !Physics.GetIgnoreLayerCollision(actor.gameObject.layer, c.gameObject.layer)), "Capas de colision incompatibles: revisar; no cambiar capas ni matriz global.");
                runtime = SceneManager.CreateScene(RuntimeName); // Additive in memory: no scene loading/replacement or save.
                SceneManager.MoveGameObjectToScene(copy, runtime); copy.transform.position += offset; environment = copy;
                Require(SceneManager.GetActiveScene() == active, "Escena activa cambio inesperadamente.");
                returnPosition = actor.transform.position; returnYaw = actor.Input.LookY; ownerId = actor.netId;
                Physics.SyncTransforms(); OriginalIntact();
                Debug.Log("NUCLEO_MANUAL_ENTORNO_CARGADO: offset=" + offset + "; V3 localY-38 -> juegoY=" + (-38 + offset.y) + "; limite global=" + killHeight + " (sin cambios); limites activos=" + hazards.Length + "; sin superposiciones AABB con " + obstacles.Length + " colliders/triggers. Warrior todavia en el mapa de origen. Opcion3 para entrar.");
            }
            catch { if (runtime.IsValid()) { if (environment) Object.DestroyImmediate(environment); SceneManager.UnloadSceneAsync(runtime); runtime = default; } throw; }
            finally { EditorSceneManager.ClosePreviewScene(staging); }
        });

        static void Landing(CharacterInstance actor, Vector3 point)
        {
            Physics.SyncTransforms(); var cc = actor.GetComponent<CharacterController>();
            Require(Physics.Raycast(point + Vector3.up * .5f, Vector3.down, out var ground, 1, GameManager.environmentLayer, QueryTriggerInteraction.Ignore) && ground.normal.y > .7f, "El punto de llegada no tiene suelo estable.");
            var center = point + cc.center; float half = Mathf.Max(0, cc.height / 2 - cc.radius);
            Require(!Physics.OverlapCapsule(center - Vector3.up * half + Vector3.up * .1f, center + Vector3.up * half, cc.radius - cc.skinWidth, ~0, QueryTriggerInteraction.Ignore).Any(c => !c.transform.IsChildOf(actor.transform)), "Llegada ocupada por geometria; no se teletransporta.");
        }

        const string HallTour = "ROM/Dungeon Ciudad del Sol/Finalizacion subsuelo/Recorrer salas en Host/";

        [MenuItem(HallTour + "1. Jardines Dorados")]
        static void VisitGardens() => VisitHall(new Vector3(-81,-15.94f,-42));
        [MenuItem(HallTour + "2. Santuario Profano")]
        static void VisitSanctuary() => VisitHall(new Vector3(147,-15.94f,-90));
        [MenuItem(HallTour + "3. Claustro del Eclipse")]
        static void VisitCloister() => VisitHall(new Vector3(24,-15.94f,138));
        [MenuItem(HallTour + "4. El Sol Corrompido")]
        static void VisitSun() => VisitHall(new Vector3(189,-15.94f,150));

        static void VisitHall(Vector3 point) => Run(() =>
        {
            var actor = HostWarrior();
            var city = SceneManager.GetSceneByPath(Original);
            Require(city.IsValid() && city.isLoaded && city.GetRootGameObjects().Any(g => g.name=="Subsuelo_Salas_Monumentales"),
                "Cargar primero Ciudad del Sol definitiva desde el Host local.");
            Physics.SyncTransforms();
            Landing(actor,point);
            actor.ServerTeleport(point,0);
        });

        [MenuItem(Menu + "5. Probar Ciudad del Sol definitiva sin desplazamiento")]
        public static void EnterDefinitive() => Run(() =>
        {
            var actor = HostWarrior();
            Require(!runtime.IsValid() && !SceneManager.GetSceneByPath(Original).IsValid(),
                "Salir de Play si hay una copia de prueba o Ciudad del Sol cargada.");
            Require(Hash(Original).Replace("-", "") == "59448B459F7DEAB271CC61D1B228F495FF12BA8C21B05493386D949B27EF752B",
                "La escena definitiva difiere de la integracion validada.");
            var hostMap = SceneManager.GetSceneByPath("Assets/ROM/Levels/map_rom_horde.unity");
            Require(hostMap.IsValid() && hostMap.isLoaded, "Iniciar primero el Host local habitual y aparecer con Warrior.");

            void OnLoaded(Scene city, LoadSceneMode mode)
            {
                if (city.path != Original) return;
                SceneManager.sceneLoaded -= OnLoaded;
                var disabledColliders = new List<Collider>();
                var disabledRenderers = new List<Renderer>();
                var disabledPlanes = new List<HordeVoidKillPlane>();
                try
                {
                    Require(HostWarrior() == actor, "El Warrior del Host cambio durante la carga.");
                    var roots = city.GetRootGameObjects();
                    Require(roots.Count(g => g.name == "Nucleo_Monumental_Conjunto") == 1,
                        "La escena definitiva debe contener un unico nucleo integrado.");
                    var planes = roots.SelectMany(g => g.GetComponentsInChildren<HordeVoidKillPlane>()).ToArray();
                    Require(planes.Length == 1 && planes[0].enabled && planes[0].KillHeight <= -46,
                        "El plano de muerte de Ciudad del Sol no deja margen bajo V3.");
                    foreach (var root in roots)
                    {
                        foreach (var camera in root.GetComponentsInChildren<Camera>()) camera.enabled = false;
                        foreach (var listener in root.GetComponentsInChildren<AudioListener>()) listener.enabled = false;
                    }

                    var cityColliders = roots.SelectMany(g => g.GetComponentsInChildren<Collider>())
                        .Where(c => c.enabled && !c.isTrigger).ToArray();
                    Require(cityColliders.Length > 0, "Ciudad del Sol no tiene colliders activos.");
                    var footprint = cityColliders[0].bounds;
                    foreach (var collider in cityColliders) footprint.Encapsulate(collider.bounds);
                    footprint.Expand(4);
                    foreach (var root in hostMap.GetRootGameObjects())
                    {
                        disabledColliders.AddRange(root.GetComponentsInChildren<Collider>()
                            .Where(c => c.enabled && footprint.Intersects(c.bounds) && !c.GetComponentInParent<Health>()));
                        disabledRenderers.AddRange(root.GetComponentsInChildren<Renderer>()
                            .Where(r => r.enabled && footprint.Intersects(r.bounds) && !r.GetComponentInParent<Health>()));
                        disabledPlanes.AddRange(root.GetComponentsInChildren<HordeVoidKillPlane>().Where(p => p.enabled));
                    }
                    foreach (var collider in disabledColliders) collider.enabled = false;
                    foreach (var renderer in disabledRenderers) renderer.enabled = false;
                    foreach (var plane in disabledPlanes) plane.enabled = false;
                    Require(!Object.FindObjectsByType<HordeVoidKillPlane>(FindObjectsSortMode.None)
                        .Any(p => p.isActiveAndEnabled && p != planes[0] && p.KillHeight > -46),
                        "Otro plano de muerte activo invade el nivel profundo.");
                    Physics.SyncTransforms();
                    var point = new Vector3(162, 4.06f, 12);
                    Landing(actor, point);
                    actor.ServerTeleport(point, 0);
                    Debug.Log("CIUDAD_DEL_SOL_DEFINITIVA_QA: Warrior en " + point +
                        ", sin offset. Salir de Play para restaurar el mapa anfitrion.");
                }
                catch (Exception e)
                {
                    foreach (var collider in disabledColliders) if (collider) collider.enabled = true;
                    foreach (var renderer in disabledRenderers) if (renderer) renderer.enabled = true;
                    foreach (var plane in disabledPlanes) if (plane) plane.enabled = true;
                    SceneManager.UnloadSceneAsync(city);
                    Debug.LogException(e);
                }
            }

            SceneManager.sceneLoaded += OnLoaded;
            try { EditorSceneManager.LoadSceneInPlayMode(Original, new LoadSceneParameters(LoadSceneMode.Additive)); }
            catch { SceneManager.sceneLoaded -= OnLoaded; throw; }
        });

        [MenuItem(Menu + "3. Entrar al acceso original Y4")]
        public static void Enter() => Run(() =>
        {
            var actor = HostWarrior(); Require(environment && runtime.IsValid() && actor.netId == ownerId, "Cargar el ensayo con este Warrior primero."); OriginalIntact();
            var point = new Vector3(162, 4.06f, 12) + offset; Landing(actor, point);
            actor.ServerTeleport(point, 0); Debug.Log("NUCLEO_MANUAL_ENTRADA: metodo ServerTeleport existente; coordenadas de diseño=" + (point - offset) + "; sin modificar capacidades. Prueba manual ida/vuelta, marcha/carrera, sin salto ni rodada.");
        });
        [MenuItem(Menu + "4. Regresar al punto del mapa de origen")]
        public static void Return() => Run(() =>
        {
            var actor = HostWarrior(); Require(environment && actor.netId == ownerId, "Warrior original de esta sesion requerido."); OriginalIntact();
            var target = returnPosition + Vector3.up * .06f; Landing(actor, target); actor.ServerTeleport(target, returnYaw);
            Debug.Log("NUCLEO_MANUAL_REGRESO: teletransporte existente; sin cambios en escena original.");
        });
        [MenuItem(Menu + "5. Informar posicion y estado del Warrior")]
        public static void Status() => Run(() =>
        {
            var actor = ClientFrontend.OwnedCharacterInstance; Require(EditorApplication.isPlaying && actor, "Warrior no disponible.");
            var motor = actor.GetComponent<WarriorAvatar>(); var cc = actor.GetComponent<CharacterController>();
            Debug.Log("NUCLEO_MANUAL_ESTADO: mundo=" + actor.transform.position + "; diseño=" + (actor.transform.position - offset) + "; Grounded=" + (motor && motor.Grounded) + "; CCground=" + (cc && cc.isGrounded) + "; salud=" + actor.Health.CurrentHealth + "; velocidad=" + (motor ? motor.Velocity : Vector3.zero) + "; owned=" + actor.isOwned + "; server=" + actor.isServer + ". Observacion manual; no certificacion automatica.");
        });
    }
}
