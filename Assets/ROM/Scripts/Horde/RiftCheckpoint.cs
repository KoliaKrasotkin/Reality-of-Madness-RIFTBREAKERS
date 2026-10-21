using UnityEngine;
namespace ROM.Horde {
    public sealed class RiftCheckpoint : MonoBehaviour {
        public string Title;
        public float SafeRadius=18;
        public bool StartingCamp;
        public Transform Respawn;
        public bool Contains(Vector3 point) => Mathf.Abs(point.y-transform.position.y)<35 && Vector2.Distance(new Vector2(point.x,point.z),new Vector2(transform.position.x,transform.position.z))<SafeRadius;
    }
}
