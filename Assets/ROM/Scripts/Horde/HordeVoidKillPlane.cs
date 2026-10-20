using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using UnityEngine;

namespace ROM.Horde
{
    /// <summary>
    /// Server fallback for falling characters, including controllers that cross a
    /// thin trigger between network ticks. Independent of physics collision layers.
    /// </summary>
    public sealed class HordeVoidKillPlane : MonoBehaviour
    {
        public float KillHeight = -38f;
        float _nextCheck;

        void FixedUpdate()
        {
            if (!NetworkServer.active || Time.time < _nextCheck || !GameSync.Singleton || GameSync.Singleton.Healths == null)
                return;

            _nextCheck = Time.time + .1f;
            foreach (Health health in GameSync.Singleton.Healths.Obj)
            {
                if (health && health.CurrentHealth > 0 && health.transform.position.y < KillHeight)
                    health.Server_ChangeHealthStateRaw(health.CurrentHealth, 0, AttackType.falldamage, health, 500);
            }
        }
    }
}
