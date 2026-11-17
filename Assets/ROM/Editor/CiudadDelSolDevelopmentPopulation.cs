using System;
using System.Collections.Generic;
using ROM.CiudadDelSol;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftEnemies;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        const string PopulationAsset = "Assets/ROM/Resources/CiudadDelSol/PopulationExterior.asset";
        static CiudadDelSolPopulation developmentPopulation;

        static bool StartDevelopmentPopulation()
        {
            GameObject root = null;
            try
            {
                Check(developmentPopulation == null && demonTrial != null && demonTrial.Enemy && oracleTrial != null && oracleTrial.Enemy,
                    "El exterior requiere los dos actores iniciales validados; no se duplicarán ni sustituirán.");
                var config = AssetDatabase.LoadAssetAtPath<CiudadDelSolPopulationDefinition>(PopulationAsset);
                Check(config, "Falta la configuración persistente PopulationExterior.");
                root = new GameObject("TEMP Ciudad del Sol · Población exterior");
                SceneManager.MoveGameObjectToScene(root, SceneManager.GetActiveScene());
                var population = root.AddComponent<CiudadDelSolPopulation>();
                population.Initialize(config, new Dictionary<string, RiftEnemyAgent>
                {
                    ["entrada-diablillo-01"] = demonTrial.Enemy,
                    ["jardines-oraculo-01"] = oracleTrial.Enemy
                });
                developmentPopulation = population;
                // Transfer lifecycle ownership, without destroying or recreating either validated actor.
                EditorApplication.update -= ObserveDemonTrial;
                EditorApplication.playModeStateChanged -= DemonPlayChanged;
                AssemblyReloadEvents.beforeAssemblyReload -= StopDemonTrial;
                demonTrial = null;
                Health.ServerDamaged -= ObserveOracleDamage;
                oracleTrial = null;
                Debug.Log($"CIUDAD_POBLACION EXTERIOR_PREPARADO: {config.CompatibleEnemies.Length} recursos, {config.Posts.Length} puestos, " +
                    $"{config.Sectors.Length} sectores, máximo global {config.GlobalMaximum}; 2 actores adoptados sin duplicarlos. " +
                    "Diablillo 90 s y Oráculo 120 s conservados. Cada grupo espera su validación física/NavMesh antes de aparecer.");
                return true;
            }
            catch (Exception error)
            {
                if (root) Object.Destroy(root);
                Debug.LogError("CIUDAD_POBLACION EXTERIOR_NO_INICIADO: " + error.Message + " No se crearon enemigos adicionales ni se intervino el interior.");
                return false;
            }
        }

        static void StopDevelopmentPopulation()
        {
            var population = developmentPopulation; developmentPopulation = null;
            if (!population) return;
            try { population.Shutdown(); }
            finally { Object.Destroy(population.gameObject); }
        }
    }
}
