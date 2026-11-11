using System;
using MultiFPS;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string WarriorPrefab = "Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
        const string BridgeRoot = "Salida_Meridional_Generated";
        const string BridgeRoute = "03 - Descanso y puente inferior";
        static readonly Vector3 BridgeSurface = new Vector3(30f, -2f, -206f);
        static readonly Vector3 BridgeModulePivot = new Vector3(30f, -4f, -206f);

        [MenuItem("ROM/Dungeon Ciudad del Sol/Prueba directa Warrior/Validar apoyo en puente")]
        static void VerifyWarriorBridge()
        {
            try
            {
                ValidateWarriorBridge();
            }
            catch (InvalidOperationException error)
            {
                Debug.LogError("Ciudad del Sol. Validación de puente interrumpida. " + error.Message);
            }
        }

        static Vector3 ValidateWarriorBridge()
        {
            var scene = SceneManager.GetActiveScene();
            Check(EditorApplication.isPlaying && !EditorApplication.isPaused &&
                scene.path == City && scene.isLoaded && SceneManager.sceneCount == 1 && pending == null,
                "Cargá Ciudad del Sol con la prueba de carga existente y dejá Play sin pausa.");
            Check(!NetworkBusy(), "Validá el puente antes de iniciar una sesión de Mirror.");

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(WarriorPrefab);
            var controller = prefab ? prefab.GetComponent<CharacterController>() : null;
            Check(controller && controller.transform.localScale == Vector3.one,
                "No se pudo obtener la cápsula raíz del Warrior a escala unitaria.");

            var physics = scene.GetPhysicsScene();
            var origin = BridgeSurface + Vector3.up * 2f;
            Check(physics.Raycast(origin, Vector3.down, out var hit, 4f, ~0, QueryTriggerInteraction.Ignore),
                $"No se encontró apoyo sólido en los 4 m bajo {origin:F3}.");
            int boardLayer = hit.collider.gameObject.layer;
            // CharacterInstance and Health assign the character layer at runtime.
            const int warriorLayer = (int)GameLayers.character;
            float slope = Vector3.Angle(hit.normal, Vector3.up);
            string support = $"{hit.collider.name} ({hit.collider.GetType().Name}), " +
                $"capa {LayerMask.LayerToName(boardLayer)} ({boardLayer}), impacto {hit.point:F3}, " +
                $"normal {hit.normal:F3}, pendiente {slope:F2}°.";
            var route = hit.transform.parent;
            // The hierarchy and world pivot identify scene prefab instance 654971598.
            Check(hit.collider is BoxCollider && hit.collider.gameObject.scene == scene &&
                hit.transform.name == "Platform_Base_01" && route && route.name == BridgeRoute &&
                route.parent && route.parent.name == BridgeRoot && !route.parent.parent &&
                Vector3.Distance(hit.transform.position, BridgeModulePivot) < .001f,
                "El primer collider bajo la referencia no es el módulo central del extremo sur. " + support);
            Check(Mathf.Abs(hit.point.y - BridgeSurface.y) < .001f,
                $"El tablero difiere de la cota prevista ({BridgeSurface.y:F3} m). {support}");
            Check((GameManager.environmentLayer & (1 << boardLayer)) != 0,
                $"El tablero queda fuera de la máscara de suelo 0x{GameManager.environmentLayer:X}. {support}");
            Check(!Physics.GetIgnoreLayerCollision(warriorLayer, boardLayer),
                $"La matriz de física ignora el contacto con el Warrior en su capa de ejecución " +
                $"{LayerMask.LayerToName(warriorLayer)} ({warriorLayer}). {support}");
            Check(slope <= controller.slopeLimit,
                $"La pendiente excede el límite del Warrior ({controller.slopeLimit:F2}°). {support}");

            // Check the approved spawn itself, including its fixed floor clearance.
            var spawn = BridgeSpawn;
            var center = spawn + Quaternion.Euler(0f, BridgeYaw, 0f) * controller.center;
            var axis = Vector3.up * (controller.height * .5f - controller.radius);
            var overlaps = new Collider[1];
            int count = physics.OverlapCapsule(center - axis, center + axis, controller.radius,
                overlaps, ~0, QueryTriggerInteraction.Ignore);
            Check(count == 0, "La cápsula invade un collider sólido: " +
                (count > 0 ? $"{overlaps[0].name} (capa {overlaps[0].gameObject.layer}) en la aparición {spawn:F3}." : ""));

            Debug.Log($"Ciudad del Sol. Apoyo confirmado en {support} " +
                $"Contacto con Warrior en capa {warriorLayer} habilitado. " +
                $"Aparición propuesta {spawn:F3}, giro Y={BridgeYaw}°. " +
                $"Cápsula despejada, radio {controller.radius:F2} m y altura {controller.height:F2} m.");
            return spawn;
        }
    }
}
