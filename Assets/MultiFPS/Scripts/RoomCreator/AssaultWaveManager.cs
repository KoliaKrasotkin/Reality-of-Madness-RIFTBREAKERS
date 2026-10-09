using UnityEngine;
using Mirror;
using MultiFPS.Gameplay;
using System.Collections.Generic;

public class AssaultWaveManager : NetworkBehaviour
{
    private int enemiesAlive = 0;

    public int EnemiesAlive => enemiesAlive;

    private readonly HashSet<Health> registeredEnemies = new HashSet<Health>();
    private readonly HashSet<Health> defeatedEnemies = new HashSet<Health>();

    public void RegisterEnemy(Health enemyHealth)
    {
        if (!isServer || enemyHealth == null)
            return;

        // Cada personaje se registra una sola vez.
        if (!registeredEnemies.Add(enemyHealth))
            return;

        enemyHealth.Server_OnHealthDepleted +=
            (part, attackType, killer, attackForce) =>
                OnEnemyDeath(enemyHealth);

        enemiesAlive++;

        Debug.Log("Asalto: enemigos vivos = " + enemiesAlive);
    }

    private void OnEnemyDeath(Health enemyHealth)
    {
        if (!isServer || enemyHealth == null)
            return;

        // Impide descontar varias veces la misma muerte.
        if (!defeatedEnemies.Add(enemyHealth))
            return;

        enemiesAlive = Mathf.Max(0, enemiesAlive - 1);

        Debug.Log("Asalto: enemigo eliminado. Vivos = " + enemiesAlive);
    }
}