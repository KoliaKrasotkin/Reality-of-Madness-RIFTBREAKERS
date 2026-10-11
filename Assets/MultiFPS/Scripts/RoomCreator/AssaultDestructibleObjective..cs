using System;
using UnityEngine;
using Mirror;

namespace MultiFPS.Gameplay.Gamemodes
{
    public class AssaultDestructibleObjective : NetworkBehaviour
    {
        [Header("Objetivo destructible")]
        [SerializeField] private int maxHealth = 300;

        [SyncVar]
        private int currentHealth;

        private bool destroyed;

        public int CurrentHealth => currentHealth;
        public int MaxHealth => maxHealth;

        public event Action Server_OnObjectiveDestroyed;

        public override void OnStartServer()
        {
            currentHealth = Mathf.Max(1, maxHealth);
            destroyed = false;
        }

        [Server]
        public void ServerTakeDamage(int damage)
        {
            if (destroyed || damage <= 0)
                return;

            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (currentHealth > 0)
                return;

            destroyed = true;

            Server_OnObjectiveDestroyed?.Invoke();

            NetworkServer.Destroy(gameObject);
        }
    }
}