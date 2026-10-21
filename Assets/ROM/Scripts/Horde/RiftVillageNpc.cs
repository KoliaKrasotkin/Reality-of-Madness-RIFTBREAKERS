using UnityEngine;

namespace ROM.Horde
{
    public sealed class RiftVillageNpc : MonoBehaviour
    {
        public string Id, DisplayName, Role;
        [TextArea] public string Greeting;
        [TextArea] public string Advice;
        public bool Trades;
        public float InteractionRadius = 4.5f;
    }
}
