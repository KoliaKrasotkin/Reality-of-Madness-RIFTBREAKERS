using System;
using System.IO;
using Mirror;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

namespace ROM.EditorTools
{
    internal static partial class CiudadDelSolJardinesPlayCheck
    {
        // Explicit batch entry point only. No startup action in ordinary Editor sessions.
        public static void TestLittleDemonInBatch()
        {
            Check(Application.isBatchMode && !EditorApplication.isPlayingOrWillChangePlaymode,
                "La prueba automatizada requiere batch en modo edición.");
            Check(EditorSceneManager.playModeStartScene == null,
                "Hay una escena de inicio configurada; no se modificará para esta prueba.");
            EditorSceneManager.OpenScene(MainMenu, OpenSceneMode.Single);
            SessionState.SetBool(DemonFunctionalProbe.Key, true);
            SessionState.SetInt(DemonFunctionalProbe.Phase, 0);
            SessionState.SetFloat(DemonFunctionalProbe.Deadline, (float)EditorApplication.timeSinceStartup + 180);
            DemonFunctionalProbe.Install();
            EditorApplication.EnterPlaymode();
        }

        static class DemonFunctionalProbe
        {
            internal const string Key = "ROM.City.DemonProbe.Active", Phase = Key + ".Phase", Deadline = Key + ".Deadline";
            static float deadline;
            static int phase;
            static bool attacked;
            static double strikeAt;
            static int warriorLife, enemyLife;
            static bool detected, pursued, damaged;
            static double nextAttack;
            static bool warriorReadyObserved;

            [InitializeOnLoadMethod]
            internal static void Install()
            {
                if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
                phase = SessionState.GetInt(Phase, 0);
                deadline = SessionState.GetFloat(Deadline, 0);
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }

            static void SetPhase(int value)
            {
                phase = value; SessionState.SetInt(Phase, value);
                Debug.Log("CIUDAD_DIABLILLO FASE " + value);
            }

