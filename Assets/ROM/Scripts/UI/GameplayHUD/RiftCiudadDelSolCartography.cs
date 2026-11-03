using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.UI.Gameplay {
// Presentation-only context. The additive QA environment retains its original design origin.
// No scene changes, collider queries, network calls or modifications to the source atlases.
public static class RiftCiudadDelSolCartography {
 public const string TrialScene = "TEMP_Nucleo_Manual_Warrior_NO_GUARDAR";
 const string ResourcesRoot = "CiudadDelSolNucleo/Revision_20261030_214538/";
 static readonly string[] AtlasNames = {
  "CiudadDelSol_01_Superior_Y16_Atlas", "CiudadDelSol_02_Intermedia_Y24_Atlas",
  "CiudadDelSol_03_Intermedia_Y30_Atlas", "CiudadDelSol_04_Profunda_Y34_Y38_Atlas"
 };
 public static readonly string[] FloorNames = {
  "Galerías superiores · Y=-16", "Nivel intermedio · Y=-24",
  "Terrazas inferiores · Y=-30", "Planta profunda · Y=-34 / -38"
 };
 static Transform root;
 static RiftMinimapAtlas[] atlases;
 static float nextSearch, pendingSince;
 static int current=-1, pending=-1, frame=-1;
 static bool active;
 static Vector3 sampledWorld;
 public static int AutomaticFloor => current;
 public static int ManualFloor { get; private set; } = -1;
 public static int DisplayFloor => ManualFloor < 0 ? current : ManualFloor;
 public static bool Active => active && root;
 public static Vector3 Offset => root ? root.position : Vector3.zero;

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void Reset() { root=null; atlases=null; nextSearch=0; current=pending=frame=-1; ManualFloor=-1; active=false; }

 static void FindContext() {
  if(root || Time.unscaledTime < nextSearch)return;
  nextSearch=Time.unscaledTime+.5f;
  var scene=SceneManager.GetSceneByName(TrialScene);
  if(!scene.IsValid() || !scene.isLoaded)return;
  foreach(var candidate in scene.GetRootGameObjects()) {
   if(candidate.name!="Environment" || !candidate.activeInHierarchy)continue;
   // This is the unrotated, unscaled, approved Environment snapshot used by the manual setup.
   if(Quaternion.Angle(candidate.transform.rotation,Quaternion.identity)>.01f ||
      (candidate.transform.lossyScale-Vector3.one).sqrMagnitude>.0001f)continue;
   root=candidate.transform; break;
  }
 }

 public static RiftMinimapAtlas Atlas(int floor) {
  if(floor<0 || floor>3)return null;
  if(atlases==null) {
   atlases=new RiftMinimapAtlas[4];
   for(int i=0;i<4;i++)atlases[i]=Resources.Load<RiftMinimapAtlas>(ResourcesRoot+AtlasNames[i]);
  }
  for(int i=0;i<4;i++)if(!atlases[i] || !atlases[i].Map ||
      (atlases[i].Center-new Vector2(81,30)).sqrMagnitude>.001f ||
      (atlases[i].WorldSize-new Vector2(234,180)).sqrMagnitude>.001f) {
    Debug.LogError("CIUDAD_MINIMAPA: atlas independiente ausente o sin calibrar: "+AtlasNames[i]);
    return null;
   }
  return atlases[floor];
 }

 public static bool InV3(Vector3 local) => local.x>=-24 && local.x<=60 && local.z>=-24 && local.z<=60;
 public static int FloorAt(Vector3 local) {
  if(local.y>=-20)return 0;
  if(local.y>=-27)return 1;
  return local.y >= (InV3(local)?-34:-32) ? 2 : 3;
 }
 static float Boundary(int lowerFloor,Vector3 local) => lowerFloor==1?-20:lowerFloor==2?-27:InV3(local)?-34:-32;

 public static bool Sample(Vector3 world,out Vector3 local) {
  FindContext(); local=root?world-root.position:world;
  if(frame==Time.frameCount && world==sampledWorld)return Active;
  frame=Time.frameCount; sampledWorld=world;
  bool inside=root && local.x>=-36 && local.x<=198 && local.z>=-60 && local.z<=120 && local.y>=-43 && local.y<=9;
  if(!inside) { active=false;current=pending=-1;ManualFloor=-1;return false; }
  if(!Atlas(FloorAt(local))) { active=false;return false; }
  AdvanceFloor(local,Time.unscaledTime);
  active=true;return true;
 }
 // Explicit clock keeps the hysteresis reproducible in the read-only Editor validation.
 static void AdvanceFloor(Vector3 local,float now) {
  int wanted=FloorAt(local);
  if(!active || current<0) { current=wanted;pending=-1; }
  else if(wanted!=current) {
   // 0.65 m Schmitt margin + 0.4 s dwell. Shared by both maps, independent of UI refresh order.
   bool crossed=wanted>current ? local.y<Boundary(current+1,local)-.65f : local.y>Boundary(current,local)+.65f;
   if(!crossed)pending=-1;
   else if(pending!=wanted) { pending=wanted;pendingSince=now; }
   else if(now-pendingSince>=.4f) { current=wanted;pending=-1; }
  } else pending=-1;
  active=true;
 }
 public static void SelectFloor(int floor) { ManualFloor=active && floor>=0 && floor<4 ? floor : -1; }
 public static Vector3 ToLocal(Vector3 world) => world-Offset;
 public static Vector3 ToWorld(Vector3 local) => local+Offset;
 public static string CurrentName => current<0 ? "" : FloorNames[current];
 public static string DisplayName => DisplayFloor<0 ? "" : FloorNames[DisplayFloor];
}
}
