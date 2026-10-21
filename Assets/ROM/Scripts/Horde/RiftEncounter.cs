using UnityEngine;
namespace ROM.Horde {
    public sealed class RiftEncounter : MonoBehaviour {
        public string Title;
        public bool MiniBoss;
        public Transform[] SpawnPoints;
        public float ActivationRadius=135;
        [System.NonSerialized] public int Spawned, Defeated;
        [System.NonSerialized] public readonly System.Collections.Generic.HashSet<uint> Participants=new System.Collections.Generic.HashSet<uint>();
        public int Count => SpawnPoints == null ? 0 : SpawnPoints.Length;
        public bool Cleared => Count>0 && Defeated>=Count;
    }
}
