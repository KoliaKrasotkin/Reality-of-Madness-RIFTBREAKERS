using MultiFPS.Gameplay;
using MultiFPS.UI;
using UnityEngine;

namespace ROM.UI.Gameplay {
// Stable presentation resource. The reference pins the approved revision; no runtime asset searches.
public sealed class RiftCiudadDelSolExteriorMap : ScriptableObject {
 public RiftMinimapAtlas Atlas;
 public const string ScenePath = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
 const string ResourcePath = "CiudadDelSolExteriorMap";
 static RiftCiudadDelSolExteriorMap configuration;
 static bool reportedMissing;

 [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
 static void Reset(){configuration=null;reportedMissing=false;}

 public static bool IsLocalContext(CharacterInstance actor){
  var player=ClientFrontend.ClientPlayerInstance;
  return actor && player && player.MyCharacter==actor && actor.gameObject.scene.isLoaded &&
   actor.gameObject.scene.path==ScenePath;
 }

 public static RiftMinimapAtlas Load(){
  if(!configuration)configuration=Resources.Load<RiftCiudadDelSolExteriorMap>(ResourcePath);
  var atlas=configuration?configuration.Atlas:null;
  if(atlas && atlas.Map && atlas.WorldSize.x>0 && atlas.WorldSize.y>0)return atlas;
  if(!reportedMissing){reportedMissing=true;Debug.LogError("CIUDAD_MAPA: falta la referencia estable al atlas exterior aprobado. No se sustituirá por El Rift ni por las plantas interiores.");}
  return null;
 }
}
}
