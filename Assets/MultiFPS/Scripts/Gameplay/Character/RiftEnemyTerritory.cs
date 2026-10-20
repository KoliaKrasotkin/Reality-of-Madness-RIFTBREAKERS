using MultiFPS.Gameplay;
using UnityEngine;
namespace MultiFPS.Gameplay {
    public sealed class RiftEnemyTerritory : MonoBehaviour {
        public Vector3 Home;
        public float Leash=65;
        public bool CanTarget(Health target) => target && Vector3.Distance(target.transform.position,Home)<Leash && (!GameManager.Gamemode || !GameManager.Gamemode.IsProtectedPosition(target.transform.position));
    }
}
