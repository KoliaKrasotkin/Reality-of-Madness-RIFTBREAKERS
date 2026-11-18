using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    public static partial class CiudadDelSolInteriorStageTwo
    {
        const string Work5="Backups/CiudadDelSol/Interior/Etapa5_20261118";
        static Plan[] StageFivePlans()=>new[]{
            P("VS04","B01","B03",V(51,-24,39),V(45,-24,39),V(18,-24,39),V(-15,-24,39)),
            P("VS05","B02","B04",V(24,-30,45),V(21,-30,45),V(21,-30,39),V(21,-30,-3),V(21,-30,-9))
        };
        public static void InspectStageFiveBatch()=>InspectStageFive();
        [MenuItem("ROM/Etapa 5 interior/Recibir VS04 VS05 sin obras")]
        static void InspectStageFive()
        {
            if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Recepción exclusiva Edit Mode.");
            if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("No se inspeccionan ni sustituyen escenas con cambios pendientes.");
            if(!SceneManager.GetSceneByPath(City).isLoaded)EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
            Directory.CreateDirectory(Work5);
            var r=InspectSelected(StageFivePlans(),Work5+"/Inicial","Etapa 5 - inicial",false,true);
            File.WriteAllText(Work5+"/Estado.txt",r.connections.Count(c=>c.technicalIndividual)+"/2 conexiones recibidas; no se guardó escena ni navegación.");
        }
    }
}
