using UnityEngine;
namespace ROM.Horde {
    public sealed class RiftCheckpointLabel : MonoBehaviour {
        void LateUpdate(){if(MultiFPS.GameplayCamera._instance)transform.rotation=MultiFPS.GameplayCamera._instance.transform.rotation;}
    }
}
