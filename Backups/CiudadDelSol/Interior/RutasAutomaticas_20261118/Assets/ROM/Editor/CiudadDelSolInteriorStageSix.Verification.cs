using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ROM.EditorTools
{
 public static partial class CiudadDelSolInteriorStageTwo
 {
  static bool sceneReadyForSavedSix()=>File.Exists(Work6+"/Prueba_navegacion.json")&&SceneManager.GetSceneByPath(City).GetRootGameObjects().Any(g=>g.name==Root6);
  static void VerifySavedSix()
  {
   var construction=JsonUtility.FromJson<Build4>(File.ReadAllText(Work6+"/Construccion.json"));
   if(!construction.saved||Hash(City)!=construction.sceneAfter||Hash(Nav)!=construction.navAfter)throw new InvalidOperationException("Datos persistidos distintos de la construccion recibida.");
   var proof=JsonUtility.FromJson<Proof4>(File.ReadAllText(Work6+"/Prueba_navegacion.json"));
   EditorSceneManager.OpenScene(City,OpenSceneMode.Single);
   bool restored=TriangleDigest4()==proof.digest;
   bool regressions=ReceiveAllSix(Work6+"/Recepcion_recarga",false);
   bool agentClear=CheckHumanoidSix(Work6+"/Recepcion_recarga");
   bool bounds=StageSixPlan().points.All(p=>InsideCorridor(p,StageSixPlan()))&&StageSixPlan().points.All(p=>!InsideCorridor(p+Vector3.up*4,StageSixPlan())&&!InsideCorridor(p+Vector3.down*4,StageSixPlan()));
   // Regression for the historical hard-coded -34.5 floor limit and other levels.
   File.WriteAllText(Work6+"/Regresion_cotas.txt","Puntos originales aceptados y plantas +/-4 m rechazadas="+bounds);
   construction.confirmedReload=restored&&regressions&&agentClear&&bounds;
   File.WriteAllText(Work6+"/Construccion.json",JsonUtility.ToJson(construction,true));
   File.WriteAllText(Work6+"/Resultado_final.txt","Recarga="+restored+"; Recepciones="+regressions+"; CapsulaHumanoid="+agentClear+"; Cotas="+bounds+"; EscenaLimpia="+!SceneManager.GetSceneByPath(City).isDirty);
   var scene=SceneManager.GetSceneByPath(City);
   File.WriteAllText(Work6+"/Componentes.txt","Superficies="+scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Unity.AI.Navigation.NavMeshSurface>(true).Length)+"; Modificadores="+scene.GetRootGameObjects().Sum(g=>g.GetComponentsInChildren<Unity.AI.Navigation.NavMeshModifier>(true).Length)+"; VolumenesEtapa6="+scene.GetRootGameObjects().Single(g=>g.name==Root6).GetComponentsInChildren<Unity.AI.Navigation.NavMeshModifierVolume>(true).Length);
   if(construction.confirmedReload)Debug.Log("CIUDAD_INTERIOR_ETAPA6_FINAL: VS03 recibida; NavMesh y PhysX bidireccionales; capsulas Warrior/Humanoid; regresiones Etapas 1-5; cuatro jugadores reales pendientes.");
   if(!construction.confirmedReload)throw new InvalidOperationException("Recepcion tras recarga pendiente. Consultar Resultado_final.");
  }
  static bool CheckHumanoidSix(string folder)
  {
   var cfg=ConstructionConfig4(SceneManager.GetSceneByPath(City));var agent=NavMesh.GetSettingsByID(0);
   cfg.radius=agent.agentRadius;cfg.height=Mathf.Max(cfg.height,agent.agentHeight);
   string globalFile=folder+"/GlobalDirecta/Recepcion.json";
   var global=JsonUtility.FromJson<Report>(File.ReadAllText(globalFile));
   var forward=InspectTrace("Capsula Humanoid real ida",global.connections[0].forward.corners,StageSixPlan(),cfg,false);
   var reverse=InspectTrace("Capsula Humanoid real vuelta",global.connections[0].reverse.corners,StageSixPlan(),cfg,false);
   File.WriteAllText(folder+"/Capsula_Humanoid_ida.json",JsonUtility.ToJson(forward,true));
   File.WriteAllText(folder+"/Capsula_Humanoid_vuelta.json",JsonUtility.ToJson(reverse,true));
   bool agentClear=forward.clear&&reverse.clear&&forward.missingNav==0&&reverse.missingNav==0;
   return agentClear;
  }
 }
}
