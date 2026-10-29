using System.Collections.Generic;using System.Linq;using UnityEngine;using UnityEngine.SceneManagement;
namespace ROM.Rendering {
 public sealed class RefugeTownRuralBinding:MonoBehaviour {
  public bool ReplaceOriginalDwellings=true,HideUrbanPaving=true;
  readonly Dictionary<GameObject,bool> objects=new Dictionary<GameObject,bool>();readonly Dictionary<Renderer,bool> renderers=new Dictionary<Renderer,bool>();
  public int HiddenHouses{get;private set;}public int HiddenPaving{get;private set;}
  void OnEnable(){if(!Application.isPlaying)return;var scene=SceneManager.GetActiveScene();if(scene.name!="map_rom_horde")return;var village=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Refugio - Aldea");if(ReplaceOriginalDwellings&&village)foreach(Transform t in village.transform)if(t.name.StartsWith("Vivienda ")){objects[t.gameObject]=t.gameObject.activeSelf;t.gameObject.SetActive(false);HiddenHouses++;}if(HideUrbanPaving){var level=scene.GetRootGameObjects().FirstOrDefault(g=>g.name=="Level");if(level)foreach(var r in level.GetComponentsInChildren<MeshRenderer>(true))if(new Vector2(r.bounds.center.x,r.bounds.center.z).magnitude<75&&(r.name=="Floor"||r.name=="ArsenalDais"||r.name=="Pad"||r.name.StartsWith("Bridge_"))){renderers[r]=r.enabled;r.enabled=false;HiddenPaving++;}}}
  public void Restore(){foreach(var pair in objects)if(pair.Key)pair.Key.SetActive(pair.Value);objects.Clear();foreach(var pair in renderers)if(pair.Key)pair.Key.enabled=pair.Value;renderers.Clear();HiddenHouses=0;HiddenPaving=0;}
  void OnDisable(){Restore();}void OnDestroy(){Restore();}
 }
}