            static void Tick()
            {
                try
                {
                    if (phase == 99)
                    {
                        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                        EditorApplication.update -= Tick;
                        SessionState.EraseBool(Key);
                        EditorApplication.Exit(SessionState.GetInt(Key + ".Exit", 1));
                        return;
                    }
                    Check(EditorApplication.timeSinceStartup < deadline, "La prueba superó 180 s en fase " + phase +
                        " (0 arranque, 1 carga, 2 Host/Warrior, 3 persecución/ataque enemigo, 4 daño/muerte).");
                    if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
                    var scene = SceneManager.GetActiveScene();
                    if (phase == 0)
                    {
                        if (scene.path != MainMenu || !ClientInterfaceManager.Instance || !MultiFPS.DNNetworkManager.Instance) return;
                        Debug.Log("CIUDAD_DIABLILLO PRUEBA_PLAY_MODE MainMenu cargado por flujo original.");
                        LoadInPlay();
                        Check(string.IsNullOrEmpty(cityLoadFailure), "Carga: " + cityLoadFailure);
                        SetPhase(1); return;
                    }
                    if (phase == 1)
                    {
                        Check(string.IsNullOrEmpty(cityLoadFailure), "Carga: " + cityLoadFailure);
                        if (scene.path != City || pending != null || !cityLoadVerified) return;
                        StartWarriorTrial();
                        Check(stopWarriorTrial != null, "Host: el arranque fue rechazado; revisá el error precedente del Host local.");
                        SetPhase(2); return;
                    }
                    Check(stopWarriorTrial != null && NetworkServer.active, "Host: la sesión local fue cancelada o no está activa.");
                    var warrior = ClientFrontend.OwnedCharacterInstance;
                    if (!warrior) { Check(phase == 2, "Warrior: desapareció durante el encuentro."); return; }
                    Check(warrior.Health && warrior.Health.CurrentHealth > 0, "Warrior: murió antes de completar el ensayo.");
                    var combat = AbilityController.For(warrior.Health);
                    if (phase == 2)
                    {
                        if (stopWarriorTrial == null) throw new InvalidOperationException("El Host original rechazó el arranque. No se cambiará la selección guardada.");
                        Check(combat && combat.Actor == warrior && combat.isServer && combat.isLocalPlayer && combat.AutoProfile != null,
                            "Warrior: el personaje existe pero su combate local original no está disponible.");
                        // The Host's WaitForWarrior callback may run later in this same Editor update.
                        // Let it finish its initial teleport before this probe approaches the enemy.
                        if (!warriorReadyObserved) { warriorReadyObserved = true; return; }
                        if (Vector3.Distance(warrior.transform.position, BridgeSpawn) > 2) return;
                        StartDemonTrial();
                        Check(demonTrial != null && demonTrial.Enemy, "No se pudo crear el Diablillo.");
                        warriorLife = warrior.Health.CurrentHealth;
                        enemyLife = demonTrial.Enemy.Health.CurrentHealth;
                        // Exercise pursuit instead of spawning already inside melee range.
                        var requested = demonTrial.Origin + Vector3.back * 7;
                        var filter = new NavMeshQueryFilter { agentTypeID = 0, areaMask = NavMesh.AllAreas };
                        Check(NavMesh.SamplePosition(requested, out var point, .4f, filter), "No hay apoyo para la aproximación de la prueba.");
                        warrior.ServerTeleport(point.position + Vector3.up * .03f, 0);
                        SetPhase(3); return;
                    }
                    if (phase == 4 && demonTrial != null && demonTrial.Enemy && demonTrial.Enemy.Health.CurrentHealth <= 0)
                    {
                        Check(damaged, "Muerte sin daño del Warrior registrado.");
                        Finish(true, $"PLAY REAL: aparición, detección, persecución NavMesh, ataque enemigo y daño al Warrior ({warriorLife}→{warrior.Health.CurrentHealth}); " +
                            $"ataques originales del Warrior y muerte del Diablillo ({enemyLife}→0). Sin daño forzado ni especialización artificial. Habilidades y clientes remotos: pendientes.");
                        return;
                    }
                    Check(demonTrial != null && demonTrial.Enemy, "Enemigo: desapareció sin verificarse su muerte.");
                    var enemy = demonTrial.Enemy;
                    Check(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "El enemigo abandonó el NavMesh.");
                    if (phase == 3)
                    {
                        if (!detected && enemy.Target == warrior.Health) { detected = true; Debug.Log("CIUDAD_DIABLILLO DETECCION_OK"); }
                        if (!pursued && demonTrial.Moved) { pursued = true; Debug.Log("CIUDAD_DIABLILLO PERSECUCION_OK"); }
                        if (warrior.Health.CurrentHealth >= warriorLife) return;
                        Check(demonTrial.Moved, "Hubo daño sin desplazamiento real de persecución.");
                        Check(enemy.Target == warrior.Health, "El objetivo del Diablillo no es el Warrior.");
                        Debug.Log("CIUDAD_DIABLILLO ATAQUE_ENEMIGO_OK daño al Warrior confirmado.");
                        combat.CmdSetTarget(enemy.netId);
                        combat.CmdAutoAttack(); attacked = true; strikeAt = EditorApplication.timeSinceStartup;
                        SetPhase(4); return;
                    }
                    if (phase == 4)
                    {
                        if (enemy.Health.CurrentHealth < enemyLife)
                        {
                            if (!damaged) Debug.Log("CIUDAD_DIABLILLO ATAQUE_WARRIOR_OK daño al Diablillo confirmado.");
                            damaged = true;
                        }
                        Check(damaged || !attacked || EditorApplication.timeSinceStartup - strikeAt < 15,
                            "El autoataque no produjo daño en 15 s.");
                        if (EditorApplication.timeSinceStartup >= nextAttack)
                        {
                            combat.CmdAutoAttack();
                            nextAttack = EditorApplication.timeSinceStartup + .25;
                        }
                    }
                }
                catch (Exception error) { Finish(false, error.Message); }
            }

            static void Finish(bool success, string message)
            {
                SetPhase(99);
                try { StopDemonTrial(); stopWarriorTrial?.Invoke(); }
                catch (Exception error) { success = false; message += " Limpieza: " + error.Message; }
                SessionState.SetInt(Key + ".Exit", success ? 0 : 1);
                var text = (success ? "CIUDAD_DIABLILLO FUNCIONAL_OK " : "CIUDAD_DIABLILLO FUNCIONAL_FALLO ") + message;
                Directory.CreateDirectory("Backups/CiudadDelSol/Combate/PrimerDiablillo");
                File.WriteAllText("Backups/CiudadDelSol/Combate/PrimerDiablillo/ResultadoFuncional.txt", text);
                Debug.Log(text);
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }
    }
}
