using System;
using System.Collections.Generic;
using System.Reflection;
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
            UnityEngine.UI.Text mapButtonLabel;
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
                var atlasField = typeof(RiftFieldUI).GetField("atlas", BindingFlags.Instance | BindingFlags.NonPublic);
                var originalAtlasField = typeof(RiftFieldUI).GetField("originalAtlas", BindingFlags.Instance | BindingFlags.NonPublic);
                Check(atlasField != null && originalAtlasField != null, "HUD: cambió la API interna de cartografía; se detuvo la adaptación temporal.");
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
                var oldAtlas = atlasField.GetValue(field);
                var oldOriginalAtlas = originalAtlasField.GetValue(field);
                restore.Add(() => { if (field) { atlasField.SetValue(field, oldAtlas); originalAtlasField.SetValue(field, oldOriginalAtlas); } });
                // Null atlas makes the existing SetMap guard reject both button and M, without changing bindings.
                // This is instance-only; shared prefabs, resource atlases and runtime scripts are untouched.
                atlasField.SetValue(field, null);
                originalAtlasField.SetValue(field, null);
                var mapImage = field.MapImage;
                var oldTexture = mapImage.texture;
                restore.Add(() => { if (mapImage) mapImage.texture = oldTexture; });
                mapImage.texture = null;

                var minimap = hud.Minimap;
                var miniImage = minimap.Map;
                var arrow = minimap.Arrow;
                var oldArrowActive = arrow && arrow.gameObject.activeSelf;
                restore.Add(() => { if (minimap) minimap.Map = miniImage; if (arrow) arrow.gameObject.SetActive(oldArrowActive); });
                // Refresh is called directly by the HUD even when the minimap component is disabled.
                // Its existing null-Map guard is the safe, local way to suppress the unrelated atlas.
                minimap.Map = null;
                if (miniImage)
                {
                    var oldMiniTexture = miniImage.texture;
                    var oldMiniColor = miniImage.color;
                    var oldRaycast = miniImage.raycastTarget;
                    restore.Add(() => { if (miniImage) { miniImage.texture = oldMiniTexture; miniImage.color = oldMiniColor; miniImage.raycastTarget = oldRaycast; } });
                    miniImage.texture = null; miniImage.color = new Color(.04f, .035f, .06f, 1); miniImage.raycastTarget = false;
                    var labelObject = new GameObject("Cartografía pendiente · ensayo Ciudad del Sol", typeof(RectTransform), typeof(UnityEngine.UI.Text));
                    created.Add(labelObject);
                    var rect = (RectTransform)labelObject.transform;
                    rect.SetParent(miniImage.transform, false);
                    rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.offsetMin = rect.offsetMax = Vector2.zero;
                    var label = labelObject.GetComponent<UnityEngine.UI.Text>();
                    label.font = hud.Theme.BodyFont; label.fontSize = 13; label.color = hud.Theme.Text;
                    label.alignment = TextAnchor.MiddleCenter; label.raycastTarget = false;
                    label.text = "CIUDAD DEL SOL\nCartografía pendiente";
                }
                if (arrow) arrow.gameObject.SetActive(false);
                mapButtonLabel = field.MapButton.GetComponentInChildren<UnityEngine.UI.Text>(true);
                var wasInteractable = field.MapButton.interactable;
                restore.Add(() => { if (field && field.MapButton) field.MapButton.interactable = wasInteractable; });
                Canvas.willRenderCanvases += PresentContext;
                AssemblyReloadEvents.beforeAssemblyReload += Dispose;
                PresentContext();
                Debug.Log("CIUDAD_HUD Preparación completa: 1 RiftGameplayHUD + 1 RiftFieldUI originales; HUD básico conservado. " +
                    "Cartografía deshabilitada y marcada pendiente. No se cambiaron Prototype, habilidades ni servicios de red. Validación visual pendiente.");
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
                if (hud.MapZone) hud.MapZone.text = "Cartografía pendiente";
                field.MapButton.interactable = false;
                if (mapButtonLabel) mapButtonLabel.text = "MAPA PENDIENTE";
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
