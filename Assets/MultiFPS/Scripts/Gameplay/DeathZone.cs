using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay
{
    [RequireComponent(typeof(BoxCollider))]
    public class DeathZone : NetworkBehaviour
    {
        public bool DespawnItems = false;        
        private void Awake()
        {
            gameObject.layer = (int)GameLayers.trigger;
        }
        private void OnTriggerStay(Collider other)
        {
            if (!isServer) return;
            Health h = other.GetComponentInParent<Health>();

            if (h && h.CurrentHealth > 0)
                h.Server_ChangeHealthStateRaw(h.CurrentHealth, 0,AttackType.falldamage, h, 500);

            if (!DespawnItems) return;
                
            Item i = other.GetComponentInParent<Item>();
            if (i)
            {
                NetworkServer.Destroy(i.gameObject);
            }
        }
    }
}
