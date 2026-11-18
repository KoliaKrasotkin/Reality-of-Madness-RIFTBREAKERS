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
  const string Work6="Backups/CiudadDelSol/Interior/Etapa6_20261118";
  const string Request6=Work6+"/Recepcion.pending";
  [InitializeOnLoadMethod] static void InstallStageSixRequest()
  {
   if(Application.isBatchMode||!File.Exists(Request6))return;
   EditorApplication.delayCall+=ReceiveStageSixRequest;
  }
  static void ReceiveStageSixRequest()
  {
   if(!File.Exists(Request6))return;
   if(EditorApplication.isCompiling||EditorApplication.isUpdating){EditorApplication.delayCall+=ReceiveStageSixRequest;return;}
   string id=File.ReadAllText(Request6).Trim();if(!Guid.TryParse(id,out _))return;
   File.Move(Request6,Request6+".consumed."+id);
   if(File.Exists(Work6+"/Error.txt"))File.Move(Work6+"/Error.txt",Work6+"/Error_anterior_"+id+".txt");
   try{if(File.Exists(Work6+"/Margenes.pending")){File.Move(Work6+"/Margenes.pending",Work6+"/Margenes.consumed."+id);FixCornerMarginsSix();}else if(File.Exists(Work6+"/Construir.pending")){File.Move(Work6+"/Construir.pending",Work6+"/Construir.consumed."+id);ConstructStageSix();}else ReceiveStageSix();}catch(Exception e){File.WriteAllText(Work6+"/Error.txt",e.ToString());Debug.LogException(e);}
  }
  static Plan StageSixPlan()=>P("VS03","B02","Acceso D03",V(24,-30,45),V(-9,-30,45),V(-9,-30,36),V(-9,-38,12),V(-9,-38,9),V(18,-38,9));
  [MenuItem("ROM/Etapa 6 interior/Recibir VS03")]
  public static void ReceiveStageSix()
  {
   if(EditorApplication.isPlayingOrWillChangePlaymode)throw new InvalidOperationException("Recepción exclusiva Edit Mode.");
   if(Enumerable.Range(0,SceneManager.sceneCount).Any(i=>SceneManager.GetSceneAt(i).isDirty))throw new InvalidOperationException("Escena con cambios pendientes; no se sustituye ni guarda.");
   if(!SceneManager.GetSceneByPath(City).isLoaded)EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
   Directory.CreateDirectory(Work6);
   if(sceneReadyForSavedSix())VerifySavedSix();
   else
   {
    var p=new[]{StageSixPlan()};
    var initial=InspectSelected(p,Work6+"/Inicial","Etapa 6 VS03",false,true);
    File.WriteAllText(Work6+"/Resultado.txt","VS03="+Raw4(initial,1));
   }
   ShowStageSixView(0);
  }
  static readonly (string title,Vector3 eye,Vector3 target)[] Views6={
   ("Anillo norte B02",V(-2,-27,51),V(-6,-30,45)),
   ("Cabecera descenso",V(-1,-27,39),V(-9,-30,36)),
   ("Cuatro escaleras",V(4,-29,24),V(-9,-34,24)),
   ("Desembarco Y-38",V(1,-35,8),V(-9,-38,12)),
   ("Acceso D03 sin intervenir cámara",V(5,-35,16),V(9,-38,9)),
   ("Contracara occidental",V(-17,-29,25),V(-9,-34,24))
  };
  static int viewIndex6;
  static void ShowStageSixView(int index)
  {
   viewIndex6=(index+Views6.Length)%Views6.Length;
   var s=SceneManager.GetSceneByPath(City);
   SceneVisibilityManager.instance.Isolate(s.GetRootGameObjects().Where(g=>g.name=="Nucleo_Monumental_Conjunto"||g.name==BuildRoot4||g.name=="CiudadDelSol_Etapa6_VS03").ToArray(),true);
   var v=SceneView.lastActiveSceneView??EditorWindow.GetWindow<SceneView>();var x=Views6[viewIndex6];
   v.drawGizmos=false;v.cameraSettings.fieldOfView=60;v.LookAtDirect(x.target,Quaternion.LookRotation(x.target-x.eye),Vector3.Distance(x.eye,x.target)*.5f);
   SceneView.duringSceneGui-=Toolbar6;SceneView.duringSceneGui+=Toolbar6;v.Focus();v.Repaint();
  }
  static void Toolbar6(SceneView view)
  {
   Handles.BeginGUI();GUILayout.BeginArea(new Rect(60,42,850,60),EditorStyles.helpBox);GUILayout.BeginHorizontal();
   if(GUILayout.Button("Anterior",GUILayout.Width(90)))ShowStageSixView(viewIndex6-1);
   if(GUILayout.Button("Siguiente",GUILayout.Width(90)))ShowStageSixView(viewIndex6+1);
   if(GUILayout.Button("Restaurar visibilidad",GUILayout.Width(160))){SceneVisibilityManager.instance.ExitIsolation();SceneView.duringSceneGui-=Toolbar6;SceneView.RepaintAll();}
   GUILayout.Label(Views6[viewIndex6].title);GUILayout.EndHorizontal();GUILayout.EndArea();Handles.EndGUI();
  }
 }
}
