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
        const string PopulationAsset = "Assets/ROM/Resources/CiudadDelSol/PopulationStage1.asset";
        static CiudadDelSolPopulation developmentPopulation;

        static bool StartDevelopmentPopulation()
        {
            GameObject root = null;
            try
            {
                Check(developmentPopulation == null && demonTrial != null && demonTrial.Enemy && oracleTrial != null && oracleTrial.Enemy,
                    "Etapa 1 requiere los dos enemigos validados; no se iniciará una población parcial.");
                var config = AssetDatabase.LoadAssetAtPath<CiudadDelSolPopulationDefinition>(PopulationAsset);
                Check(config, "Falta la configuración persistente PopulationStage1.");
                root = new GameObject("TEMP Ciudad del Sol · Población etapa 1");
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
                Debug.Log("CIUDAD_POBLACION ETAPA_1_PREPARADA: 13 recursos compatibles auditados; 2 actores adoptados sin duplicarlos. " +
                    "Diablillo 90 s, Oráculo 120 s; máximo global 2, máximo 1 por sector. Falta validación funcional en Play.");
                return true;
            }
            catch (Exception error)
            {
                if (root) Object.Destroy(root);
                Debug.LogError("CIUDAD_POBLACION ETAPA_1_NO_INICIADA: " + error.Message +
                    " No se avanzó a la distribución exterior ni interior.");
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
