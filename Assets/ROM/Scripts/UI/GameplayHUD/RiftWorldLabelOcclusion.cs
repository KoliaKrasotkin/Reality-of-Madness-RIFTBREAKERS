using System.Collections.Generic;
using MultiFPS.UI;
using UnityEngine;
namespace ROM.UI.Gameplay {
// Local presentation only; restore each renderer's previous flag on close/disable.
[DefaultExecutionOrder(1500)]public sealed class RiftWorldLabelOcclusion:MonoBehaviour {
 readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
 RiftFieldUI map;float next;
 void Awake(){foreach(var checkpoint in FindObjectsByType<ROM.Horde.RiftCheckpoint>(FindObjectsSortMode.None))if(!checkpoint.GetComponentInChildren<ROM.Horde.RiftCheckpointLabel>(true)){var guard=new GameObject("Local checkpoint presentation guard");guard.transform.SetParent(checkpoint.transform,false);guard.AddComponent<ROM.Horde.RiftCheckpointLabel>();}}
 void LateUpdate(){if(!map)map=FindFirstObjectByType<RiftFieldUI>();bool covered=ClientFrontend.Pause||(UIPauseMenu._instance&&UIPauseMenu._instance.InterfaceShowing)||(map&&map.MapOpen)||(ROM.UI.Sprint01.RiftVillageUI.Instance&&ROM.UI.Sprint01.RiftVillageUI.Instance.TalkingTo);if(!covered){Restore();return;}if(Time.unscaledTime<next)return;next=Time.unscaledTime+.2f;
  foreach(var label in FindObjectsByType<TextMesh>(FindObjectsSortMode.None)){if(label.GetComponentInParent<ROM.Horde.RiftCheckpoint>())continue;var renderer=label.GetComponent<Renderer>();if(!renderer)continue;if(!hidden.ContainsKey(renderer))hidden[renderer]=renderer.forceRenderingOff;renderer.forceRenderingOff=true;}
 }
 void Restore(){foreach(var p in hidden)if(p.Key)p.Key.forceRenderingOff=p.Value;hidden.Clear();}
 void OnDisable()=>Restore();
}
}
