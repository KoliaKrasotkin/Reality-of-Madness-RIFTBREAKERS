using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using Unity.AI.Navigation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace ROM.Horde.EditorTools
{
    /// <summary>
    /// Genera el mapa del modo Horda desde cero: plataforma circular flotando sobre
    /// el vacío, arsenal en el centro, coberturas en anillos, ocho puentes con un
    /// portal de la grieta al final de cada uno, navmesh, representer para el menú
    /// y registro en Build Settings y en el RoomCreator del hub.
    ///
    /// Es idempotente: volver a correrlo regenera la escena y los assets generados
    /// sin duplicar nada. Se usa desde el menú o en batchmode con
    /// -executeMethod ROM.Horde.EditorTools.HordeMapBuilder.BuildFromCommandLine
    /// </summary>
    public static class HordeMapBuilder
    {
        const string Root = "Assets/ROM/Levels/Horde";
        const string ScenePath = "Assets/ROM/Levels/map_rom_horde.unity";
        const string RepresenterPath = "Assets/ROM/Levels/representer_map_rom_horde.asset";
        const string HubScenePath = "Assets/ROM/Scenes/MainMenu.unity";
        const string KitPrefabs = "Assets/MultiFPS/Levels/Prefabs";

        const string MapName = "Horda: Plataforma de la Grieta";

        //medidas del mapa, en metros
        const float PlatformRadius = 30f;
        const float BridgeLength = 15f;
        const float BridgeWidth = 4f;
        const float PadRadius = 5f;
        const int PortalCount = 8;

        static float PadDistance => PlatformRadius + BridgeLength + PadRadius - 1.5f;

        [MenuItem("ROM/Horda/Construir mapa Horda")]
        public static void BuildFromMenu()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;

            Build();

            EditorUtility.DisplayDialog("Horda", $"Mapa generado en {ScenePath} y agregado al menú como \"{MapName}\".", "OK");
        }

        public static void BuildFromCommandLine()
        {
            try
            {
                Build();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void Build()
        {
            EnsureFolder(Root);
            EnsureFolder(Root + "/Meshes");
            EnsureFolder(Root + "/Materials");

            Materials mats = CreateMaterials();

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            SetupEnvironment(mats);

            Transform level = new GameObject("Level").transform;
            BuildPlatform(level, mats);
            BuildCover(level, mats);
            HordeRiftPortal[] portals = BuildBridgesAndPortals(level, mats);
            BuildSkyRift(level, mats);

            //se hornea con la geometría sola, antes de sumar pickups y armas con colliders propios
            BakeNavMesh();

            HordeWeaponCache cache = BuildWeaponCache();
            BuildPickups();
            SpawnpointsContainer playerSpawns = BuildPlayerSpawns();
            BuildDeathZone();
            BuildRoomManager(portals, cache, playerSpawns);
            InstantiateKitPrefab(KitPrefabs + "/Gameplay/GameplayCamera.prefab", new Vector3(0, 25, -40), Quaternion.Euler(30, 0, 0));

            AssignSceneIds();

            EditorSceneManager.SaveScene(scene, ScenePath);

            CreateRepresenter();
            AddToBuildSettings();
            RegisterInHub();

            AssetDatabase.SaveAssets();

            Debug.Log($"[ROM] Mapa Horda generado en {ScenePath}");
        }

        #region entorno
        static void SetupEnvironment(Materials mats)
        {
            RenderSettings.skybox = mats.Skybox;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.32f, 0.24f, 0.45f);
            RenderSettings.ambientEquatorColor = new Color(0.18f, 0.12f, 0.24f);
            RenderSettings.ambientGroundColor = new Color(0.05f, 0.03f, 0.08f);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.1f, 0.05f, 0.15f);
            RenderSettings.fogDensity = 0.006f;

            GameObject sun = new GameObject("Directional Light");
            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(0.85f, 0.82f, 1f);
            light.intensity = 1.3f;
            light.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
            RenderSettings.sun = light;

            GameObject center = new GameObject("Center Light");
            center.transform.position = new Vector3(0, 7, 0);
            Light centerLight = center.AddComponent<Light>();
            centerLight.type = LightType.Point;
            centerLight.color = new Color(1f, 0.85f, 0.6f);
            centerLight.range = 22f;
            centerLight.intensity = 8f;
        }

        static void BuildSkyRift(Transform parent, Materials mats)
        {
            //la grieta en el cielo: puramente decorativa
            MeshObject("SkyRift", parent, SaveMesh("sky_rift", BuildArc(46f, 49f, 1.5f, 0, 360, 96)),
                mats.Glow, new Vector3(0, 70, 0), Quaternion.Euler(18f, 0, 8f), false);

            MeshObject("SkyRiftInner", parent, SaveMesh("sky_rift_inner", BuildArc(30f, 31f, 1f, 0, 360, 96)),
                mats.Glow, new Vector3(0, 74, 0), Quaternion.Euler(-12f, 0, 20f), false);
        }
        #endregion

        #region geometría
        static void BuildPlatform(Transform parent, Materials mats)
        {
            Transform platform = new GameObject("Platform").transform;
            platform.SetParent(parent, false);

            MeshObject("Floor", platform, SaveMesh("platform", BuildArc(0, PlatformRadius, 1.2f, 0, 360, 128)),
                mats.Platform, new Vector3(0, -1.2f, 0), Quaternion.identity, true);

            //panza de la plataforma, para que no se vea como una moneda desde los puentes
            MeshObject("Underside", platform, SaveMesh("platform_underside", BuildArc(0, PlatformRadius * 0.7f, 4f, 0, 360, 96)),
                mats.Cover, new Vector3(0, -5.2f, 0), Quaternion.identity, false);

            //líneas de luz en el piso: marcan los anillos de cobertura
            float[] trims = { 4.6f, 13.6f, 20.4f, PlatformRadius - 0.6f };
            for (int i = 0; i < trims.Length; i++)
            {
                MeshObject($"FloorTrim_{i}", platform, SaveMesh($"floor_trim_{i}", BuildArc(trims[i] - 0.12f, trims[i], 0.02f, 0, 360, 128)),
                    mats.GlowSoft, new Vector3(0, 0.005f, 0), Quaternion.identity, false);
            }

            //tarima del arsenal
            MeshObject("ArsenalDais", platform, SaveMesh("dais", BuildArc(0, 4f, 0.3f, 0, 360, 64)),
                mats.Dais, Vector3.zero, Quaternion.identity, true);
            MeshObject("ArsenalDaisTrim", platform, SaveMesh("dais_trim", BuildArc(4f, 4.2f, 0.32f, 0, 360, 64)),
                mats.Glow, Vector3.zero, Quaternion.identity, false);

            //borde bajo con huecos donde llegan los puentes
            float gap = 13f;
            float step = 360f / PortalCount;
            Mesh rimMesh = SaveMesh("rim", BuildArc(PlatformRadius - 0.8f, PlatformRadius, 0.6f, gap * 0.5f, step - gap * 0.5f, 16));
            for (int i = 0; i < PortalCount; i++)
            {
                MeshObject($"Rim_{i}", platform, rimMesh, mats.Cover, Vector3.zero, Quaternion.Euler(0, i * step, 0), true);
            }
        }

        static void BuildCover(Transform parent, Materials mats)
        {
            Transform cover = new GameObject("Cover").transform;
            cover.SetParent(parent, false);

            //anillo interior: muros bajos curvos, para defender el arsenal
            Mesh innerWall = SaveMesh("inner_wall", BuildArc(9.4f, 10.2f, 1.25f, -15f, 15f, 12));
            for (int i = 0; i < 6; i++)
            {
                MeshObject($"InnerWall_{i}", cover, innerWall, mats.Cover, Vector3.zero, Quaternion.Euler(0, 30f + i * 60f, 0), true);
            }

            //anillo medio: pilares altos entre los puentes
            Mesh pillar = SaveMesh("pillar", BuildArc(0, 1.1f, 5f, 0, 360, 24));
            Mesh pillarBand = SaveMesh("pillar_band", BuildArc(1.1f, 1.18f, 0.18f, 0, 360, 24));
            float step = 360f / PortalCount;
            for (int i = 0; i < PortalCount; i++)
            {
                Vector3 pos = Polar(17f, step * i + step * 0.5f);
                MeshObject($"Pillar_{i}", cover, pillar, mats.Cover, pos, Quaternion.identity, true);
                MeshObject($"PillarBand_{i}", cover, pillarBand, mats.Glow, pos + Vector3.up * 3.6f, Quaternion.identity, false);
            }

            //anillo exterior: bloques en la boca de cada puente, cortan la línea de tiro directa
            for (int i = 0; i < PortalCount; i++)
            {
                float angle = step * i;
                Vector3 pos = Polar(24f, angle) + Vector3.up * 0.65f;
                GameObject block = Box($"BridgeBlock_{i}", cover, pos, Quaternion.Euler(0, angle, 0), new Vector3(3.2f, 1.3f, 0.9f), mats.Cover);
                block.isStatic = true;
            }
        }

        static HordeRiftPortal[] BuildBridgesAndPortals(Transform parent, Materials mats)
        {
            Transform bridges = new GameObject("Bridges").transform;
            bridges.SetParent(parent, false);

            Mesh padMesh = SaveMesh("portal_pad", BuildArc(0, PadRadius, 1f, 0, 360, 48));
            Mesh padTrim = SaveMesh("portal_pad_trim", BuildArc(PadRadius - 0.15f, PadRadius, 0.02f, 0, 360, 48));
            Mesh ringMesh = SaveMesh("portal_ring", BuildArc(2.6f, 3.1f, 0.45f, 0, 360, 48));
            Mesh coreMesh = SaveMesh("portal_core", BuildArc(0, 2.6f, 0.06f, 0, 360, 48));

            HordeRiftPortal[] portals = new HordeRiftPortal[PortalCount];
            float step = 360f / PortalCount;

            for (int i = 0; i < PortalCount; i++)
            {
                float angle = step * i;
                Vector3 outward = Polar(1f, angle);
                Quaternion faceCenter = Quaternion.LookRotation(-outward);

                //puente
                float bridgeStart = PlatformRadius - 0.6f;
                float bridgeCenter = bridgeStart + BridgeLength * 0.5f;
                Box($"Bridge_{i}", bridges, outward * bridgeCenter + Vector3.down * 0.3f, faceCenter,
                    new Vector3(BridgeWidth, 0.6f, BridgeLength), mats.Bridge).isStatic = true;

                for (int side = -1; side <= 1; side += 2)
                {
                    Vector3 edge = outward * bridgeCenter + faceCenter * new Vector3(side * (BridgeWidth * 0.5f - 0.08f), 0.02f, 0);
                    DestroyCollider(Box($"BridgeTrim_{i}_{side}", bridges, edge, faceCenter, new Vector3(0.12f, 0.05f, BridgeLength), mats.GlowSoft));
                }

                //plataforma del portal
                GameObject portalRoot = new GameObject($"RiftPortal_{i}");
                portalRoot.transform.SetParent(bridges, false);
                portalRoot.transform.position = outward * PadDistance;
                portalRoot.transform.rotation = faceCenter;

                MeshObject("Pad", portalRoot.transform, padMesh, mats.Platform, Vector3.down, Quaternion.identity, true, true);
                MeshObject("PadTrim", portalRoot.transform, padTrim, mats.GlowSoft, new Vector3(0, 0.005f, 0), Quaternion.identity, false, true);

                //marco del portal, detrás de los puntos de aparición
                LocalBox("FrameLeft", portalRoot.transform, new Vector3(-3.5f, 3.3f, -3f), new Vector3(0.6f, 6.6f, 0.6f), mats.Cover);
                LocalBox("FrameRight", portalRoot.transform, new Vector3(3.5f, 3.3f, -3f), new Vector3(0.6f, 6.6f, 0.6f), mats.Cover);
                LocalBox("FrameTop", portalRoot.transform, new Vector3(0, 6.9f, -3f), new Vector3(7.6f, 0.6f, 0.6f), mats.Cover);

                //anillo giratorio: la malla es plana en XZ, el pivote la para de frente al centro
                Transform spinner = new GameObject("Spinner").transform;
                spinner.SetParent(portalRoot.transform, false);
                spinner.localPosition = new Vector3(0, 3.5f, -3f);
                spinner.localRotation = Quaternion.Euler(90f, 0, 0);

                GameObject ring = MeshObject("Ring", spinner, ringMesh, mats.Glow, new Vector3(0, -0.22f, 0), Quaternion.identity, false, true);
                GameObject core = MeshObject("Core", spinner, coreMesh, mats.RiftCore, new Vector3(0, -0.03f, 0), Quaternion.identity, false, true);

                //giran en runtime: si quedaran estáticos el static batching los congelaría
                ring.isStatic = false;
                core.isStatic = false;

                GameObject lightGo = new GameObject("PortalLight");
                lightGo.transform.SetParent(portalRoot.transform, false);
                lightGo.transform.localPosition = new Vector3(0, 3.5f, -1.5f);
                Light portalLight = lightGo.AddComponent<Light>();
                portalLight.type = LightType.Point;
                portalLight.range = 16f;
                portalLight.intensity = 0f;

                //puntos de aparición delante del anillo
                Vector3[] spawnOffsets =
                {
                    new Vector3(-1.8f, 0.1f, -0.8f),
                    new Vector3(0f, 0.1f, 0.4f),
                    new Vector3(1.8f, 0.1f, -0.8f),
                };

                Transform[] spawnPoints = new Transform[spawnOffsets.Length];
                for (int s = 0; s < spawnOffsets.Length; s++)
                {
                    Transform point = new GameObject($"EnemySpawn_{s}").transform;
                    point.SetParent(portalRoot.transform, false);
                    point.localPosition = spawnOffsets[s];
                    spawnPoints[s] = point;
                }

                HordeRiftPortal portal = portalRoot.AddComponent<HordeRiftPortal>();
                SerializedObject so = new SerializedObject(portal);
                SetArray(so.FindProperty("_spawnPoints"), spawnPoints);
                SetArray(so.FindProperty("_glowRenderers"), new UnityEngine.Object[] { ring.GetComponent<Renderer>(), core.GetComponent<Renderer>() });
                so.FindProperty("_light").objectReferenceValue = portalLight;
                so.FindProperty("_spinningPart").objectReferenceValue = spinner;
                so.ApplyModifiedPropertiesWithoutUndo();

                portals[i] = portal;
            }

            return portals;
        }
        #endregion

        #region gameplay
        static HordeWeaponCache BuildWeaponCache()
        {
            string[] weaponNames =
            {
                "item_rifleV2", "item_shotgun", "item_lightMachineGun", "item_sniperRifle",
                "item_rocketLauncher", "item_rifle", "item_shotgun", "item_rifleV2",
            };

            GameObject go = new GameObject("Arsenal");
            HordeWeaponCache cache = go.AddComponent<HordeWeaponCache>();

            Transform[] slots = new Transform[weaponNames.Length];
            for (int i = 0; i < slots.Length; i++)
            {
                float angle = 360f / slots.Length * i;
                Transform slot = new GameObject($"WeaponSlot_{i}").transform;
                slot.SetParent(go.transform, false);
                slot.position = Polar(2.4f, angle) + Vector3.up * 0.75f;
                slot.rotation = Quaternion.Euler(0, angle + 90f, 0);
                slots[i] = slot;
            }

            SerializedObject so = new SerializedObject(cache);
            SetArray(so.FindProperty("_weapons"), weaponNames.Select(LoadItem).ToArray());
            SetArray(so.FindProperty("_slots"), slots);
            so.ApplyModifiedPropertiesWithoutUndo();

            return cache;
        }

        static void BuildPickups()
        {
            Transform pickups = new GameObject("Pickups").transform;

            //munición rodeando la tarima del arsenal
            string[] ammo =
            {
                "Pickup_RifleAmmo", "Pickup_ShotgunAmmo", "Pickup_SniperRifleAmmo", "Pickup_RocketLauncher",
                "Pickup_RifleAmmo", "Pickup_PistolAmmo", "Pickup_GrenadesBox",
            };
            for (int i = 0; i < ammo.Length; i++)
            {
                InstantiateKitPrefab($"{KitPrefabs}/Gameplay/Props/{ammo[i]}.prefab",
                    Polar(6f, 360f / ammo.Length * i + 20f) + Vector3.up * 0.6f, Quaternion.identity, pickups);
            }

            //botiquines chicos detrás de los muros interiores
            for (int i = 0; i < 6; i++)
            {
                InstantiateKitPrefab($"{KitPrefabs}/Gameplay/Props/prop_healthKit.prefab",
                    Polar(8.4f, 30f + i * 60f) + Vector3.up * 0.6f, Quaternion.identity, pickups);
            }

            //botiquines grandes cerca del borde: arriesgar para curarse
            for (int i = 0; i < 4; i++)
            {
                InstantiateKitPrefab($"{KitPrefabs}/Gameplay/Props/prop_bigHealthKit.prefab",
                    Polar(26.5f, 22.5f + i * 90f) + Vector3.up * 0.6f, Quaternion.identity, pickups);
            }
        }

        static SpawnpointsContainer BuildPlayerSpawns()
        {
            GameObject go = new GameObject("SpawnPoints_Jugadores");
            SpawnpointsContainer container = go.AddComponent<SpawnpointsContainer>();
            container.Spawnpoints = new List<Transform>();

            for (int i = 0; i < 6; i++)
            {
                float angle = 60f * i;
                Transform point = new GameObject($"PlayerSpawn_{i}").transform;
                point.SetParent(go.transform, false);
                point.position = Polar(6.5f, angle) + Vector3.up * 0.1f;
                point.rotation = Quaternion.LookRotation(Polar(1f, angle)); //mirando hacia afuera, hacia la horda
                container.Spawnpoints.Add(point);
            }

            return container;
        }

        static void BuildDeathZone()
        {
            GameObject go = new GameObject("DeathZone");
            go.layer = (int)GameLayers.trigger; //así el navmesh no lo toma como piso
            go.transform.position = new Vector3(0, -45f, 0);

            BoxCollider box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.size = new Vector3(400f, 20f, 400f);

            go.AddComponent<NetworkIdentity>();
            DeathZone zone = go.AddComponent<DeathZone>();
            zone.DespawnItems = true;
        }

        static void BuildRoomManager(HordeRiftPortal[] portals, HordeWeaponCache cache, SpawnpointsContainer playerSpawns)
        {
            GameObject go = new GameObject("ROOMMANAGER");
            go.AddComponent<NetworkIdentity>();
            go.AddComponent<GameSync>();
            go.AddComponent<RoomManager>();
            Horde horde = go.AddComponent<Horde>();

            SerializedObject so = new SerializedObject(horde);
            SetArray(so.FindProperty("_portals"), portals);
            so.FindProperty("_weaponCache").objectReferenceValue = cache;
            so.FindProperty("defaultSpawnPoints").objectReferenceValue = playerSpawns;

            //un elemento por slot del personaje: primaria, secundaria, cuchillo, granada, bomba
            SetArray(so.FindProperty("_playerStartingItems"), new UnityEngine.Object[]
            {
                LoadItem("item_pistol"), null, LoadItem("item_knife"), LoadItem("item_grenade"), null,
            });
            SetArray(so.FindProperty("_artilleryWeapons"), new UnityEngine.Object[]
            {
                LoadItem("item_rocketLauncher"), LoadItem("item_lightMachineGun"), LoadItem("item_shotgun"),
            });
            so.FindProperty("_meleeWeapon").objectReferenceValue = LoadItem("item_knife");
            so.ApplyModifiedPropertiesWithoutUndo();

            //el HUD es un prefab editable (Assets/ROM/UI/HordeHUD.prefab) que queda en la escena
            ROM.UI.EditorTools.ROMUIBuilder.InstallHordeHUD(horde);
        }

        static void BakeNavMesh()
        {
            GameObject go = new GameObject("NavMesh");
            NavMeshSurface surface = go.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.All;
            surface.useGeometry = NavMeshCollectGeometry.PhysicsColliders;
            surface.layerMask = 1 << 0;
            surface.agentTypeID = 0;

            surface.BuildNavMesh();

            string path = $"{Root}/NavMesh-Horde.asset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(surface.navMeshData, path);
            EditorUtility.SetDirty(surface);
        }

        /// <summary>
        /// Mirror asigna el sceneId en OnValidate. Al armar la escena por código no
        /// siempre se llega a llamar, así que lo forzamos y verificamos.
        /// </summary>
        static void AssignSceneIds()
        {
            MethodInfo onValidate = typeof(NetworkIdentity).GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);

            foreach (NetworkIdentity identity in UnityEngine.Object.FindObjectsByType<NetworkIdentity>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (identity.sceneId == 0 && onValidate != null)
                    onValidate.Invoke(identity, null);

                if (identity.sceneId == 0)
                    Debug.LogError($"[ROM] {identity.name} quedó sin sceneId; abrí y guardá la escena a mano.", identity);

                EditorUtility.SetDirty(identity);
            }
        }
        #endregion

        #region registro en el menú
        static void CreateRepresenter()
        {
            MapRepresenter representer = AssetDatabase.LoadAssetAtPath<MapRepresenter>(RepresenterPath);
            bool isNew = representer == null;

            if (isNew)
                representer = ScriptableObject.CreateInstance<MapRepresenter>();

            representer.Scene = ScenePath;
            representer.Name = MapName;
            representer.AvailableGamemodes = new[] { Gamemodes.Raid };
            representer.MaxPlayersPresets = new[] { 2, 4, 8 };

            if (isNew)
                AssetDatabase.CreateAsset(representer, RepresenterPath);
            else
                EditorUtility.SetDirty(representer);
        }

        static void AddToBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

            if (scenes.Any(s => s.path == ScenePath))
                return;

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// Agrega el representer a la lista Maps del RoomCreator del hub editando el
        /// YAML a mano: abrir y guardar el hub desde el editor lo re-serializa entero
        /// y deja un diff enorme en una escena que no es nuestra.
        /// </summary>
        static void RegisterInHub()
        {
            string guid = AssetDatabase.AssetPathToGUID(RepresenterPath);
            string entry = $"  - {{fileID: 11400000, guid: {guid}, type: 2}}";

            List<string> lines = File.ReadAllLines(HubScenePath).ToList();

            if (lines.Contains(entry))
                return;

            int mapsLine = lines.IndexOf("  Maps:");
            if (mapsLine < 0)
            {
                Debug.LogError("[ROM] No se encontró la lista Maps del RoomCreator en el hub; agregá el representer de la Horda a mano.");
                return;
            }

            int insertAt = mapsLine + 1;
            while (insertAt < lines.Count && lines[insertAt].StartsWith("  - {fileID: 11400000"))
                insertAt++;

            lines.Insert(insertAt, entry);

            //mismo fin de línea que el archivo original
            File.WriteAllText(HubScenePath, string.Join("\n", lines) + "\n");
            AssetDatabase.ImportAsset(HubScenePath);
        }
        #endregion

        #region materiales
        class Materials
        {
            public Material Platform, Dais, Cover, Bridge, Glow, GlowSoft, RiftCore, Skybox;
        }

        static Materials CreateMaterials()
        {
            return new Materials
            {
                Platform = LitMaterial("horde_platform", new Color(0.16f, 0.16f, 0.2f), 0.6f, 0.45f),
                Dais = LitMaterial("horde_dais", new Color(0.55f, 0.42f, 0.18f), 0.9f, 0.6f),
                Cover = LitMaterial("horde_cover", new Color(0.28f, 0.27f, 0.32f), 0.2f, 0.25f),
                Bridge = LitMaterial("horde_bridge", new Color(0.12f, 0.11f, 0.15f), 0.7f, 0.5f),
                Glow = EmissiveMaterial("horde_glow", new Color(0.75f, 0.2f, 1f), 4f),
                GlowSoft = EmissiveMaterial("horde_glow_soft", new Color(0.55f, 0.25f, 1f), 1.6f),
                RiftCore = EmissiveMaterial("horde_rift_core", new Color(0.25f, 0.05f, 0.4f), 2f),
                Skybox = SkyboxMaterial(),
            };
        }

        static Material LitMaterial(string name, Color color, float metallic, float smoothness)
        {
            Material mat = LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Metallic", metallic);
            mat.SetFloat("_Smoothness", smoothness);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material EmissiveMaterial(string name, Color color, float intensity)
        {
            Material mat = LoadOrCreateMaterial(name, "Universal Render Pipeline/Lit");
            mat.SetColor("_BaseColor", color);
            mat.SetFloat("_Smoothness", 0.8f);
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", color * intensity);
            mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material SkyboxMaterial()
        {
            Material mat = LoadOrCreateMaterial("horde_skybox", "Skybox/Procedural");
            mat.SetColor("_SkyTint", new Color(0.45f, 0.2f, 0.6f));
            mat.SetColor("_GroundColor", new Color(0.04f, 0.02f, 0.06f));
            mat.SetFloat("_AtmosphereThickness", 0.55f);
            mat.SetFloat("_Exposure", 0.7f);
            mat.SetFloat("_SunSize", 0.02f);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        static Material LoadOrCreateMaterial(string name, string shaderName)
        {
            string path = $"{Root}/Materials/{name}.mat";
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            Shader shader = Shader.Find(shaderName);

            if (!shader)
                throw new InvalidOperationException($"No se encontró el shader {shaderName}");

            if (!mat)
            {
                mat = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = shader;
            }

            return mat;
        }
        #endregion

        #region helpers de escena
        static GameObject MeshObject(string name, Transform parent, Mesh mesh, Material mat, Vector3 position, Quaternion rotation, bool collider, bool local = false)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);

            if (local)
            {
                go.transform.localPosition = position;
                go.transform.localRotation = rotation;
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
            }

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;

            if (collider)
                go.AddComponent<MeshCollider>().sharedMesh = mesh;
            else
                renderer.shadowCastingMode = ShadowCastingMode.Off;

            go.isStatic = true;
            return go;
        }

        static GameObject Box(string name, Transform parent, Vector3 position, Quaternion rotation, Vector3 size, Material mat)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        static GameObject LocalBox(string name, Transform parent, Vector3 localPosition, Vector3 size, Material mat)
        {
            GameObject go = Box(name, parent, Vector3.zero, Quaternion.identity, size, mat);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.identity;
            go.isStatic = true;
            return go;
        }

        static void DestroyCollider(GameObject go)
        {
            Collider c = go.GetComponent<Collider>();
            if (c) UnityEngine.Object.DestroyImmediate(c);
            go.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        static GameObject InstantiateKitPrefab(string path, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (!prefab)
            {
                Debug.LogError($"[ROM] No se encontró el prefab {path}");
                return null;
            }

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent)
                instance.transform.SetParent(parent, false);
            instance.transform.position = position;
            instance.transform.rotation = rotation;
            return instance;
        }

        static Item LoadItem(string prefabName)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitPrefabs}/Gameplay/{prefabName}.prefab");
            Item item = prefab ? prefab.GetComponent<Item>() : null;

            if (!item)
                Debug.LogError($"[ROM] No se encontró el arma {prefabName}");

            return item;
        }

        static void SetArray(SerializedProperty property, IList<UnityEngine.Object> values)
        {
            property.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void SetArray<T>(SerializedProperty property, T[] values) where T : UnityEngine.Object
        {
            SetArray(property, values.Cast<UnityEngine.Object>().ToList());
        }

        /// <summary>
        /// Punto sobre el piso a una distancia y ángulo del centro. El ángulo crece
        /// en sentido horario visto desde arriba, igual que la rotación en Y.
        /// </summary>
        static Vector3 Polar(float radius, float angleDeg)
        {
            float a = angleDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(a), 0, Mathf.Cos(a)) * radius;
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
        #endregion

        #region mallas
        static Mesh SaveMesh(string name, Mesh mesh)
        {
            string path = $"{Root}/Meshes/{name}.asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);

            mesh.name = name;

            if (existing)
            {
                //se reescribe en el lugar para no romper referencias de otras escenas
                EditorUtility.CopySerialized(mesh, existing);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        /// <summary>
        /// Sector de anillo extruido: con inner = 0 es un cilindro (o una porción de
        /// torta), con inner > 0 es un anillo. Los ángulos siguen la convención de
        /// Polar. Las caras tienen normales planas salvo los laterales curvos.
        /// </summary>
        static Mesh BuildArc(float inner, float outer, float height, float startDeg, float endDeg, int segments)
        {
            MeshData m = new MeshData();
            Vector3 up = Vector3.up * height;
            bool full = Mathf.Abs(endDeg - startDeg) >= 359.9f;

            for (int i = 0; i < segments; i++)
            {
                Vector3 d0 = Polar(1f, Mathf.Lerp(startDeg, endDeg, i / (float)segments));
                Vector3 d1 = Polar(1f, Mathf.Lerp(startDeg, endDeg, (i + 1) / (float)segments));

                //tapa superior e inferior
                if (inner > 0f)
                {
                    m.Quad(d0 * inner + up, d0 * outer + up, d1 * outer + up, d1 * inner + up, Vector3.up);
                    m.Quad(d0 * inner, d1 * inner, d1 * outer, d0 * outer, Vector3.down);
                }
                else
                {
                    m.Tri(up, d0 * outer + up, d1 * outer + up, Vector3.up);
                    m.Tri(Vector3.zero, d1 * outer, d0 * outer, Vector3.down);
                }

                //lateral exterior e interior, con normales suaves
                m.SmoothQuad(d0 * outer, d1 * outer, d1 * outer + up, d0 * outer + up, d0, d1);

                if (inner > 0f)
                    m.SmoothQuad(d1 * inner, d0 * inner, d0 * inner + up, d1 * inner + up, -d1, -d0);
            }

            if (!full)
            {
                Vector3 s = Polar(1f, startDeg);
                Vector3 e = Polar(1f, endDeg);
                Vector3 middle = Polar(1f, (startDeg + endDeg) * 0.5f);

                //cada tapa mira hacia afuera del sector, o sea lejos de su bisectriz
                Vector3 sNormal = Vector3.Cross(Vector3.up, s).normalized;
                if (Vector3.Dot(sNormal, middle) > 0f) sNormal = -sNormal;
                Vector3 eNormal = Vector3.Cross(Vector3.up, e).normalized;
                if (Vector3.Dot(eNormal, middle) > 0f) eNormal = -eNormal;

                m.Quad(s * inner, s * outer, s * outer + up, s * inner + up, sNormal);
                m.Quad(e * inner, e * outer, e * outer + up, e * inner + up, eNormal);
            }

            return m.ToMesh();
        }

        class MeshData
        {
            readonly List<Vector3> _vertices = new List<Vector3>();
            readonly List<Vector3> _normals = new List<Vector3>();
            readonly List<Vector2> _uvs = new List<Vector2>();
            readonly List<int> _triangles = new List<int>();

            public void Tri(Vector3 a, Vector3 b, Vector3 c, Vector3 normal)
            {
                //en Unity la cara visible es la que tiene Cross(b - a, c - a) apuntando al que mira
                if (Vector3.Dot(Vector3.Cross(b - a, c - a), normal) < 0f)
                    (b, c) = (c, b);

                int start = _vertices.Count;
                Add(a, normal);
                Add(b, normal);
                Add(c, normal);
                _triangles.Add(start);
                _triangles.Add(start + 1);
                _triangles.Add(start + 2);
            }

            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
            {
                Tri(a, b, c, normal);
                Tri(a, c, d, normal);
            }

            public void SmoothQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normalAD, Vector3 normalBC)
            {
                Vector3 faceNormal = (normalAD + normalBC).normalized;
                bool flip = Vector3.Dot(Vector3.Cross(b - a, c - a), faceNormal) < 0f;

                int start = _vertices.Count;
                Add(a, normalAD);
                Add(b, normalBC);
                Add(c, normalBC);
                Add(d, normalAD);

                if (flip)
                {
                    _triangles.AddRange(new[] { start, start + 2, start + 1, start, start + 3, start + 2 });
                }
                else
                {
                    _triangles.AddRange(new[] { start, start + 1, start + 2, start, start + 2, start + 3 });
                }
            }

            void Add(Vector3 vertex, Vector3 normal)
            {
                _vertices.Add(vertex);
                _normals.Add(normal);

                //proyección simple: arriba/abajo usa XZ, los laterales usan el desarrollo
                _uvs.Add(Mathf.Abs(normal.y) > 0.5f
                    ? new Vector2(vertex.x, vertex.z) * 0.25f
                    : new Vector2(vertex.x + vertex.z, vertex.y) * 0.25f);
            }

            public Mesh ToMesh()
            {
                Mesh mesh = new Mesh();
                if (_vertices.Count > 65000)
                    mesh.indexFormat = IndexFormat.UInt32;

                mesh.SetVertices(_vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                mesh.SetTriangles(_triangles, 0);
                mesh.RecalculateBounds();
                mesh.RecalculateTangents();
                return mesh;
            }
        }
        #endregion
    }
}
