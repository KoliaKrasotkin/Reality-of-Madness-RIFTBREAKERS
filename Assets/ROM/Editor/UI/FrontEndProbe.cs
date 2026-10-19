using System;
using System.IO;
using System.Linq;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.EditorTools
{
    [InitializeOnLoad]
    public static class FrontEndProbe
    {
        const string Key="ROM.FrontEndProbe";
        static double started;
        static int step;
        static string report;
        static FrontEndProbe(){EditorApplication.playModeStateChanged+=Changed;}
        public static void FromCommandLine()
        {
            FrontEndBuilder.Build();SessionState.SetInt(Key,1);EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.isPlaying=true;
        }
        public static void CheckFromCommandLine(){SessionState.SetInt(Key,1);EditorSceneManager.OpenScene("Assets/MultiFPS/Levels/hub.unity");EditorApplication.isPlaying=true;}
        public static void ReleaseFromCommandLine(){FrontEndBuilder.Build();ROM.Horde.EditorTools.FracturedForestBuilder.CaptureRiftReview();CheckFromCommandLine();}
        static void Changed(PlayModeStateChange state)
        {
            if(SessionState.GetInt(Key,0)==0)return;
            if(state==PlayModeStateChange.EnteredPlayMode){started=EditorApplication.timeSinceStartup;step=0;report="FRONTEND LOCAL VALIDATION\n";EditorApplication.update+=Tick;}
            if(state==PlayModeStateChange.EnteredEditMode){SessionState.SetInt(Key,0);EditorApplication.Exit(SessionState.GetInt(Key+"Result",1));}
        }
        static void Tick()
        {
            if(EditorApplication.timeSinceStartup-started<1.5)return;
            try{
                var hub=UnityEngine.Object.FindFirstObjectByType<RiftHub>();Assert(hub,"Hub exists");
                if(step==0){
                    Assert(hub.Creator&&hub.Creator.Maps.Length==1&&hub.Creator.Maps[0].Name=="El Rift","Single destination");
                    Assert(hub.Catalog.Characters.Length==5,"Five IDS characters");Assert(hub.Catalog.Models.All(m=>m.Prefab),"Every catalog item has a model");
                    Assert(hub.Services.Missions.Missions.Count==1,"No extra mission selection");
                    CheckPoses(hub.Catalog);
                    string file="Documentation/frontend-social-test.json";if(File.Exists(file))File.Delete(file);var social=new LocalSocialService(Path.GetFullPath(file));
                    string self=social.Self.Id;Assert(social.AddFriend("Nyx")==null,"Add local friend");string nyx=social.Friends.First().Id;
                    Assert(social.Send(nyx,"Mensaje privado de prueba")==null,"Send DM");Assert(social.Invite(nyx)==null,"Party invitation");
                    social.SwitchLocalProfile(nyx);Assert(social.Conversation(self).Count==1,"Recipient sees DM");Assert(social.AcceptInvite(self)==null,"Accept party invitation");Assert(social.SetReady(true)==null,"Peer ready");social.SetAppearance(2);
                    social.SwitchLocalProfile(self);social.SetReady(true);Assert(social.Send("party","Probando la party local")==null,"Party chat");
                    var reloaded=new LocalSocialService(Path.GetFullPath(file));Assert(reloaded.Party.Count==2&&reloaded.Conversation(nyx).Count==1,"Persistence round trip");
                    social.AddFriend("Tercer perfil");var third=social.Friends.Last().Id;social.SwitchLocalProfile(third);Assert(social.Conversation(nyx).Count==0,"DM privacy between profiles");social.SwitchLocalProfile(self);
                    hub.UseLocalService(social);hub.Build();
                    Assert(hub.Stage&&hub.Stage.Ready,"Lobby camera, render texture and character");
                    foreach(var page in new[]{HubPage.Home,HubPage.Loadout,HubPage.Progression,HubPage.Marketplace,HubPage.Auction,HubPage.Settings,HubPage.Social}){hub.Navigate(page);Assert(hub.Page==page,"Screen "+page);}
                    var video=RiftVideoSettings.Ensure();var original=video.Current.Copy();var test=original.Copy();test.Exposure=.5f;test.Fov=100;video.Preview(test);Assert(video.AwaitingConfirmation&&video.Current.Fov==100,"Video preview");video.Revert();Assert(!video.AwaitingConfirmation&&video.Current.Fov==original.Fov,"Video rollback");
                    hub.Navigate(HubPage.Home);Find(hub,"JUGAR EL RIFT").onClick.Invoke();Assert(hub.Creator.Maps[hub.Creator.MapselectionDropdown.value].Name=="El Rift","Direct launch configured");Find(hub,"CANCELAR").onClick.Invoke();
                    report+="Real hosting cancelled at confirmation; no network session started.\n";
                    hub.Navigate(HubPage.Home);
                }
                else if(step==1){SprintUIProbe.Capture(hub,"Documentation/rift-lobby-3d.png",1920,1080);hub.Navigate(HubPage.Loadout);}
                else if(step==2){SprintUIProbe.Capture(hub,"Documentation/rift-equipment-3d.png",1920,1080);hub.Navigate(HubPage.Marketplace);}
                else if(step==3){
                    Assert(hub.GetComponentsInChildren<RiftModelView>().Length>=2,"3D product viewer");SprintUIProbe.Capture(hub,"Documentation/rift-marketplace-3d.png",1920,1080);
                    long balance=hub.Services.Balances.AncientGold;Find(hub,"COMPRAR COSMÉTICO").onClick.Invoke();Find(hub,"CONFIRMAR").onClick.Invoke();Assert(hub.Services.Marketplace.Products.Any(p=>p.Owned)&&hub.Services.Balances.AncientGold==balance,"Purchase simulation isolated");
                    Find(hub,"SUBASTAS").onClick.Invoke();
                }
                else if(step==4){SprintUIProbe.Capture(hub,"Documentation/rift-auction-3d.png",1920,1080);hub.Navigate(HubPage.Settings);}
                else if(step==5){SprintUIProbe.Capture(hub,"Documentation/rift-video-options.png",1920,1080);hub.Navigate(HubPage.Social);}
                else if(step==6){SprintUIProbe.Capture(hub,"Documentation/rift-friends-console.png",1920,1080);hub.Navigate(HubPage.Home);}
                else if(step==7){hub.Social.SetAppearance(0);hub.Navigate(HubPage.Loadout);Find(hub,"VER PERSONAJE").onClick.Invoke();}
                else if(step>=8&&step<=12){
                    int index=step-8;SprintUIProbe.Capture(hub,"Documentation/rift-character-"+index+".png",1920,1080);
                    if(index<4){hub.Social.SetAppearance(index+1);hub.Navigate(HubPage.Loadout);Find(hub,"VER PERSONAJE").onClick.Invoke();}
                }
                else{
                    File.WriteAllText("Documentation/frontend-local-validation.txt",report+"All checks passed. Local profiles, messages and party are single-device data.\n");SessionState.SetInt(Key+"Result",0);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;return;
                }
                step++;started=EditorApplication.timeSinceStartup;
            }catch(Exception e){Debug.LogException(e);File.WriteAllText("Documentation/frontend-local-validation.txt",report+"FAIL: "+e);SessionState.SetInt(Key+"Result",1);EditorApplication.update-=Tick;EditorApplication.isPlaying=false;}
        }
        static Button Find(RiftHub hub,string name)=>hub.GetComponentsInChildren<Button>().Last(b=>b.name==name);
        static void CheckPoses(FrontEndCatalog catalog)
        {
            var result="IDS NATIVE HUMANOID POSE CHECK\n";
            int left=Array.IndexOf(HumanTrait.MuscleName,"Left Upper Leg In-Out"),right=Array.IndexOf(HumanTrait.MuscleName,"Right Upper Leg In-Out");
            foreach(var model in catalog.Characters){
                var clone=UnityEngine.Object.Instantiate(model.Prefab);clone.transform.position=new Vector3(0,-10000,0);
                var animator=clone.GetComponent<Animator>();Assert(animator&&animator.isHuman,"Native avatar "+model.Name);animator.Rebind();animator.Update(0);
                var stance=clone.GetComponent<NaturalLegStance>();stance.RelaxedPreviewIdle=false;
                using(var handler=new HumanPoseHandler(animator.avatar,animator.transform)){
                    var before=new HumanPose();handler.GetHumanPose(ref before);
                    float beforeWidth=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,animator.GetBoneTransform(HumanBodyBones.RightFoot).position)/Mathf.Max(.001f,animator.humanScale);
                    stance.ApplyNow();var after=new HumanPose();handler.GetHumanPose(ref after);
                    Assert(Mathf.Abs(after.muscles[left])<=.181f&&Mathf.Abs(after.muscles[right])<=.181f,"Leg muscle bounds "+model.Name);
                    float afterWidth=Vector3.Distance(animator.GetBoneTransform(HumanBodyBones.LeftFoot).position,animator.GetBoneTransform(HumanBodyBones.RightFoot).position)/Mathf.Max(.001f,animator.humanScale);
                    result+=$"{model.Name}: in-out ({before.muscles[left]:F3},{before.muscles[right]:F3}) -> ({after.muscles[left]:F3},{after.muscles[right]:F3}); foot separation / human scale {beforeWidth:F3} -> {afterWidth:F3}; humanScale={animator.humanScale}\n";
                }
                UnityEngine.Object.DestroyImmediate(clone);
            }
            File.WriteAllText("Documentation/ids-leg-stance-validation.txt",result+"Native lobby rigs checked. Full gameplay motion/IK still requires visual playtest.\n");
        }
        static void Assert(bool condition,string name){if(!condition)throw new Exception(name);report+=name+": PASS\n";}
    }
}
