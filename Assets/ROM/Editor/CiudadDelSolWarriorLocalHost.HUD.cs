using System;
using System.Collections.Generic;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay.Gamemodes;
using ROM.UI.Gameplay;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        static Action PrepareCityRaidHud()
        {
            Check(EditorApplication.isPlaying && NetworkServer.active && GameManager.Gamemode is Raid &&
                SceneManager.GetActiveScene().path == City, "HUD: solo disponible en el ensayo Raid de Ciudad del Sol.");
            var presentation = new CityRaidHudPresentation();
            try { presentation.Prepare(); return presentation.Dispose; }
            catch { presentation.Dispose(); throw; }
        }

        // Editor-owned adapter: only touches live instances and restores them on trial cleanup.
        sealed class CityRaidHudPresentation : IDisposable
        {
            readonly List<GameObject> created = new List<GameObject>();
            readonly List<Action> restore = new List<Action>();
            RiftGameplayHUD hud;
            RiftFieldUI field;
            bool disposed;

            public void Prepare()
            {
                var huds = Object.FindObjectsByType<RiftGameplayHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                var fields = Object.FindObjectsByType<RiftFieldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                Check(huds.Length <= 1 && fields.Length <= 1, "HUD: hay interfaces duplicadas; no se crearon otras.");
                Check(huds.Length == fields.Length, "HUD: hay una integración parcial preexistente; no se modificará automáticamente.");
                var hudPrefab = Resources.Load<GameObject>("RiftGameplayHUD");
                var fieldPrefab = Resources.Load<GameObject>("RiftFieldUI");
                Check(hudPrefab && hudPrefab.GetComponent<RiftGameplayHUD>() && fieldPrefab && fieldPrefab.GetComponent<RiftFieldUI>(),
                    "HUD: faltan los dos prefabs originales en Resources.");
                var basicPrefab = MultiFPS.UI.ClientInterfaceManager.Instance.PlayerHudUI;
                var killInfo = basicPrefab.transform.Find("GameplayHUD/KillInfo");
                var damageIndicator = basicPrefab.transform.Find("GameplayHUD/TakenDamageIndicator");
                Check(killInfo && damageIndicator && killInfo.GetComponent<UnityEngine.UI.Text>() &&
                    !killInfo.GetComponent<UnityEngine.UI.Text>().raycastTarget &&
                    damageIndicator.GetComponent<UnityEngine.UI.Image>() && !damageIndicator.GetComponent<UnityEngine.UI.Image>().raycastTarget,
                    "HUD básico: no se confirmó la corrección de raycasts de KillInfo y TakenDamageIndicator. No se alteró el prefab.");

                // Field first: the original HUD may dock its map/potion controls during Start.
                field = fields.Length == 1 ? fields[0] : InstantiateOriginal<RiftFieldUI>(fieldPrefab);
                hud = huds.Length == 1 ? huds[0] : InstantiateOriginal<RiftGameplayHUD>(hudPrefab);
                Check(field.gameObject.scene.path == City && hud.gameObject.scene.path == City &&
                    field.isActiveAndEnabled && hud.isActiveAndEnabled, "HUD: las interfaces deben estar activas en Ciudad del Sol.");
                Check(hud.Theme && hud.Minimap && hud.Minimap.Map && field.MapButton && field.MapPanel && field.MapImage,
                    "HUD: las referencias originales de presentación están incompletas.");
                Check(Object.FindObjectsByType<RiftGameplayHUD>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1 &&
                    Object.FindObjectsByType<RiftFieldUI>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length == 1,
                    "HUD: se detectó una duplicación durante la inicialización.");

                field.SetMap(false);
                Check(RiftCiudadDelSolExteriorMap.Load(), "HUD: falta el atlas exterior aprobado de Ciudad del Sol.");
                Canvas.willRenderCanvases += PresentContext;
                AssemblyReloadEvents.beforeAssemblyReload += Dispose;
                PresentContext();
                Debug.Log("CIUDAD_HUD Preparación completa: 1 RiftGameplayHUD + 1 RiftFieldUI originales; HUD básico conservado. " +
                    "Cartografía exterior aprobada habilitada en los mapas originales. No se cambiaron Prototype, habilidades ni servicios de red. Validación visual pendiente.");
            }

            T InstantiateOriginal<T>(GameObject prefab) where T : Component
            {
                var instance = Object.Instantiate(prefab);
                created.Add(instance);
                instance.name = "TEMP Ciudad del Sol · " + prefab.name;
                SceneManager.MoveGameObjectToScene(instance, SceneManager.GetActiveScene());
                return instance.GetComponent<T>();
            }

            void PresentContext()
            {
                if (disposed || !hud || !field || SceneManager.GetActiveScene().path != City) return;
                if (hud.Zone) hud.Zone.text = "CIUDAD DEL SOL";
                if (hud.Checkpoint) hud.Checkpoint.text = "Ensayo Raid local";
                if (hud.MapZone) hud.MapZone.text = "CIUDAD DEL SOL";
            }

            public void Dispose()
            {
                if (disposed) return;
                disposed = true;
                Canvas.willRenderCanvases -= PresentContext;
                AssemblyReloadEvents.beforeAssemblyReload -= Dispose;
                for (int i = restore.Count - 1; i >= 0; i--) restore[i]();
                foreach (var item in created) if (item) Object.Destroy(item);
                restore.Clear(); created.Clear();
            }
        }
    }
}
