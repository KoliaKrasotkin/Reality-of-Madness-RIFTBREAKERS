using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using ROM.UI.Gameplay;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ROM.EditorTools {
// Explicit, read-only validation. No Play transitions, character spawning or scene saves.
public static class CiudadDelSolMinimapValidation {
 const string Menu="ROM/Dungeon Ciudad del Sol/Cartografia por plantas/Validar integracion de minimapa (sin Play)";
 const BindingFlags Hidden=BindingFlags.Static|BindingFlags.NonPublic;
 static readonly Type Context=typeof(RiftCiudadDelSolCartography);
 static void Require(bool condition,string message){if(!condition)throw new InvalidOperationException(message);}
 static void Clock(Vector3 local,float time)=>Context.GetMethod("AdvanceFloor",Hidden).Invoke(null,new object[]{local,time});
 static void Reset()=>Context.GetMethod("Reset",Hidden).Invoke(null,null);
 static string Hash(string path){using(var sha=SHA256.Create())using(var file=File.OpenRead(path))return BitConverter.ToString(sha.ComputeHash(file));}

 [MenuItem(Menu,true)]
 static bool CanRun()=>!EditorApplication.isPlayingOrWillChangePlaymode&&!EditorApplication.isCompiling;

 [MenuItem(Menu)]
 public static void Run(){
  Require(!EditorApplication.isPlayingOrWillChangePlaymode,"Ejecutar fuera de Play; no interrumpir el ensayo.");
  string[] protectedFiles={"Assets/Scenes/Dungeon_Isla_del_Sol.unity","Assets/ROM/UI/GameplayHUD/Resources/RiftMinimapAtlas.asset","Assets/ROM/UI/GameplayHUD/Resources/RiftMinimap.png"};
  string[] hashes=Array.ConvertAll(protectedFiles,Hash);
  var activeScene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
  var selected=Selection.activeObject;
  int count=UnityEngine.SceneManagement.SceneManager.sceneCount;
  var scenes=new UnityEngine.SceneManagement.Scene[count];var dirty=new bool[count];var roots=new int[count];
  for(int i=0;i<count;i++){scenes[i]=UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);dirty[i]=scenes[i].isDirty;roots[i]=scenes[i].rootCount;}
  var preview=EditorSceneManager.NewPreviewScene();
  try {
   Reset();
   for(int i=0;i<4;i++){
    var atlas=RiftCiudadDelSolCartography.Atlas(i);
    Require(atlas&&atlas.Map,"No se pudo cargar atlas "+i);
    Require(atlas.Map.width==2048&&atlas.Map.height==1576,"Resolucion/aspecto de atlas alterado: "+i);
    Require(atlas.Center==new Vector2(81,30)&&atlas.WorldSize==new Vector2(234,180),"Calibracion distinta: "+i);
    var uv=atlas.View(new Vector3(81,0,30),12);
    Require((uv.center-new Vector2(.5f,.5f)).sqrMagnitude<.000001f,"Centro UV incorrecto.");
    Require(Mathf.Abs(uv.width*234-uv.height*180)<.001f,"Escala X/Z distinta en minimapa.");
   }
   Vector3 east=new Vector3(96,-16,39);
   Clock(east,0);Require(RiftCiudadDelSolCartography.AutomaticFloor==0,"Planta superior.");
   Clock(new Vector3(96,-20.3f,39),1);Clock(new Vector3(96,-20.3f,39),2);
   Require(RiftCiudadDelSolCartography.AutomaticFloor==0,"Oscilacion dentro de margen.");
   Clock(new Vector3(96,-24,39),3);Clock(new Vector3(96,-24,39),3.2f);
   Require(RiftCiudadDelSolCartography.AutomaticFloor==0,"No se respeto el tiempo de confirmacion.");
   Clock(new Vector3(96,-24,39),3.5f);Require(RiftCiudadDelSolCartography.AutomaticFloor==1,"Descenso a -24.");
   Clock(new Vector3(96,-30,39),4);Clock(new Vector3(96,-30,39),4.5f);Require(RiftCiudadDelSolCartography.AutomaticFloor==2,"Descenso a -30.");
   Clock(new Vector3(96,-34,39),5);Clock(new Vector3(96,-34,39),5.5f);Require(RiftCiudadDelSolCartography.AutomaticFloor==3,"Profunda oriental -34.");
   Clock(new Vector3(96,-30,39),6);Clock(new Vector3(96,-30,39),6.5f);Require(RiftCiudadDelSolCartography.AutomaticFloor==2,"Ascenso desde profunda.");
   Clock(new Vector3(96,-24,39),7);Clock(new Vector3(96,-24,39),7.5f);Require(RiftCiudadDelSolCartography.AutomaticFloor==1,"Ascenso a -24.");
   Clock(east,8);Clock(east,8.5f);Require(RiftCiudadDelSolCartography.AutomaticFloor==0,"Ascenso a -16.");
   Require(RiftCiudadDelSolCartography.FloorAt(new Vector3(18,-33,18))==2,"Transicion V3 -30/-38.");
   Require(RiftCiudadDelSolCartography.FloorAt(new Vector3(18,-38,18))==3,"Profunda V3 -38.");
   RiftCiudadDelSolCartography.SelectFloor(2);
   Require(RiftCiudadDelSolCartography.DisplayFloor==2&&RiftCiudadDelSolCartography.AutomaticFloor==0,"Seleccion manual altero minimapa automatico.");
   RiftCiudadDelSolCartography.SelectFloor(-1);Require(RiftCiudadDelSolCartography.DisplayFloor==0,"Regreso a automatico.");

   var root=EditorUtility.CreateGameObjectWithHideFlags("Cartography validation origin",HideFlags.HideAndDontSave);
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,preview);
   root.transform.position=new Vector3(4096,200,0);
   Context.GetField("root",Hidden).SetValue(null,root.transform);
   var world=east+root.transform.position;
   Require(RiftCiudadDelSolCartography.Sample(world,out var local)&&(local-east).sqrMagnitude<.00001f,"Compensacion de offset.");
   var host=EditorUtility.CreateGameObjectWithHideFlags("Cartography validation UI",HideFlags.HideAndDontSave,typeof(RectTransform));
   UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host,preview);
   var map=host.AddComponent<RiftFieldUI>();
   map.Content=host.GetComponent<RectTransform>();map.Content.sizeDelta=new Vector2(2340,1800);
   typeof(RiftFieldUI).GetField("atlas",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(map,RiftCiudadDelSolCartography.Atlas(0));
   typeof(RiftFieldUI).GetProperty("CityCartography").SetValue(map,true);
   var center=new Vector3(81,-16,30)+root.transform.position;
   Require(map.TryPosition(center,out var point)&&point.sqrMagnitude<.001f,"Proyeccion de posicion desplazada.");
   Require(map.TryWorldPosition(point,out var back)&&(back-center).sqrMagnitude<.001f,"Conversion inversa desplazada.");
   Require(map.Heading(center,0).y>0&&Mathf.Abs(map.Heading(center,0).x)<.001f,"Orientacion norte.");
   Require(map.Heading(center,90).x>0&&Mathf.Abs(map.Heading(center,90).y)<.001f,"Orientacion este.");
   Require(!map.TryPosition(Vector3.zero,out _),"Un punto del mapa general se proyecto en Ciudad del Sol.");
   RiftCiudadDelSolCartography.SelectFloor(1);
   Require(!RiftCiudadDelSolCartography.Sample(Vector3.zero,out _)&&RiftCiudadDelSolCartography.ManualFloor==-1,"La salida no libera contexto/manual.");
   Require(!RiftCiudadDelSolCartography.Active,"Contexto retenido fuera del ensayo.");
  } finally {Reset();EditorSceneManager.ClosePreviewScene(preview);}
  for(int i=0;i<protectedFiles.Length;i++)Require(Hash(protectedFiles[i])==hashes[i],"Origen protegido cambiado: "+protectedFiles[i]);
  Require(UnityEngine.SceneManagement.SceneManager.GetActiveScene()==activeScene,"Cambio inesperado de escena activa.");
  Require(UnityEngine.SceneManagement.SceneManager.sceneCount==count,"Cambio inesperado de escenas abiertas.");
  Require(Selection.activeObject==selected,"Cambio inesperado de seleccion.");
  for(int i=0;i<count;i++)Require(scenes[i].IsValid()&&scenes[i].isDirty==dirty[i]&&scenes[i].rootCount==roots[i],"Estado de escena abierta alterado: "+scenes[i].name);
  Debug.Log("CIUDAD_MINIMAPA_VALIDADO: cuatro recursos, escala, coordenadas ida/vuelta, offset, orientacion, sectores, histeresis subida/bajada, seleccion manual y salida. Escenas abiertas, seleccion, estado dirty, original y atlas general intactos. Copia de validacion descartada, sin Play ni guardado. Falta revision visual manual en Host.");
 }
}
}
