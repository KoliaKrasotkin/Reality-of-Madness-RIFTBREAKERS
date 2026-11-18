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
        const string Request5=Work5+"/Recepcion.pending";
        static double deadline5;
        [InitializeOnLoadMethod]
        static void InstallRequest5()
        {
            if(Application.isBatchMode||!File.Exists(Request5))return;
            deadline5=EditorApplication.timeSinceStartup+120;
            EditorApplication.update-=RunRequest5;EditorApplication.update+=RunRequest5;
        }
        static void RunRequest5()
        {
            if(!File.Exists(Request5)){EditorApplication.update-=RunRequest5;return;}
            if(EditorApplication.isCompiling||EditorApplication.isUpdating)return;
            EditorApplication.update-=RunRequest5;
            var nonce=File.ReadAllText(Request5).Trim();if(!Guid.TryParse(nonce,out _))return;
            File.Move(Request5,Request5+".consumed."+nonce);
            try{if(EditorApplication.timeSinceStartup>deadline5)throw new TimeoutException("Recepción no iniciada a tiempo.");InspectStageFive();}
            catch(Exception e){File.WriteAllText(Work5+"/Error_solicitud.txt",e.ToString());Debug.LogException(e);}
        }
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
            File.WriteAllText(Work5+"/RegresionesIniciales.txt",ConstructionReception4(Work5+"/RegresionesIniciales",false).ToString());
        }
    }
}
