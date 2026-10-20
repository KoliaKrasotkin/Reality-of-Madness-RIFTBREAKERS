using System;
using System.IO;
using System.Linq;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
namespace ROM.EditorTools {
public static class TrainingGroundBuilder {
public static void Build(){
var scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Rift);var mode=UnityEngine.Object.FindFirstObjectByType<ROM.Horde.Horde>();mode.ClassConfiguration=RiftClassRules.Load();
var old=GameObject.Find("Refugio - Entrenamiento");if(old)UnityEngine.Object.DestroyImmediate(old);
var root=new GameObject("Refugio - Entrenamiento").transform;
var spawns=UnityEngine.Object.FindFirstObjectByType<SpawnpointsContainer>();var first=spawns.Spawnpoints.First();
Physics.SyncTransforms();NavMeshHit hit=default;bool found=false;
for(int n=0;n<72;n++){
    float angle=(n%24)*15;float distance=10+(n/24)*4;
    Vector3 desired=first.position+Quaternion.Euler(0,angle,0)*first.forward*distance;
    if(!NavMesh.SamplePosition(desired,out hit,2,NavMesh.AllAreas))continue;
    if(Physics.CheckCapsule(hit.position+Vector3.up*.6f,hit.position+Vector3.up*2,.4f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))continue;
    Vector3 ray=hit.position+Vector3.up-first.position-Vector3.up*1.3f;
    if(Physics.Raycast(first.position+Vector3.up*1.3f,ray.normalized,ray.magnitude,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore))continue;
    found=true;break;
}
if(!found)throw new Exception("Training ground needs a clear firing lane from spawn");
var spot=new GameObject("Training dummy spawn").transform;spot.SetParent(root);spot.position=hit.position+Vector3.up*.1f;spot.rotation=Quaternion.LookRotation(first.position-spot.position);mode.TrainingSpot=spot;
if(!mode.IsSafe(spot.position))throw new Exception("Dummy must stay in initial safe zone");
var mat=AssetDatabase.LoadAssetAtPath<Material>("Assets/ROM/Gameplay/Classes/TrainingGold.mat");if(!mat){mat=new Material(Shader.Find("Universal Render Pipeline/Unlit"));mat.color=new Color(1,.65f,.12f);AssetDatabase.CreateAsset(mat,"Assets/ROM/Gameplay/Classes/TrainingGold.mat");}
var ring=new GameObject("Training circle",typeof(LineRenderer)).GetComponent<LineRenderer>();ring.transform.SetParent(root);ring.sharedMaterial=mat;ring.loop=true;ring.widthMultiplier=.12f;ring.positionCount=64;
for(int i=0;i<64;i++){float a=i*Mathf.PI*2/64;ring.SetPosition(i,spot.position+new Vector3(Mathf.Cos(a)*7,.05f,Mathf.Sin(a)*7));}
EditorUtility.SetDirty(mode);EditorSceneManager.SaveScene(scene);
Vector3 savedPosition=spot.position;scene=EditorSceneManager.OpenScene(ProjectSceneCleanup.Menu);var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();hub.Build();EditorUtility.SetDirty(hub);EditorSceneManager.SaveScene(scene);AssetDatabase.SaveAssets();Directory.CreateDirectory("Documentation/training");File.WriteAllText("Documentation/training/layout.txt","Training dummy at "+savedPosition+"; class selection on Home; independent ability overlay.");
}
public static void Run(){try{Build();TrainingGroundProbe.Run();}catch(Exception e){Debug.LogException(e);EditorApplication.Exit(1);}}
}
}
