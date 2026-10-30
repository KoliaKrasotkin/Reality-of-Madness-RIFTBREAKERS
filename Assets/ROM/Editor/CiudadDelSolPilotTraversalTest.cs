using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ROM.EditorTools
{
    // Disposable Play Mode harness. Never saves the original scene, changes a source asset or starts networking.
    [InitializeOnLoad]
    public static class CiudadDelSolPilotTraversalTest
    {
        const string Menu="ROM/Dungeon Ciudad del Sol/Ampliacion vertical - piloto/";
        const string Source="Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        const string Prefab="Assets/ROM/Characters/Warrior/WarriorPlayer.prefab";
        const string Key="ROM.VerticalPilot.Traversal";
        const string Actor="TEMP_Warrior_CharacterController_Test";
        const string Report="Temp/ROM_Reviews/PilotoVertical_01/PlayMode_Traversal.txt";
        const string Fixture="Temp/ROM_Reviews/PilotoVertical_01/TraversalFixture.unity";
        const string FixtureName="TEMP_PilotoVertical_CharacterController_NO_GUARDAR";
        static CharacterController cc;
        static readonly List<string> log=new List<string>();
        static Vector3[] route;
        static Vector3 planar;
        static float vertical,elapsed,stalled,air,maxAir,peak,fallDistance;
        static bool falling;
        static int test,index,frame=-1,steps,sideFrames;
        static float speed;
        static Vector3 previous;
        static bool awaitingFixture;
        static double fixtureDeadline;
        static string entryScene;
        static string Digest(){using(var s=SHA256.Create())return BitConverter.ToString(s.ComputeHash(File.ReadAllBytes(Source)));}
        static CiudadDelSolPilotTraversalTest(){EditorApplication.playModeStateChanged+=State;EditorApplication.update+=Tick;}

        [MenuItem(Menu+"6. Probar CharacterController en Play Mode")]
        public static void Prepare()
        {
            try
            {
                if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new InvalidOperationException("Salir de Play y esperar compilacion.");
                var original=SceneManager.GetSceneByPath(Source);
                if(!original.IsValid()||original.isDirty)throw new InvalidOperationException("Original ausente o dirty: no iniciar ni guardar.");
                // PreviewStage fields are not a durable test fixture across assembly reloads.
                // Rebuild only the disposable preview so its original-state guard is current.
                CiudadDelSolVerticalPilotPreviewGenerator.Close();
                SceneManager.SetActiveScene(original);
                CiudadDelSolVerticalPilotPreviewGenerator.Preview();
                var pilot=Resources.FindObjectsOfTypeAll<GameObject>().SingleOrDefault(g=>g.name=="Subsuelo_PilotoVertical01_Temporal"&&g.scene.IsValid());
                if(!pilot||!EditorSceneManager.IsPreviewScene(pilot.scene))throw new InvalidOperationException("No esta abierta la copia temporal del piloto.");
                if(!original.IsValid()||original.isDirty)throw new InvalidOperationException("Original ausente o dirty: no iniciar ni guardar.");
                CiudadDelSolVerticalPilotPreviewGenerator.Validate();
                SessionState.SetString(Key+"Hash",Digest());
                var template=WarriorController();
                if(!template||!template.enabled)throw new InvalidOperationException("Falta CharacterController original.");
                var staging=EditorSceneManager.NewPreviewScene();
                var holder=new GameObject("TEMP_Staging");SceneManager.MoveGameObjectToScene(holder,staging);
                foreach(var root in pilot.scene.GetRootGameObjects())
                {
                    var copy=Object.Instantiate(root,holder.transform);copy.name=root.name;
                    foreach(var b in copy.GetComponentsInChildren<MonoBehaviour>(true))b.enabled=false;
                }
                StageUtility.GoToMainStage();
                // StageNavigationManager finishes its scene-mode transition after this menu callback.
                EditorApplication.delayCall+=()=>CreatePlayScene(staging,holder);
            }
            catch(Exception e){Debug.LogException(e);}
        }
        static CharacterController WarriorController()
        {
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if(!prefab)throw new InvalidOperationException("Falta prefab Warrior: "+Prefab);
            var controller=prefab.GetComponent<CharacterController>();
            if(!controller||!controller.enabled)throw new InvalidOperationException("Falta CharacterController original habilitado.");
            return controller;
        }
        static void CreatePlayScene(Scene staging,GameObject holder)
        {
            try
            {
                var original=SceneManager.GetSceneByPath(Source);
                if(StageUtility.GetCurrentStage()!=StageUtility.GetMainStage()||!original.IsValid()||original.isDirty||Digest()!=SessionState.GetString(Key+"Hash",""))throw new InvalidOperationException("Original o modo de escena no preparado; no guardar.");
                var temp=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                temp.name=FixtureName;
                holder.transform.SetParent(null);SceneManager.MoveGameObjectToScene(holder,temp);
                EditorSceneManager.ClosePreviewScene(staging);
                var cameraObject=new GameObject("TEMP_TestCamera");SceneManager.MoveGameObjectToScene(cameraObject,temp);
                var camera=cameraObject.AddComponent<Camera>();camera.transform.position=new Vector3(237,-28.2f,93);camera.transform.LookAt(new Vector3(225,-21,78));camera.farClipPlane=200;
                if(Digest()!=SessionState.GetString(Key+"Hash",""))throw new InvalidOperationException("Original cambio; prueba cancelada.");
                SceneManager.SetActiveScene(temp);
                // MultiFPS.Preloader replaces an unlisted scene with MainMenu before EnteredPlayMode.
                // Persist ONLY this disposable fixture, never the original; reload it after bootstrap.
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Fixture));
                if(!EditorSceneManager.SaveScene(temp,Fixture))throw new InvalidOperationException("No se pudo conservar la copia temporal para la transicion a Play.");
                SessionState.SetString(Key+"FixtureHash",FileDigest(Fixture));
                SessionState.SetBool(Key,true);
                EditorApplication.isPlaying=true;
            }
            catch(Exception e){Debug.LogException(e);}
        }

        static void State(PlayModeStateChange state)
        {
            if(!SessionState.GetBool(Key,false))return;
            if(state==PlayModeStateChange.EnteredPlayMode)
            {
                log.Clear();test=0;
                try
                {
                    if(Digest()!=SessionState.GetString(Key+"Hash",""))throw new InvalidOperationException("El archivo original cambio; no iniciar recorrido.");
                    if(!File.Exists(Fixture)||FileDigest(Fixture)!=SessionState.GetString(Key+"FixtureHash",""))throw new InvalidOperationException("Copia temporal ausente o alterada.");
                    // This runs after BeforeSceneLoad/Preloader, so bootstrap cannot discard the actor.
                    var entry=SceneManager.GetActiveScene();
                    entryScene=entry.name+" ["+entry.path+"]";
                    cc=null;awaitingFixture=true;fixtureDeadline=EditorApplication.timeSinceStartup+120;
                    // LoadSceneInPlayMode completes at the end of the frame, not inside this callback.
                    EditorSceneManager.LoadSceneInPlayMode(Fixture,new LoadSceneParameters(LoadSceneMode.Single));
                    Write("CICLO_VIDA_ESPERANDO_COPIA: entrada="+entryScene);
                }
                catch(Exception e){awaitingFixture=false;Finish("ERROR de ciclo de vida: "+e);}
            }
            if(state==PlayModeStateChange.EnteredEditMode)
            {
                awaitingFixture=false;cc=null;
                SessionState.SetBool(Key,false);
                if(Digest()!=SessionState.GetString(Key+"Hash","")){Debug.LogError("TRAVERSAL_ORIGINAL_HASH_CHANGED: no guardar.");return;}
                EditorApplication.delayCall+=()=>{EditorSceneManager.OpenScene(Source,OpenSceneMode.Single);Debug.Log("TRAVERSAL_ORIGINAL_INTACTO: archivo original sin cambios, reabierto sin guardar. Informe: "+Report);};
            }
        }
        static void CreateRuntimeActor(Scene fixture)
        {
                try
                {
                    SceneManager.SetActiveScene(fixture);
                    if(!fixture.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Any(t=>t.name=="Subsuelo_PilotoVertical01_Temporal"))throw new InvalidOperationException("Falta la geometria temporal: no crear controlador en otra escena.");
                    var actors=fixture.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).Where(t=>t.name==Actor).ToArray();
                    if(actors.Length>1)throw new InvalidOperationException("Objetos de prueba duplicados; ensayo cancelado.");
                    var actor=actors.Length==1?actors[0].gameObject:new GameObject(Actor);
                    SceneManager.MoveGameObjectToScene(actor,fixture);
                    actor.SetActive(false);
                    var template=WarriorController(); // Fresh asset reference AFTER domain/scene reload.
                    cc=actor.GetComponent<CharacterController>();if(!cc)cc=actor.AddComponent<CharacterController>();
                    EditorUtility.CopySerialized(template,cc);
                    actor.layer=template.gameObject.layer;actor.transform.localScale=template.transform.lossyScale;
                    actor.transform.position=Down()[0]+Vector3.up*.04f;actor.SetActive(true);
                    if(!cc||cc.gameObject!=actor||EditorUtility.IsPersistent(cc)||cc.gameObject.scene!=fixture)throw new InvalidOperationException("CharacterController no pertenece a la instancia temporal de Play.");
                    Physics.SyncTransforms();
                    Write("CICLO_VIDA_OK: entrada="+entryScene+"; copia="+fixture.path+"; controlador recuperado/creado DESPUES de EnteredPlayMode y scene.isLoaded, instancia="+cc.GetInstanceID());
                }
                catch(Exception e){Finish("ERROR de ciclo de vida: "+e);return;}
                log.Add("PRUEBA REAL PLAY MODE: CharacterController.Move por frames, gravedad y colisiones. Sin salto.");
                log.Add("Componente copiado mediante CopySerialized de "+Prefab+". No se ejecutan WarriorAvatar, salud, Mirror ni entrada del jugador.");
                log.Add($"height={cc.height}; radius={cc.radius}; center={cc.center}; slopeLimit={cc.slopeLimit}; stepOffset={cc.stepOffset}; skinWidth={cc.skinWidth}; minMoveDistance={cc.minMoveDistance}; scale={cc.transform.lossyScale}; layer={cc.gameObject.layer}");
                Begin();
        }
        static string FileDigest(string path){using(var h=SHA256.Create())return BitConverter.ToString(h.ComputeHash(File.ReadAllBytes(path)));}
        static Vector3[] Down()=>new[]{new Vector3(225,-16,99),new Vector3(225,-16,96),new Vector3(225,-18,90),new Vector3(225,-20,84),new Vector3(225,-22,78),new Vector3(225,-24,72),new Vector3(225,-24,69),new Vector3(231,-24,69),new Vector3(231,-24,72),new Vector3(231,-26,78),new Vector3(231,-28,84),new Vector3(231,-30,90),new Vector3(231,-30,93)};
        static string Label=>new[]{"BAJADA_CAMINANDO","SUBIDA_CAMINANDO","BAJADA_CORRIENDO","SUBIDA_CORRIENDO"}[test];
        static void Begin()
        {
            route=Down();if(test%2==1)Array.Reverse(route);
            speed=test<2?2.8f:5.7f;
            cc.enabled=false;cc.transform.position=route[0]+Vector3.up*.04f;cc.enabled=true;Physics.SyncTransforms();
            planar=Vector3.zero;vertical=0;elapsed=stalled=air=maxAir=fallDistance=0;falling=false;steps=sideFrames=0;index=1;previous=cc.transform.position;frame=-1;
            Write("INICIO "+Label+" "+cc.transform.position+" velocidad="+speed);
        }
        static void Tick()
        {
            if(!EditorApplication.isPlaying||EditorApplication.isPaused||!SessionState.GetBool(Key,false))return;
            if(awaitingFixture)
            {
                var fixture=SceneManager.GetSceneByPath(Fixture);
                if(fixture.IsValid()&&fixture.isLoaded){awaitingFixture=false;CreateRuntimeActor(fixture);}
                else if(EditorApplication.timeSinceStartup>fixtureDeadline){awaitingFixture=false;Finish("ERROR de ciclo de vida: timeout esperando carga de la copia temporal.");}
                return;
            }
            if(!cc||Time.frameCount==frame)return;
            frame=Time.frameCount;
            float dt=Time.deltaTime;if(dt<=0)return;
            elapsed+=dt;
            // Same grounding fallback, gravity and horizontal acceleration as WarriorAvatar.
            bool grounded=cc.isGrounded||Physics.SphereCast(cc.transform.position+Vector3.up*.35f,.24f,Vector3.down,out _,.23f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
            if(grounded&&vertical<0)vertical=-2;
            var target=route[index];var delta=target-cc.transform.position;delta.y=0;
            // Slow only when arriving at a waypoint, preserving velocity through staircase joins.
            float desiredSpeed=Mathf.Min(speed,delta.magnitude/Mathf.Max(.001f,dt));
            planar=Vector3.MoveTowards(planar,delta.normalized*desiredSpeed,22*dt);
            vertical=Mathf.Max(-55,vertical-23*dt);
            var before=cc.transform.position;var flags=cc.Move((planar+Vector3.up*vertical)*dt);steps++;
            if((flags&CollisionFlags.Above)!=0&&vertical>0)vertical=0;
            if((flags&CollisionFlags.Sides)!=0)sideFrames++;
            bool supported=(flags&CollisionFlags.Below)!=0||Physics.SphereCast(cc.transform.position+Vector3.up*.35f,.24f,Vector3.down,out _,.23f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
            if(!supported){air+=dt;maxAir=Mathf.Max(maxAir,air);}else{if(air>.15f)Write("CONTACTO recuperado tras "+air.ToString("F3")+"s en "+cc.transform.position);air=0;}
            // Same server fall-damage floor ray and threshold, without invoking health/gameplay.
            bool floor=Physics.Raycast(cc.transform.position+Vector3.up*.2f,Vector3.down,.5f,MultiFPS.GameManager.environmentLayer,QueryTriggerInteraction.Ignore);
            if(!floor){if(!falling){falling=true;peak=cc.transform.position.y;}peak=Mathf.Max(peak,cc.transform.position.y);}
            else if(falling){fallDistance=Mathf.Max(fallDistance,peak-cc.transform.position.y);falling=false;}
            var moved=cc.transform.position-before;moved.y=0;
            stalled=moved.magnitude<.002f&&delta.magnitude>.2f?stalled+dt:0;
            if(cc.transform.position.y< -31||stalled>2||elapsed>100){Result(false,"bloqueo/caida/timeout; destino="+target+"; cercanos="+Nearby());return;}
            delta=target-cc.transform.position;
            if(new Vector2(delta.x,delta.z).magnitude<.14f)
            {
                if(Mathf.Abs(delta.y)>.5f){Result(false,"cota incorrecta al alcanzar junta "+index+" destino="+target+"; cercanos="+Nearby());return;}
                Write("JUNTA "+index+" OK en "+cc.transform.position+" grounded="+supported+" flags="+flags);
                if(++index>=route.Length){Result(true,"recorrido completo");return;}
            }
            previous=cc.transform.position;
        }
        static string Nearby()=>string.Join("; ",Physics.OverlapSphere(cc.transform.position+Vector3.up,.8f,~0,QueryTriggerInteraction.Ignore).Where(c=>c!=cc).Select(c=>Path(c.transform)).Distinct());
        static string Path(Transform t)=>t.parent?Path(t.parent)+"/"+t.name:t.name;
        static void Result(bool passed,string reason)
        {
            Write($"RESULTADO {Label}: {(passed?"COMPLETO":"FALLO")} pos={cc.transform.position}; {reason}; tiempo={elapsed:F2}; framesMove={steps}; contactoLateralFrames={sideFrames}; maxSinContacto={maxAir:F3}s; caidaRegistrada={fallDistance:F3}m; danoPotencial={(fallDistance>5.5f?Mathf.RoundToInt(fallDistance*fallDistance*.5f):0)}");
            if(++test<4)Begin();else Finish("Cuatro recorridos finalizados. Revisar resultados individuales; no equivalen a prueba del jugador completo ni multijugador.");
        }
        static void Write(string s){log.Add(s);Debug.Log("PILOTO_TRAVERSAL "+s);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Report));File.WriteAllLines(Report,log);}
        static void Finish(string message){Write(message);EditorApplication.isPlaying=false;}
    }
}

