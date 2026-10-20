using System;
using System.IO;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.Gamemodes;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ROM.Horde.EditorTools
{
    // Isolated Play Mode check using the real Health, Mirror server and kill-plane.
    // No gameplay scene or project settings are saved by this probe.
    [InitializeOnLoad]
    public static class VoidFallSmokeProbe
    {
        const string Key="ROM.VoidFallSmokeProbe";
        static Health health;
        static double started;
        static int deaths;
        static bool confirmedAlive;

        static VoidFallSmokeProbe()
        {
            EditorApplication.playModeStateChanged+=Changed;
        }

        public static void FromCommandLine()
        {
            SessionState.SetInt(Key,1);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            EditorApplication.isPlaying=true;
        }

        static void Changed(PlayModeStateChange change)
        {
            if(SessionState.GetInt(Key,0)==0) return;
            if(change==PlayModeStateChange.EnteredPlayMode)
            {
                try
                {
                    var transport=new GameObject("ProbeTransport").AddComponent<TelepathyTransport>();
                    Transport.active=transport;
                    NetworkServer.listen=false; // No network socket or remote connections.
                    NetworkServer.Listen(1);
                    var room=new GameObject("ProbeRoom"); room.SetActive(false);
                    room.AddComponent<NetworkIdentity>(); room.AddComponent<GameSync>();
                    var mode=room.AddComponent<Gamemode>();
                    room.GetComponent<RoomManager>().enabled=false;
                    room.GetComponent<GameSync>().enabled=false;
                    mode.enabled=false; room.SetActive(true); GameManager.SetGamemode(mode);
                    NetworkServer.Spawn(room);
                    var player=new GameObject("FallingHealth"); player.SetActive(false);
                    player.AddComponent<NetworkIdentity>(); health=player.AddComponent<Health>();
                    player.AddComponent<BoxCollider>(); player.AddComponent<Rigidbody>(); health.enabled=false;
                    health.CurrentHealth=20000; health.MaxHealth=20000;
                    health._damageMemories=new DamageMemory[6];
                    health.Server_OnHealthDepleted+=(part,type,killer,force)=>deaths++;
                    player.transform.position=new Vector3(0,-32,0);
                    player.SetActive(true);
                    NetworkServer.Spawn(player);
                    GameSync.Singleton.Healths.Obj[0]=health;
                    new GameObject("VoidGuard").AddComponent<HordeVoidKillPlane>().KillHeight=-38;
                    started=EditorApplication.timeSinceStartup;
                    EditorApplication.update+=Tick;
                }
                catch(Exception e) { Finish(false,e.ToString()); }
            }
            else if(change==PlayModeStateChange.EnteredEditMode)
            {
                int result=SessionState.GetInt(Key,3);
                SessionState.EraseInt(Key);
                EditorApplication.Exit(result==2?0:1);
            }
        }

        static void Tick()
        {
            try
            {
                if(!health) { Finish(false,"El personaje desaparecio durante la prueba."); return; }
                if(EditorApplication.timeSinceStartup-started>.1 && health.transform.position.y>-38)
                    confirmedAlive=health.CurrentHealth==20000;
                if(health.CurrentHealth==0)
                {
                    Finish(confirmedAlive && deaths==1 && health.transform.position.y<-38,
                        "Health inicial=20000; vivo sobre el limite="+confirmedAlive+"; health final="+health.CurrentHealth+"; callbacks de muerte="+deaths+"; Y="+health.transform.position.y);
                }
                else if(EditorApplication.timeSinceStartup-started>12) Finish(false,"Timeout: la caida no produjo muerte.");
            }
            catch(Exception e) { Finish(false,e.ToString()); }
        }

        static void Finish(bool success,string detail)
        {
            EditorApplication.update-=Tick;
            string report=(success?"PASS: ":"FAIL: ")+detail;
            Directory.CreateDirectory("Documentation");
            File.WriteAllText("Documentation/void-fall-smoke-test.txt",report);
            Debug.Log("[VoidProbe] "+report);
            SessionState.SetInt(Key,success?2:3);
            if(health) UnityEngine.Object.DestroyImmediate(health.gameObject);
            NetworkServer.Shutdown();
            EditorApplication.isPlaying=false;
        }
    }
}
