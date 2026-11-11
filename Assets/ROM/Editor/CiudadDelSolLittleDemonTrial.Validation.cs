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

            [InitializeOnLoadMethod]
            internal static void Install()
            {
                if (!Application.isBatchMode || !SessionState.GetBool(Key, false)) return;
                phase = SessionState.GetInt(Phase, 0);
                deadline = SessionState.GetFloat(Deadline, 0);
                EditorApplication.update -= Tick;
                EditorApplication.update += Tick;
            }

            static void SetPhase(int value) { phase = value; SessionState.SetInt(Phase, value); }

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
                    Check(EditorApplication.timeSinceStartup < deadline, "La prueba superó su límite de 180 s.");
                    if (!EditorApplication.isPlaying || EditorApplication.isPaused) return;
                    var scene = SceneManager.GetActiveScene();
                    if (phase == 0)
                    {
                        if (scene.path != MainMenu || !ClientInterfaceManager.Instance || !MultiFPS.DNNetworkManager.Instance) return;
                        Debug.Log("CIUDAD_DIABLILLO PRUEBA_PLAY_MODE MainMenu cargado por flujo original.");
                        LoadInPlay(); SetPhase(1); return;
                    }
                    if (phase == 1)
                    {
                        if (scene.path != City || pending != null) return;
                        StartWarriorTrial(); SetPhase(2); return;
                    }
                    var warrior = ClientFrontend.OwnedCharacterInstance;
                    if (!warrior || !NetworkServer.active) return;
                    var combat = AbilityController.For(warrior.Health);
                    if (phase == 2)
                    {
                        if (stopWarriorTrial == null) throw new InvalidOperationException("El Host original rechazó el arranque. No se cambiará la selección guardada.");
                        if (!combat || !combat.Prototype) return;
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
                    Check(demonTrial != null && demonTrial.Enemy, "El enemigo desapareció antes de completar la comprobación.");
                    var enemy = demonTrial.Enemy;
                    Check(enemy.GetComponent<NavMeshAgent>().isOnNavMesh, "El enemigo abandonó el NavMesh.");
                    if (phase == 3)
                    {
                        if (warrior.Health.CurrentHealth >= warriorLife) return;
                        Check(demonTrial.Moved, "Hubo daño sin desplazamiento real de persecución.");
                        Check(enemy.Target == warrior.Health, "El objetivo del Diablillo no es el Warrior.");
                        combat.CmdSetTarget(enemy.netId);
                        combat.CmdAutoAttack(); attacked = true; strikeAt = EditorApplication.timeSinceStartup;
                        SetPhase(4); return;
                    }
                    if (phase == 4)
                    {
                        if (enemy.Health.CurrentHealth < enemyLife)
                        {
                            Finish(true, $"PLAY REAL: persecución >0,5 m; Warrior {warriorLife}→{warrior.Health.CurrentHealth}; " +
                                $"Diablillo {enemyLife}→{enemy.Health.CurrentHealth}; objetivo y autoataque por Commands originales. " +
                                "Sin muerte forzada, recompensa ni respawn de enemigos. Clientes remotos y habilidades: pendientes.");
                            return;
                        }
                        Check(!attacked || EditorApplication.timeSinceStartup - strikeAt < 15,
                            "El autoataque no produjo daño en 15 s.");
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
