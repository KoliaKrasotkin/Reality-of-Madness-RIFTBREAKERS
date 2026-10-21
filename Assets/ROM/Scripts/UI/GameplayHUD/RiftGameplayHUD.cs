using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.UI.Sprint01;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace ROM.UI.Gameplay
{
    [DefaultExecutionOrder(900)]
    public sealed class RiftGameplayHUD:MonoBehaviour
    {
        public RiftHUDTheme Theme;
        public RectTransform Design;
        public Text Zone,Gold,Checkpoint,MapZone,Coordinates,CharacterName,ClassName,Level,Life,Resource,TargetName,TargetLife,TargetStats,TargetCaption;
        public RiftHUDGraphic LifeFill,ResourceFill,TargetFill;
        public RectTransform TargetPanel;
        public Image Portrait;
        public Button[] Abilities;
        public Image[] Icons,Cooldowns;
        public Text[] Timers;
        public RiftHUDMinimap Minimap;
        bool cursorHeld;ChatUI observedChat;GameObject[] chatFrames;
        RiftHUDTargetMarker marker;Canvas canvas;float cleanupAt;Health selected;
        public Health SelectedTarget=>selected;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){if(scene.name=="map_rom_horde"&&!FindFirstObjectByType<RiftGameplayHUD>()){var prefab=Resources.Load<GameObject>("RiftGameplayHUD");if(prefab)Instantiate(prefab);}}
        void Awake(){canvas=GetComponent<Canvas>();for(int i=0;i<Abilities.Length;i++){int slot=i;Abilities[i].onClick.AddListener(()=>Use(slot));}CreateMarker();}
        void Use(int slot){var player=ClientFrontend.ClientPlayerInstance;if(!player)return;var combat=player.GetComponent<AbilityController>();if(combat&&combat.Prototype)combat.Request(slot);else if(slot==1)player.CmdUseClassAbility(0);else if(slot==2)player.CmdUseClassAbility(1);}
        public void OpenInventory()=>Open(HubPage.Inventory);
        public void OpenCharacter()=>Open(HubPage.Character);
        public void OpenAbilities()=>Open(HubPage.Abilities);
        public void OpenSettings()=>Open(HubPage.Settings);
        public void Open(HubPage page){var pause=UIPauseMenu._instance;if(!pause)return;pause.ShowPauseMenu(true);var hub=pause.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true);if(hub){if(!hub.Design)hub.Build();hub.ShowGameplayPage(page);}}
        void LateUpdate()
        {
            var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;
            bool visible=actor&&actor.Health.CurrentHealth>0&&!ClientFrontend.Pause&&!ClientFrontend.Hub;
            canvas.enabled=visible;bool wantCursor=visible&&Keyboard.current!=null&&Keyboard.current.leftAltKey.isPressed;if(wantCursor!=cursorHeld){cursorHeld=wantCursor;if(cursorHeld)UICursorManager.RequestCursor(gameObject);else UICursorManager.ReleaseCursor(gameObject);}
            if(Time.unscaledTime>cleanupAt){cleanupAt=Time.unscaledTime+1;HideLegacy();}
            if(ChatUI.Instance&&ChatUI.Instance.chatUI){var chat=ChatUI.Instance;var group=chat.chatUI.GetComponent<CanvasGroup>();if(!group)group=chat.chatUI.AddComponent<CanvasGroup>();group.alpha=chat.ChatWriting?1:0;group.blocksRaycasts=chat.ChatWriting;}
            if(observedChat!=ChatUI.Instance){observedChat=ChatUI.Instance;chatFrames=observedChat?observedChat.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="RiftChatFrame").Select(t=>t.gameObject).ToArray():null;}if(chatFrames!=null)foreach(var frame in chatFrames)if(frame)frame.SetActive(observedChat&&observedChat.ChatWriting);
            if(!visible){if(marker)marker.gameObject.SetActive(false);return;}
            var hp=actor.Health;var combat=player.GetComponent<AbilityController>();bool warrior=combat&&combat.Prototype;
            var world=ROM.Horde.Horde.Instance;bool safe=world&&world.IsSafe(actor.transform.position);
            Zone.text=safe?"ZONA SEGURA":"EL RIFT";Zone.color=safe?new Color(.2f,1,.48f):Theme.BrightGold;
            Gold.text=RiftExplorationProgress.Gold.ToString("N0")+" ORO LOCAL";
            Checkpoint.text=world?world.LocalCheckpoint:"El Rift";MapZone.text=Checkpoint.text;
            var pos=actor.transform.position;Coordinates.text=$"X: {pos.x:0}  Y: {pos.z:0}";
            CharacterName.text=hp.CharacterName;ClassName.text=warrior?"Warrior":player.ClassDefinition?player.ClassDefinition.Name:"";
            Level.text=Theme.InitialLevel.ToString();Life.text=$"{hp.CurrentHealth:N0} / {hp.MaxHealth:N0}";LifeFill.Fill=(float)hp.CurrentHealth/Mathf.Max(1,hp.MaxHealth);LifeFill.SetVerticesDirty();
            float amount=warrior?combat.Resource:0,max=warrior?combat.Class.MaximumResource:100;
            Resource.text=$"{amount:0} / {max:0}";ResourceFill.Fill=amount/Mathf.Max(1,max);ResourceFill.SetVerticesDirty();
            if(Theme.Portraits!=null&&Theme.Portraits.Length>0)Portrait.sprite=Theme.Portraits[Mathf.Clamp(UserSettings.CharacterSkinID,0,Theme.Portraits.Length-1)];
            double now=NetworkTime.time;
            for(int i=0;i<Abilities.Length;i++){
                var definition=warrior?combat.Definition(i):null;bool unlocked=warrior?definition&&definition.Implemented:i==1||i==2;
                double remain=warrior&&combat.Ready.Count>i?System.Math.Max(0,combat.Ready[i]-now):0;
                if(warrior&&combat.GlobalReady>now)remain=System.Math.Max(remain,combat.GlobalReady-now);
                bool enough=!definition||definition.Cost<=amount;
                Abilities[i].interactable=unlocked&&enough&&remain<=0;
                Icons[i].gameObject.SetActive(unlocked);if(unlocked){if(definition&&definition.Icon)Icons[i].sprite=definition.Icon;Icons[i].color=enough?Color.white:new Color(.72f,.65f,.65f);}
                var locked=Abilities[i].transform.Find("Lock");if(locked)locked.gameObject.SetActive(!unlocked);
                Cooldowns[i].fillAmount=unlocked&&remain>0?(float)remain/Mathf.Max(.1f,definition?definition.Cooldown:1):0;
                Timers[i].text=unlocked&&remain>=1?Mathf.CeilToInt((float)remain).ToString():"";
            }
            SelectTarget(actor);Minimap.Refresh(actor);
            if(UICursorManager.GamePlayInput()&&Keyboard.current!=null){if(warrior&&Keyboard.current.qKey.wasPressedThisFrame)Use(0);if(warrior&&Keyboard.current.digit2Key.wasPressedThisFrame)Use(2);if(Keyboard.current.iKey.wasPressedThisFrame)Open(HubPage.Inventory);else if(Keyboard.current.bKey.wasPressedThisFrame)Open(HubPage.Character);else if(Keyboard.current.kKey.wasPressedThisFrame)Open(HubPage.Abilities);foreach(var portal in RiftPortal.All)if(portal&&portal.Definition&&Vector3.Distance(pos,portal.transform.position)<portal.Definition.InteractRange&&Keyboard.current.eKey.wasPressedThisFrame){if(combat)combat.CmdPortal(portal.Definition.Id);break;}}
        }
        void SelectTarget(CharacterInstance actor)
        {
            selected=null;float best=.86f;var forward=Quaternion.Euler(0,actor.Input.LookY,0)*Vector3.forward;
            foreach(var p in GameManager.Players){if(!p||!p.BOT||!p.MyCharacter||p.MyCharacter.Health.CurrentHealth<=0)continue;var delta=p.MyCharacter.transform.position-actor.transform.position;delta.y=0;if(delta.sqrMagnitude>45*45)continue;float score=Vector3.Dot(delta.normalized,forward);if(score>best){best=score;selected=p.MyCharacter.Health;}}
            TargetPanel.gameObject.SetActive(selected);marker.SetTarget(selected);if(!selected)return;marker.gameObject.SetActive(true);marker.Follow(GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():Camera.main);
            TargetName.text=selected.CharacterName.ToUpperInvariant();TargetLife.text=$"{selected.CurrentHealth:N0} / {selected.MaxHealth:N0}";TargetFill.Fill=(float)selected.CurrentHealth/Mathf.Max(1,selected.MaxHealth);TargetFill.SetVerticesDirty();
            if(selected.TrainingTarget){TargetStats.text=$"DAÑO: {selected.TrainingLastDamage:N0}  ·  TOTAL: {selected.TrainingTotalDamage:N0}";TargetCaption.text="No ataca  ·  No da recompensas";}
            else{var enemy=selected.GetComponent<RiftEnemyCombat>();TargetStats.text=enemy&&enemy.Definition?enemy.Definition.name.Replace("_"," "):"";TargetCaption.text=enemy&&enemy.CastEnds>NetworkTime.time?"PREPARANDO ATAQUE  "+(enemy.CastEnds-NetworkTime.time).ToString("0.0")+" s":"";}
        }
        void HideLegacy(){var ui=MultiFPS.UI.HUD.UICharacter._instance;if(ui){var root=ui.transform.Find("GameplayHUD");if(root){root.gameObject.SetActive(true);HideGroup(root.gameObject);}}foreach(var old in FindObjectsByType<ROM.Horde.HordeHUD>(FindObjectsSortMode.None)){var c=old.GetComponent<Canvas>();if(c)c.enabled=false;}foreach(var label in FindObjectsByType<TrainingTargetDisplay>(FindObjectsSortMode.None))foreach(var r in label.GetComponentsInChildren<MeshRenderer>())if(r.GetComponent<TextMesh>())r.enabled=false;var ring=GameObject.Find("Training circle");if(ring){var r=ring.GetComponent<LineRenderer>();if(r)r.enabled=false;}var popup=MultiFPS.UI.HUD.HUDDamagePopup.Instance;if(popup&&popup.GridParent){popup.GridParent.gameObject.SetActive(true);HideGroup(popup.GridParent.gameObject);}}
        static void HideGroup(GameObject go){var group=go.GetComponent<CanvasGroup>();if(!group)group=go.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;}
        void CreateMarker(){var go=new GameObject("Selected enemy - client presentation");marker=go.AddComponent<RiftHUDTargetMarker>();marker.Circle=Line(go,"Small red ground circle",64);marker.Circle.loop=true;for(int i=0;i<64;i++){float a=i*2*Mathf.PI/64;marker.Circle.SetPosition(i,new Vector3(Mathf.Cos(a)*.62f,0,Mathf.Sin(a)*.62f));}marker.Diamond=Line(go,"Floating red diamond",4);marker.Diamond.loop=true;marker.Diamond.SetPositions(new[]{new Vector3(0,.19f,0),new Vector3(.1f,0,0),new Vector3(0,-.19f,0),new Vector3(-.1f,0,0)});go.SetActive(false);}
        LineRenderer Line(GameObject parent,string name,int count){var r=new GameObject(name).AddComponent<LineRenderer>();r.transform.SetParent(parent.transform);r.useWorldSpace=false;r.positionCount=count;r.widthMultiplier=.025f;r.sharedMaterial=Theme.TargetMaterial;r.startColor=r.endColor=new Color(1,.12f,.1f);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        void OnDestroy(){if(cursorHeld)UICursorManager.ReleaseCursor(gameObject);if(marker)Destroy(marker.gameObject);}
    }
}
