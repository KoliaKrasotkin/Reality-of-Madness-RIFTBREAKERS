using System.Collections.Generic;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
public sealed class CombatVfxPool:MonoBehaviour {
 static CombatVfxPool instance;readonly Dictionary<GameObject,Stack<GameObject>> pool=new Dictionary<GameObject,Stack<GameObject>>();
 struct Entry {public GameObject Key,Object;public float End;}readonly List<Entry> active=new List<Entry>();
 public static void Spawn(GameObject prefab,Vector3 position,Quaternion rotation,float lifetime){if(!prefab||Application.isBatchMode&&!Mirror.NetworkClient.active)return;if(!instance){var go=new GameObject("ROM - pooled combat VFX");instance=go.AddComponent<CombatVfxPool>();}instance.Emit(prefab,position,rotation,Mathf.Clamp(lifetime,.1f,6));}
 void Emit(GameObject key,Vector3 position,Quaternion rotation,float lifetime){if(active.Count>=24)return;if(!pool.TryGetValue(key,out var stack))pool[key]=stack=new Stack<GameObject>();GameObject go=null;while(stack.Count>0&&!go)go=stack.Pop();if(!go)go=Instantiate(key,transform);go.transform.SetPositionAndRotation(position,rotation);go.SetActive(true);foreach(var p in go.GetComponentsInChildren<ParticleSystem>())p.Play(true);active.Add(new Entry{Key=key,Object=go,End=Time.time+lifetime});}
 void Update(){for(int i=active.Count-1;i>=0;i--){var e=active[i];if(Time.time<e.End)continue;if(e.Object){e.Object.SetActive(false);if(pool[e.Key].Count<6)pool[e.Key].Push(e.Object);else Destroy(e.Object);}active.RemoveAt(i);}}
 void OnDestroy(){if(instance==this)instance=null;}
}}
