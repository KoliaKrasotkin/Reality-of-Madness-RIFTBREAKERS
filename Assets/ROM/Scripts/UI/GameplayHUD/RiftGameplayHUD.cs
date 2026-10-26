using System.Linq;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using MultiFPS.UI;
using ROM.UI.Sprint01;
using ROM.Horde;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
namespace ROM.UI.Gameplay
{
    [DefaultExecutionOrder(900)]
    public sealed partial class RiftGameplayHUD:MonoBehaviour
    {
        public RiftHUDTheme Theme;
        public RectTransform Design;
        public Text Zone,Gold,Checkpoint,MapZone,Coordinates,CharacterName,ClassName,Level,Life,Resource,TargetName,TargetLife,TargetStats,TargetCaption;
        public RiftHUDGraphic LifeFill,ResourceFill,TargetFill;
        public RectTransform TargetPanel;
        public Text ExperienceLabel;public Image ExperienceFill;public Image Portrait;
        public RectTransform HoverPanel;public Text HoverName,HoverInfo;public Text[] KeyLabels,ShortcutLabels;
        public Button[] Abilities;
        public Image[] Icons,Cooldowns;
        public Text[] Timers;
        public RiftHUDMinimap Minimap;
        RiftCooldownVisuals cooldownVisuals;Text bagButtonLabel;ChatUI observedChat;GameObject[] chatFrames;
        RiftHUDTargetMarker marker;Canvas canvas;float cleanupAt,nextContent;Health selected;int legacyPasses;MultiFPS.UI.HUD.UICharacter legacyCharacter;
        public Health SelectedTarget=>selected;public RiftWorldTarget SelectedNPC=>RiftMMOController.Instance?RiftMMOController.Instance.SelectedWorld:null;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Install(){SceneManager.sceneLoaded-=Loaded;SceneManager.sceneLoaded+=Loaded;}
        static void Loaded(Scene scene,LoadSceneMode mode){if(scene.name=="map_rom_horde"&&!FindFirstObjectByType<RiftGameplayHUD>()){var prefab=Resources.Load<GameObject>("RiftGameplayHUD");if(prefab)Instantiate(prefab);}}
        void Awake(){if(!FindFirstObjectByType<RiftCombatNumbers>())new GameObject("Rift combat numbers").AddComponent<RiftCombatNumbers>();canvas=GetComponent<Canvas>();cooldownVisuals=gameObject.AddComponent<RiftCooldownVisuals>();for(int i=0;i<Abilities.Length;i++){int slot=i;Abilities[i].onClick.AddListener(()=>Use(slot));var drag=Abilities[i].gameObject.GetComponent<RiftHotbarSlot>();if(!drag)drag=Abilities[i].gameObject.AddComponent<RiftHotbarSlot>();drag.Slot=slot;}CreateMarker();CreateBagDock();ModernizeLayout();gameObject.AddComponent<RiftUnitFrames>();gameObject.AddComponent<RiftHUDPanelDock>();gameObject.AddComponent<RiftCombatAlerts>();gameObject.AddComponent<RiftWorldLabelOcclusion>();gameObject.AddComponent<RiftSessionChatHUD>();}
        public void Use(int slot){if(RiftHotbar.Dragging||RiftControls.TextEntryFocused||ClientFrontend.Pause||!UICursorManager.GamePlayInput())return;int ability=RiftHotbar.Resolve(slot);if(ability<=0)return;var player=ClientFrontend.ClientPlayerInstance;if(!player)return;var combat=player.GetComponent<AbilityController>();if(combat&&combat.Prototype)combat.Request(ability);else if(ability<=2){RiftMMOController.Instance?.StartAutoAttack();player.CmdUseClassAbility((byte)(ability-1));}}
        void CreateBagDock(){
            var button=RiftWidgets.Button(Design,"BOLSA ["+RiftControls.ActionLabel("Inventory")+"]",0,0,238,58,OpenInventory);
            button.name="Always accessible inventory";bagButtonLabel=button.GetComponentInChildren<Text>();
            var rect=(RectTransform)button.transform;rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(1,0);rect.anchoredPosition=new Vector2(-24,260);
            var tooltip=button.gameObject.AddComponent<RiftTooltipTrigger>();tooltip.Title="Bolsa de provisiones";tooltip.Description="Revisá tus pociones, cantidades y equipo. El acceso indicado abre o cierra la bolsa; Escape vuelve al juego.";tooltip.ActionId="Inventory";
        }
        public void OpenInventory()=>Open(HubPage.Inventory);
        public void OpenCharacter()=>Open(HubPage.Character);
        public void OpenAbilities(){Open(HubPage.Abilities);var hub=UIPauseMenu._instance?UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true):null;if(hub)hub.ShowSkills();}
        public void OpenTalents(){Open(HubPage.Abilities);var hub=UIPauseMenu._instance?UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true):null;if(hub)hub.ShowTalents();}
        public void OpenParty(){OpenFriends();var hub=UIPauseMenu._instance?UIPauseMenu._instance.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true):null;if(hub)hub.ShowPartyChat();}
        public void OpenJournal()=>Open(HubPage.Missions);public void OpenAchievements()=>Open(HubPage.Progression);public void OpenFriends()=>Open(HubPage.Social);
        public void OpenSettings()=>Open(HubPage.Settings);
        public void Open(HubPage page){if(page==HubPage.Inventory&&RiftVillageUI.Instance&&RiftVillageUI.Instance.VendorWindow)return;var pause=UIPauseMenu._instance;if(!pause)return;var current=pause.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true);if(pause.InterfaceShowing&&current&&current.Page==page){pause.ShowInterface(false);RiftControls.InterfaceConsumedFrame=Time.frameCount;return;}pause.ShowInterface(true);RiftControls.InterfaceConsumedFrame=Time.frameCount;var hub=pause.pauseMenuOverlay.GetComponentInChildren<RiftHub>(true);if(hub){if(!hub.Design)hub.Build();hub.ShowGameplayPage(page);}}
        void LateUpdate()
        {
            var player=ClientFrontend.ClientPlayerInstance;var actor=player?player.MyCharacter:null;
            bool visible=actor&&actor.Health.CurrentHealth>0&&!ClientFrontend.Hub;
            canvas.enabled=visible;
            if(legacyCharacter!=MultiFPS.UI.HUD.UICharacter._instance){legacyCharacter=MultiFPS.UI.HUD.UICharacter._instance;legacyPasses=0;}
            if(legacyPasses<4&&Time.unscaledTime>cleanupAt){legacyPasses++;cleanupAt=Time.unscaledTime+1;var measured=RiftFrameDiagnostics.Begin();HideLegacy();RiftFrameDiagnostics.End(1,measured);}
            if(ChatUI.Instance&&ChatUI.Instance.chatUI){var chat=ChatUI.Instance;var group=chat.chatUI.GetComponent<CanvasGroup>();if(!group)group=chat.chatUI.AddComponent<CanvasGroup>();group.alpha=chat.ChatWriting?1:0;group.blocksRaycasts=chat.ChatWriting;}
            if(observedChat!=ChatUI.Instance){observedChat=ChatUI.Instance;chatFrames=observedChat?observedChat.GetComponentsInChildren<RectTransform>(true).Where(t=>t.name=="RiftChatFrame").Select(t=>t.gameObject).ToArray():null;}if(chatFrames!=null)foreach(var frame in chatFrames)if(frame)frame.SetActive(observedChat&&observedChat.ChatWriting);
            if(!visible){if(marker)marker.gameObject.SetActive(false);if(HoverPanel)HoverPanel.gameObject.SetActive(false);return;}
            var hp=actor.Health;var combat=player.GetComponent<AbilityController>();bool warrior=combat&&combat.Prototype;var pos=actor.transform.position;
            if(Time.unscaledTime>=nextContent){nextContent=Time.unscaledTime+.1f;if(bagButtonLabel)bagButtonLabel.text="Inventario ["+RiftControls.ActionLabel("Inventory")+"]";
            var world=ROM.Horde.Horde.Instance;bool safe=world&&world.IsSafe(actor.transform.position);
            bool crelium=ROM.Tutorial.Crilium.CriliumSession.Active;Zone.text=crelium?"CRELIUM":safe?"ZONA SEGURA":"EL RIFT";Zone.color=safe?new Color(.2f,1,.48f):Theme.BrightGold;
            Gold.text=crelium?"EL PODER DE NERTÖR":RiftExplorationProgress.Gold.ToString("N0")+" ORO LOCAL";
            Checkpoint.text=crelium?"Crelium":world?world.LocalCheckpoint:"El Rift";MapZone.text=Checkpoint.text;
            Coordinates.text=$"X: {pos.x:0}  Y: {pos.z:0}";
            CharacterName.text=hp.CharacterName;ClassName.text=warrior?"Warrior":player.ClassDefinition?player.ClassDefinition.Name:"";
            Level.text=(combat?combat.Level:Theme.InitialLevel).ToString();if(ExperienceLabel&&combat){bool cap=combat.Level>=combat.Progression.MaximumLevel;int xp=combat.Progression.InLevel(combat.Experience);ExperienceLabel.text=cap?"NIVEL MÁXIMO":"XP "+xp+" / "+combat.Progression.Required(combat.Level);ExperienceFill.fillAmount=cap?1:(float)xp/combat.Progression.Required(combat.Level);}Life.text=$"{hp.CurrentHealth:N0} / {hp.MaxHealth:N0}";SetFill(LifeFill,(float)hp.CurrentHealth/Mathf.Max(1,hp.MaxHealth));
            float amount=warrior?combat.Resource:0,max=warrior?combat.Class.MaximumResource:100;
            Resource.text=$"Furia {amount:0} / {max:0}";SetFill(ResourceFill,amount/Mathf.Max(1,max));
            if(Theme.Portraits!=null&&Theme.Portraits.Length>0)Portrait.sprite=Theme.Portraits[Mathf.Clamp(UserSettings.CharacterSkinID,0,Theme.Portraits.Length-1)];
            double now=NetworkTime.time;
            for(int i=0;i<Abilities.Length;i++){
                int id=RiftHotbar.Resolve(i);var definition=warrior&&id>0?combat.Definition(id):null;bool unlocked=warrior?combat.IsAbilityLearned(id):id>0&&id<=2;
                double remain=warrior&&combat.Ready.Count>id?System.Math.Max(0,combat.Ready[id]-now):!warrior&&id<=2?System.Math.Max(0,(id==1?player.FirstReadyAt:player.SecondReadyAt)-now):0;
                double ownRemain=remain,globalRemain=warrior?System.Math.Max(0,combat.GlobalReady-now):0;if(globalRemain>remain)remain=globalRemain;
                bool enough=!definition||definition.Cost<=amount;
                Abilities[i].interactable=unlocked&&enough&&remain<=0;
                Icons[i].gameObject.SetActive(unlocked);if(unlocked){if(definition&&definition.Icon)Icons[i].sprite=definition.Icon;else if(!warrior&&Theme.AbilityIcons.Length>=id)Icons[i].sprite=Theme.AbilityIcons[id-1];Icons[i].color=enough?Color.white:new Color(.72f,.65f,.65f);}
                var locked=Abilities[i].transform.Find("Lock");if(locked)locked.gameObject.SetActive(id>0&&!unlocked);
                float duration=definition?combat.AbilityCooldown(id,definition):1;if(globalRemain>ownRemain){var active=warrior?combat.Definition(combat.Casting):null;duration=active?active.Recovery:definition?definition.Recovery:1;}cooldownVisuals.Apply(i,unlocked,enough,(float)remain,duration);
            }
            SelectTarget(actor);
            if(KeyLabels!=null)for(int i=0;i<KeyLabels.Length;i++)if(KeyLabels[i]){string label=RiftControls.ActionLabel("Ability"+i);KeyLabels[i].text=label;KeyLabels[i].fontSize=label.Length>3?10:17;}
            if(ShortcutLabels!=null){string[] ids={"Inventory","Character","Abilities","Pause","Talents","Journal","Achievements","Friends"};for(int i=0;i<ShortcutLabels.Length&&i<ids.Length;i++)if(ShortcutLabels[i])ShortcutLabels[i].text=RiftControls.ActionLabel(ids[i]);}
            }
            RefreshHover(actor);if(ClientFrontend.Pause&&HoverPanel)HoverPanel.gameObject.SetActive(false);Minimap.Refresh(actor);if(marker&&marker.gameObject.activeSelf)marker.Follow(GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():Camera.main);
            if(UICursorManager.GamePlayInput()&&!RiftControls.Rebinding&&!RiftInterfaceCursor.Typing&&RiftControls.InterfaceConsumedFrame!=Time.frameCount){if(RiftControls.Pressed("Talents"))OpenTalents();else if(RiftControls.Pressed("Friends"))OpenFriends();else if(RiftControls.Pressed("Party"))OpenParty();else if(RiftControls.Pressed("Equipment"))OpenCharacter();else if(RiftControls.Pressed("Journal"))Open(HubPage.Missions);else if(RiftControls.Pressed("Achievements"))Open(HubPage.Progression);else if(RiftInterfaceCursor.InventoryConsumedFrame!=Time.frameCount&&RiftControls.Pressed("Inventory"))Open(HubPage.Inventory);else if(RiftControls.Pressed("Character"))Open(HubPage.Character);else if(RiftControls.Pressed("Abilities"))OpenAbilities();else if(RiftControls.Pressed("Settings"))OpenSettings();foreach(var portal in RiftPortal.All)if(portal&&portal.Definition&&Vector3.Distance(pos,portal.transform.position)<portal.Definition.InteractRange&&RiftControls.Pressed("Interact")){if(combat)combat.CmdPortal(portal.Definition.Id);break;}}

        }
        static void SetFill(RiftHUDGraphic bar,float fill){if(Mathf.Approximately(bar.Fill,fill))return;bar.Fill=fill;bar.SetVerticesDirty();}
        void SelectTarget(CharacterInstance actor)
        {
            var targeting=RiftMMOController.Instance;selected=targeting?targeting.Selected:null;
            var npc=targeting?targeting.SelectedWorld:null;TargetPanel.gameObject.SetActive(selected||npc);var frame=TargetPanel.Find("Target health frame");if(frame)frame.gameObject.SetActive(selected);TargetFill.gameObject.SetActive(selected);TargetLife.gameObject.SetActive(selected);
            if(npc){marker.SetWorld(npc);marker.gameObject.SetActive(true);marker.Follow(GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():Camera.main);TargetName.text=npc.DisplayName;TargetStats.text=npc.Role;TargetCaption.text="Amistoso · ["+RiftControls.ActionLabel("Interact")+"] / clic izquierdo";TargetName.color=new Color(.35f,1,.6f);return;}
            marker.SetTarget(selected,selected&&selected.Team==actor.Health.Team);if(!selected)return;marker.gameObject.SetActive(true);marker.Follow(GameplayCamera._instance?GameplayCamera._instance.GetComponent<Camera>():Camera.main);TargetName.color=new Color(.94f,.93f,.98f);
            TargetName.text=selected.CharacterName.ToUpperInvariant();TargetLife.text=$"{selected.CurrentHealth:N0} / {selected.MaxHealth:N0}";SetFill(TargetFill,(float)selected.CurrentHealth/Mathf.Max(1,selected.MaxHealth));
            if(selected.TrainingTarget){TargetStats.text=$"DAÑO: {selected.TrainingLastDamage:N0}  ·  TOTAL: {selected.TrainingTotalDamage:N0}";TargetCaption.text="No ataca  ·  No da recompensas";}
            else{var enemy=selected.GetComponent<RiftEnemyCombat>();var native=selected.GetComponent<MultiFPS.Gameplay.RiftEnemies.RiftEnemyAgent>();TargetStats.text=native&&native.Definition?native.Definition.Role:enemy&&enemy.Definition?enemy.Definition.name.Replace("_"," "):"";TargetCaption.text=selected==actor.Health?"Vos":selected.Team==actor.Health.Team?"Aliado":native&&native.SkillEnds>NetworkTime.time?native.Definition.SkillName+"  "+(native.SkillEnds-NetworkTime.time).ToString("0.0")+" s":enemy&&enemy.CastEnds>NetworkTime.time?"PREPARANDO ATAQUE  "+(enemy.CastEnds-NetworkTime.time).ToString("0.0")+" s":"";}
        }
        void RefreshHover(CharacterInstance actor)
        {
            if(!HoverPanel)return;var input=RiftMMOController.Instance;bool show=input&&input.Available&&!input.Dragging&&!RiftMMOController.PointerOnUI;
            var health=show?input.Hovered:null;var hoverCollider=show&&input.HasHoverHit?input.HoverHit.collider:null;RiftVillageNpc npc=hoverCollider?hoverCollider.GetComponentInParent<RiftVillageNpc>():null;
            HoverPanel.gameObject.SetActive(show&&(health||npc));if(!show||(!health&&!npc))return;
            if(health){HoverName.text=health.CharacterName;var enemy=health.GetComponent<RiftEnemyCombat>();string role=health.TrainingTarget?"Muñeco de entrenamiento":enemy&&enemy.Definition?enemy.Definition.DisplayName:health.Team==actor.Health.Team?"Aliado":"Enemigo";HoverInfo.text=role+"\nVida: "+health.CurrentHealth.ToString("N0")+" / "+health.MaxHealth.ToString("N0");}
            else{HoverName.text=npc.DisplayName;HoverInfo.text=npc.Role+(npc.Trades?"\nComerciante · Oro local":"");}
            if(Mouse.current!=null){var root=(RectTransform)canvas.transform;RectTransformUtility.ScreenPointToLocalPointInRectangle(root,Mouse.current.position.ReadValue(),null,out var point);var size=HoverPanel.sizeDelta;point+=new Vector2(18,-18);point.x=Mathf.Clamp(point.x,root.rect.xMin+8,root.rect.xMax-size.x-8);point.y=Mathf.Clamp(point.y,root.rect.yMin+size.y+8,root.rect.yMax-8);HoverPanel.anchoredPosition=point;}
        }
        void HideLegacy(){foreach(var feed in FindObjectsByType<MultiFPS.UI.HUD.UIKillFeedV2>(FindObjectsSortMode.None))HideGroup(feed.gameObject);var ui=MultiFPS.UI.HUD.UICharacter._instance;if(ui){var root=ui.transform.Find("GameplayHUD");if(root){root.gameObject.SetActive(true);HideGroup(root.gameObject);}}foreach(var old in FindObjectsByType<ROM.Horde.HordeHUD>(FindObjectsSortMode.None)){var c=old.GetComponent<Canvas>();if(c)c.enabled=false;}foreach(var label in FindObjectsByType<TrainingTargetDisplay>(FindObjectsSortMode.None))foreach(var r in label.GetComponentsInChildren<MeshRenderer>())if(r.GetComponent<TextMesh>())r.enabled=false;var ring=GameObject.Find("Training circle");if(ring){var r=ring.GetComponent<LineRenderer>();if(r)r.enabled=false;}var popup=MultiFPS.UI.HUD.HUDDamagePopup.Instance;if(popup&&popup.GridParent){popup.GridParent.gameObject.SetActive(true);HideGroup(popup.GridParent.gameObject);}}
        static void HideGroup(GameObject go){var group=go.GetComponent<CanvasGroup>();if(!group)group=go.AddComponent<CanvasGroup>();group.alpha=0;group.blocksRaycasts=false;group.interactable=false;}
        void CreateMarker(){var go=new GameObject("Selected enemy - client presentation");marker=go.AddComponent<RiftHUDTargetMarker>();marker.Circle=Line(go,"Small red ground circle",64);marker.Circle.loop=true;for(int i=0;i<64;i++){float a=i*2*Mathf.PI/64;marker.Circle.SetPosition(i,new Vector3(Mathf.Cos(a)*.62f,0,Mathf.Sin(a)*.62f));}marker.Diamond=Line(go,"Floating red diamond",4);marker.HostileMaterial=Theme.TargetMaterial;marker.FriendlyMaterial=Theme.FriendlyTargetMaterial;marker.Diamond.loop=true;marker.Diamond.SetPositions(new[]{new Vector3(0,.19f,0),new Vector3(.1f,0,0),new Vector3(0,-.19f,0),new Vector3(-.1f,0,0)});go.SetActive(false);}
        LineRenderer Line(GameObject parent,string name,int count){var r=new GameObject(name).AddComponent<LineRenderer>();r.transform.SetParent(parent.transform);r.useWorldSpace=false;r.positionCount=count;r.widthMultiplier=.025f;r.sharedMaterial=Theme.TargetMaterial;r.startColor=r.endColor=new Color(1,.12f,.1f);r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;r.receiveShadows=false;return r;}
        void OnDestroy(){if(marker)Destroy(marker.gameObject);}
    }
}
