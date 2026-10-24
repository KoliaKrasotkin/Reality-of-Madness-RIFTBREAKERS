using System.Collections.Generic;
using MultiFPS.UI;
using UnityEngine;
namespace ROM.Horde {
    // Local presentation only: checkpoints, interaction, physics and network state are untouched.
    public sealed class RiftCheckpointLabel : MonoBehaviour {
        ROM.UI.Gameplay.RiftFieldUI map;
        readonly Dictionary<Renderer,bool> hidden=new Dictionary<Renderer,bool>();
        void LateUpdate(){
            if(MultiFPS.GameplayCamera._instance)transform.rotation=MultiFPS.GameplayCamera._instance.transform.rotation;
            if(!map)map=FindFirstObjectByType<ROM.UI.Gameplay.RiftFieldUI>();
            if(ClientFrontend.Pause||(map&&map.MapOpen)||(ROM.UI.Sprint01.RiftVillageUI.Instance&&ROM.UI.Sprint01.RiftVillageUI.Instance.TalkingTo)){
                if(hidden.Count==0){var checkpoint=GetComponentInParent<RiftCheckpoint>();var renderers=checkpoint?checkpoint.GetComponentsInChildren<Renderer>(true):GetComponents<Renderer>();foreach(var renderer in renderers){if(renderer.name=="Base del cristal")continue;hidden[renderer]=renderer.forceRenderingOff;renderer.forceRenderingOff=true;}}
            }else Restore();
        }
        void Restore(){foreach(var pair in hidden)if(pair.Key)pair.Key.forceRenderingOff=pair.Value;hidden.Clear();}
        void OnDisable()=>Restore();
    }
}
