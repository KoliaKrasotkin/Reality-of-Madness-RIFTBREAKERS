using System;
using System.Collections.Generic;
using System.Linq;
using MultiFPS;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using W = ROM.UI.Sprint01.RiftWidgets;

namespace ROM.UI.Sprint01
{
    public enum HubPage { Home, Missions, Loadout, Lobby, Progression, Auction, Exchange, Marketplace, Settings, Social, Codex, Results, Inventory, Character, Abilities }
    public sealed partial class RiftHub : MonoBehaviour
    {
        public Sprite Background,Logo;
        public ROMUITheme Theme;
        public Font InterfaceFont;
        public Font ButtonFont;
        public FrontEndCatalog Catalog;
        public MultiFPS.Gameplay.RiftCombat.SpeciesDefinition CombatSpecies;
        public bool InGame;
        public Action ResumeRequested,LeaveRequested;
        public RectTransform ArsenalPanel;
        public GameObject LegacyMenu;
        public RoomCreator Creator;
        public RoomConnector Connector;
        public RectTransform Design;
        public HubPage Page {get;private set;}
        public SprintServices Services {get;private set;}
        public ISocialService Social {get;private set;}
        public RiftModelView Stage {get;private set;}
        RectTransform content,modal,fullGameplayHeader,compactGameplayHeader;
        RiftWarriorShowcase talentShowcase;
        Text toast;
        Image fallback;
        RiftModelView productView;
        VideoPreferences draft;
        string shopTab="Marketplace",selectedModel="",peer="party",chatDraft="",friendDraft="",query="";
        string settingsTab="Video";
        readonly List<Button> nav=new List<Button>();
        bool selectedOwned;
        bool videoDialog;
        Text videoTimer;
        int profileTestIndex;
        int emotePage;
        string homePanel="";
        float toastUntil;
        Text emoteStatus;
        public void Initialize()
        {
            if(Application.isPlaying){RiftInputUI.Ensure();ROM.Progression.RiftJournalView.Changed-=JournalChanged;ROM.Progression.RiftJournalView.Changed+=JournalChanged;}
            W.Body=InterfaceFont?InterfaceFont:Theme?Theme.Body:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // The condensed display font is restricted to headings. Body and navigation
            // use the existing Inter font to keep the new UI readable.
            W.Title=W.Body;
            W.ButtonFont=ButtonFont?ButtonFont:W.Body;
            if(Services==null)Services=SprintServices.CreateMock(Catalog);if(Application.isPlaying){RiftControls.Changed-=RefreshBoundLabels;RiftControls.Changed+=RefreshBoundLabels;}
            if(Social==null){Social=new LocalSocialService();Social.Changed+=SocialChanged;}
        }
        public void UseLocalService(LocalSocialService service)
        {
            if(Social!=null)Social.Changed-=SocialChanged;Social=service;Social.Changed+=SocialChanged;
        }
        public void UseSocialService(ISocialService service){if(Social!=null)Social.Changed-=SocialChanged;Social=service??throw new ArgumentNullException(nameof(service));Social.Changed+=SocialChanged;}
        public void UseServices(SprintServices services){Services=services??throw new ArgumentNullException(nameof(services));}
        void Start(){Initialize();RiftVideoSettings.Ensure();Build();if(InGame&&requestedGameplayPage.HasValue)Navigate(requestedGameplayPage.Value);}
        [ContextMenu("Reconstruir vista previa del menú")]
        public void Build()
        {
            Initialize();boundLabels.Clear();if(Design)DestroyImmediate(Design.gameObject);nav.Clear();productView=null;
            Design=W.Rect(transform,"SafeAreaDesign",0,0,1920,1080);
            fallback=W.Image(Design,"LoadingFallback",0,0,1920,1080,Color.white,Background);
            if(!InGame&&Catalog&&Catalog.StagePrefab&&Application.isPlaying){
                try{Stage=RiftModelView.Create(Design,530,20,1100,1040,Catalog,true);Stage.ConfigureMainMenuPresentation();Stage.ShowParty(Catalog,Social);fallback.enabled=!Stage.Ready;}
                catch(Exception e){Debug.LogError("Lobby stage failed: "+e.Message);if(Stage)Stage.gameObject.SetActive(false);fallback.enabled=true;}
            }
            if(!InGame){BuildModernMenuEnvironment();return;}
            var header=InGame?(fullGameplayHeader=W.Rect(Design,"Full gameplay header",0,0,1920,110)):Design;
            W.Image(header,"HeaderShade",0,0,1920,110,new Color(.015f,.018f,.027f,.96f));
            W.Image(header,"HeaderLine",0,108,1920,2,W.Gold);
            W.Image(header,"Brand",28,16,210,72,Color.white,Logo).preserveAspect=true;
            if(InGame){
                fallback.sprite=null;fallback.color=new Color(.015f,.012f,.02f,.94f);
                W.Label(header,"EL RIFT",260,20,170,65,22);
                BoundButton(header,"BOLSA","Inventory",440,24,230,58,()=>TogglePanel("Inventory",HubPage.Inventory));
                BoundButton(header,"PERSONAJE","Character",685,24,225,58,()=>TogglePanel("Character",HubPage.Character));
                BoundButton(header,"HABILIDADES","Abilities",925,24,245,58,()=>TogglePanel("Abilities",HubPage.Abilities));
                BoundButton(header,"DIARIO","Journal",1185,24,195,58,()=>TogglePanel("Journal",HubPage.Missions));
                BoundButton(header,"AJUSTES","Settings",1395,24,195,58,()=>TogglePanel("Settings",HubPage.Settings));
                BoundButton(header,"CERRAR","Pause",1605,24,275,58,()=>ResumeRequested?.Invoke());
                BuildCompactHeader();content=W.Rect(Design,"Screen",36,150,1848,810);toast=W.Label(Design,"",460,970,1200,36,20,W.Gold);Navigate(HubPage.Home);Fit();return;
            }
            W.Button(Design,"MENÚ",1640,24,240,58,()=>ToggleHomePanel("Menu"));
            W.Label(Design,"LOCAL",36,1030,180,25,15,W.Muted);
            content=W.Rect(Design,"Screen",36,168,1848,810);
            toast=W.Label(Design,"",460,970,1200,36,20,new Color(1,.78f,.48f));
            Navigate(HubPage.Home);Fit();
        }
        void Update()
        {
            PollBinding();
            HandleInterfaceKeys();
            if(!InGame&&!RiftControls.Rebinding&&RiftControls.EscapeConsumedFrame!=Time.frameCount&&RiftControls.Pressed("Pause")&&!modal){if(Page!=HubPage.Home){homePanel="";Navigate(HubPage.Home);}else if(!string.IsNullOrEmpty(homePanel)){homePanel="";Navigate(HubPage.Home);}}
            if(toast&&Time.unscaledTime>toastUntil)toast.text="";
            Fit();if(fallback&&Stage){fallback.enabled=!Stage.Ready;Stage.SetDisplay(Stage.Ready);}
            if(emoteStatus)emoteStatus.text=Stage&&!string.IsNullOrEmpty(Stage.CurrentEmoteName)?Stage.CurrentEmoteName:"Elegí un emote para tu personaje";
            if(videoDialog&&RiftVideoSettings.Instance){
                if(videoTimer)videoTimer.text="Restaurar en "+Mathf.CeilToInt(RiftVideoSettings.Instance.SecondsRemaining)+" s";
                if(!RiftVideoSettings.Instance.AwaitingConfirmation){videoDialog=false;if(modal)DestroyImmediate(modal.gameObject);draft=RiftVideoSettings.Instance.Current.Copy();Navigate(Page);Notify("Se restauraron los ajustes anteriores.");}
            }
        }
        void Fit()
        {
            if(!Design||Screen.width<=0||Screen.height<=0)return;var parent=(RectTransform)transform;var safe=Screen.safeArea;
            float scale=Mathf.Min(parent.rect.width*safe.width/Screen.width/1920f,parent.rect.height*safe.height/Screen.height/1080f);
            Design.anchorMin=Design.anchorMax=Design.pivot=new Vector2(.5f,.5f);Design.localScale=Vector3.one*scale;
            Design.anchoredPosition=new Vector2((safe.center.x/Screen.width-.5f)*parent.rect.width,(safe.center.y/Screen.height-.5f)*parent.rect.height);
            if(!InGame&&Stage)Stage.FitMainMenuViewport(540+parent.rect.height/(2*Mathf.Max(.01f,scale)));
        }
        public void SetHint(string message){if(toast)toast.text=message;toastUntil=Time.unscaledTime+4;}
        public void ShowGameSettings(){if(ArsenalPanel)ArsenalPanel.gameObject.SetActive(false);if(content)content.gameObject.SetActive(true);Navigate(HubPage.Settings);}
        public void ShowArsenal(){if(!ArsenalPanel)return;content.gameObject.SetActive(false);ArsenalPanel.SetParent(Design,false);ArsenalPanel.anchorMin=ArsenalPanel.anchorMax=ArsenalPanel.pivot=new Vector2(.5f,.5f);ArsenalPanel.anchoredPosition=new Vector2(0,-35);ArsenalPanel.localScale=Vector3.one*1.2f;ArsenalPanel.gameObject.SetActive(true);}
        public void Notify(string message){SetHint(message??"Listo.");}
        void SocialChanged()
        {
            if(Stage&&Catalog)Stage.ShowParty(Catalog,Social);
            if(content)Navigate(Page);
        }
        static void Clear(Transform parent){foreach(var child in parent.Cast<Transform>().ToArray())DestroyImmediate(child.gameObject);}
        public void Navigate(HubPage page)
        {
            CancelBinding();
            if(!content)return;
            if(page!=HubPage.Home&&Stage)Stage.StopEmote();emoteStatus=null;
            if(page==HubPage.Lobby)page=HubPage.Home;
            if(page!=HubPage.Home)homePanel="";
            Page=page;if(talentShowcase)DestroyImmediate(talentShowcase.gameObject);talentShowcase=null;Clear(content);RefreshBoundLabels();productView=null;
            const bool talentCompositionPilotEnabled=true; // Validated by live-character, alpha and contrast review.
            bool talentPilot=(talentCompositionPilotEnabled||(Application.isEditor&&Environment.GetCommandLineArgs().Contains("-riftTalentPilotReview")))&&InGame&&page==HubPage.Abilities&&abilityTab=="Talents";
            bool skillLayout=InGame&&page==HubPage.Abilities;bool spellLayout=skillLayout&&!talentPilot;
            bool journalLayout=InGame&&page==HubPage.Missions;bool inventoryLayout=InGame&&page==HubPage.Inventory;if(modernMoreMenu)modernMoreMenu.gameObject.SetActive(false);
            bool achievementLayout=page==HubPage.Progression;bool pauseLanding=InGame&&page==HubPage.Home;
            bool compact=InGame&&(page==HubPage.Inventory||page==HubPage.Character||page==HubPage.Missions||page==HubPage.Abilities||achievementLayout);
            if(fullGameplayHeader)fullGameplayHeader.gameObject.SetActive(!compact&&!pauseLanding);if(compactGameplayHeader)compactGameplayHeader.gameObject.SetActive(compact&&!inventoryLayout&&!achievementLayout&&!skillLayout&&!journalLayout);if(modernInventoryHeader){modernInventoryHeader.gameObject.SetActive(inventoryLayout);if(inventoryLayout)modernInventoryHeader.SetAsLastSibling();}
            if(InGame&&fallback){fallback.enabled=!compact&&!pauseLanding;fallback.raycastTarget=false;}
            int active=page==HubPage.Home?0:page==HubPage.Loadout?1:page==HubPage.Progression?2:page==HubPage.Marketplace||page==HubPage.Auction||page==HubPage.Exchange?3:page==HubPage.Settings?4:-1;
            for(int i=0;i<nav.Count;i++){var frame=nav[i].targetGraphic as RiftButtonGraphic;if(frame){frame.Active=i==active;frame.SetVerticesDirty();}}
            if(page==HubPage.Home)Home();
            else{
                var panel=skillLayout?W.Panel(content,"PagePanel",talentPilot?24:724,talentPilot?-70:-110,talentPilot?1800:1124,talentPilot?930:1016):achievementLayout?W.Panel(content,"PagePanel",0,-80,1848,900):inventoryLayout?W.Panel(content,"PagePanel",860,-40,988,945):talentPilot?W.Panel(content,"PagePanel",640,80,1208,720):journalLayout?W.Panel(content,"PagePanel",860,-40,988,945):compact?W.Panel(content,"PagePanel",1104,80,744,720):W.Panel(content,"PagePanel",0,0,1848,810);
                panel.GetComponent<Image>().raycastTarget=true;if(skillLayout){float layoutScale=talentPilot?.86f:988f/1124f;panel.localScale=Vector3.one*layoutScale;panel.anchoredPosition=new Vector2(talentPilot?(1848-1800*layoutScale)*.5f:860,talentPilot?70:40);}if(achievementLayout){RiftModernWidgets.Surface(panel,Color.clear);panel.GetComponent<Image>().raycastTarget=false;panel.GetComponent<Outline>().enabled=false;}if(inventoryLayout){RiftModernWidgets.Surface(panel,new Color(.006f,.008f,.013f,.8f));panel.gameObject.AddComponent<RiftInventoryFontScope>().Font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");panel.GetComponent<Image>().color=new Color(.006f,.008f,.013f,.8f);}if(skillLayout){var image=panel.GetComponent<Image>();image.color=Color.clear;image.raycastTarget=false;DestroyImmediate(image);foreach(Transform child in panel)child.gameObject.SetActive(false);var glass=panel.gameObject.AddComponent<ROM.UI.Gameplay.RiftMapSurface>();glass.Fill=new Color(.025f,.028f,.046f,.82f);glass.Border=new Color(.4f,.39f,.5f,.85f);glass.Radius=12;glass.raycastTarget=true;BuildSkillNavigation(panel,talentPilot);}
                if(journalLayout)BuildJournalNavigation(panel);if(!inventoryLayout&&!achievementLayout&&!skillLayout&&!journalLayout)W.Label(panel,talentPilot?"TALENTOS DE WARRIOR":PageTitle(page),28,14,inventoryLayout?920:talentPilot?1120:compact?660:1200,60,compact?28:32);
                if(!InGame&&!achievementLayout)W.Button(panel,"VOLVER",1560,18,260,50,()=>Navigate(HubPage.Home));else if(page==HubPage.Settings)W.Button(panel,"SALIR DE PARTIDA",1440,18,380,50,()=>Dialog("Volver al menú","Vas a salir de esta partida.",()=>LeaveRequested?.Invoke()));
                if(page==HubPage.Marketplace)W.Label(panel,$"AG {Services.Balances.AncientGold:N0} / RC {Services.Balances.RiftCrystals:N0}",1050,20,490,48,19,W.Gold);
                var body=skillLayout?W.Rect(panel,"Body",talentPilot?470:28,110,talentPilot?1302:1068,talentPilot?790:884):achievementLayout?W.Rect(panel,"Body",0,0,1848,900):W.Rect(panel,"Body",28,journalLayout?110:inventoryLayout?24:compact?80:95,journalLayout?932:inventoryLayout?932:talentPilot?1152:compact?688:1792,journalLayout?820:inventoryLayout?916:compact?610:690);
                switch(page){
                    case HubPage.Inventory:case HubPage.Character:case HubPage.Abilities:GameplayPage(body,page);break;
                    case HubPage.Missions:QuestJournal(body);break;
                    case HubPage.Loadout:Equipment(body);break;
                    case HubPage.Marketplace:case HubPage.Auction:Shop(body);break;
                    case HubPage.Exchange:Exchange(body);break;
                    case HubPage.Settings:Settings(body);break;
                    case HubPage.Social:Friends(body);break;
                    case HubPage.Progression:Progress(body);break;
                    default:W.Label(body,"EL RIFT\nExplorá el destino y defendé las fisuras dimensionales.",20,20,1500,180,28);break;
                }
            }
            if(Page==HubPage.Missions||Page==HubPage.Social||Page==HubPage.Settings){RiftPanelChrome.Apply(content);if(InGame){RiftPanelChrome.Apply(fullGameplayHeader);RiftPanelChrome.Apply(compactGameplayHeader);}}
            var first=InGame&&Page==HubPage.Abilities&&abilityTab=="Talents"?content.GetComponentsInChildren<Button>().FirstOrDefault(b=>b.name=="CENTRAR"):content.GetComponentInChildren<Button>();if(first&&EventSystem.current)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        static string PageTitle(HubPage page)=>page switch {HubPage.Inventory=>"INVENTARIO",HubPage.Character=>"PERSONAJE",HubPage.Abilities=>"HABILIDADES",HubPage.Loadout=>"EQUIPAMIENTO",HubPage.Missions=>"DIARIO DE MISIONES",HubPage.Progression=>"LOGROS",HubPage.Settings=>"AJUSTES",HubPage.Social=>"AMIGOS Y MENSAJES",HubPage.Exchange=>"INTERCAMBIO",_=>"TIENDA"};
        public void ToggleHomePanel(string panel){homePanel=Page==HubPage.Home&&homePanel==panel?"":panel;Navigate(HubPage.Home);}
        void CloseHomePanel(){homePanel="";Navigate(HubPage.Home);}
        RectTransform Drawer(string name,string title,float x,float width,float height){var r=W.Panel(content,name,x,0,width,height);W.Label(r,title,22,12,width-175,42,26,W.Gold);W.Button(r,"CERRAR",width-150,17,128,38,CloseHomePanel).GetComponentInChildren<Text>().fontSize=18;return r;}
        void Home()
        {
            if(InGame){ModernPause();return;}
            if(!InGame&&homePanel!="Party"&&homePanel!="Emotes"){ModernHome();return;}
            W.Label(content,Roster.Active.DisplayName,0,620,650,45,30);
            var rules=MultiFPS.Gameplay.RiftClassRules.Load();
            W.Label(content,CombatClassLabel(rules)+" · "+(CurrentCharacter()?.Name??"Personaje"),0,670,800,32,20,W.Muted);
            W.Button(content,"CLASE",0,744,170,52,()=>ToggleHomePanel("Class"));
            W.Button(content,"PERSONAJES",185,744,205,52,()=>ToggleHomePanel("Characters"));
            W.Button(content,"EMOTES",405,744,180,52,()=>ToggleHomePanel("Emotes"));
            W.Button(content,"PARTY · "+Social.Party.Count+"/4",600,744,220,52,()=>ToggleHomePanel("Party"));
            W.Label(content,"EL RIFT · EXPLORACIÓN COOPERATIVA",1398,645,450,38,21,W.Muted);
            W.Button(content,"JUGAR",1398,702,450,94,Deploy,true);
            if(homePanel=="Menu"){
                var menu=Drawer("MenuDrawer","MENÚ",0,450,650);
                string[] names={"EQUIPAMIENTO","MISIONES","LOGROS","TIENDA","AMIGOS","AJUSTES"};HubPage[] pages={HubPage.Loadout,HubPage.Missions,HubPage.Progression,HubPage.Marketplace,HubPage.Social,HubPage.Settings};
                for(int i=0;i<names.Length;i++){var page=pages[i];W.Button(menu,names[i],22,78+i*68,406,56,()=>Navigate(page));}
                W.Button(menu,"UNIRSE POR DIRECCIÓN",22,514,406,56,Connection);
                W.Label(menu,"Esc para volver",22,590,406,30,18,W.Muted);
            }
            else if(homePanel=="Class")DrawClassSelection();
            else if(homePanel=="Characters")DrawCharacters();
            else if(homePanel=="CreateCharacter")DrawCharacterCreation();
            else if(homePanel=="Emotes"&&Catalog&&Catalog.Emotes.Length>0){
                var emotes=Drawer("Emotes","EMOTES",0,450,355);
                int pages=Mathf.CeilToInt(Catalog.Emotes.Length/4f);emotePage%=pages;
                for(int i=0;i<4;i++){int index=emotePage*4+i;if(index>=Catalog.Emotes.Length)break;var emote=Catalog.Emotes[index];var button=W.Button(emotes,emote.Name,22+(i%2)*205,75+(i/2)*68,195,56,()=>{if(!Stage||!Stage.PlayEmote(emote))Notify("Esperá a que cargue el personaje.");});button.GetComponentInChildren<Text>().fontSize=18;}
                emoteStatus=W.Label(emotes,"Elegí un emote",22,218,406,42,18,W.Muted);
                W.Button(emotes,"DETENER",22,285,195,45,()=>{if(Stage)Stage.StopEmote();});W.Button(emotes,"MÁS "+(emotePage+1)+"/"+pages,227,285,195,45,()=>{emotePage=(emotePage+1)%pages;Navigate(HubPage.Home);});
            }
            else if(homePanel=="Party"){
                var party=Drawer("Party","PARTY · "+Social.Party.Count+"/4",1398,450,540);
                for(int i=0;i<4;i++){
                    if(i<Social.Party.Count){var member=Social.Party[i];var friend=Social.Find(member.ProfileId);bool self=member.ProfileId==Social.Self.Id;
                        W.Label(party,(self?"TÚ · ":"")+(friend?.Name??"Jugador"),24,80+i*82,330,34,23);
                        W.Label(party,(member.ProfileId==Social.LeaderId?"Líder · ":"")+(member.Ready?"Listo":"Preparando"),24,115+i*82,350,25,17,W.Muted);
                        if(!self&&Social.Self.Id==Social.LeaderId){string id=member.ProfileId;W.Button(party,"×",350,80+i*82,75,35,()=>Notify(Social.RemoveMember(id)));}
                    }else W.Button(party,"+ INVITAR",24,80+i*82,400,62,()=>Navigate(HubPage.Social));
                }
                bool ready=Social.Party.Any(p=>p.ProfileId==Social.Self.Id&&p.Ready);W.Button(party,ready?"LISTO ✓":"MARCAR COMO LISTO",24,442,400,60,()=>Notify(Social.SetReady(!ready)));
            }
        }
        string CombatClassLabel(MultiFPS.Gameplay.RiftClassRules rules)=>Roster.Active.Available?"Warrior":"CLASE NO DISPONIBLE";
        PreviewModel CurrentCharacter()
        {
            if(!Catalog)return null;var list=Catalog.Characters;return list.Length==0?null:list[Mathf.Clamp(Roster.Active.Skin,0,list.Length-1)];
        }
        public void Deploy()
        {
            if(!RequireWarrior())return;
            if(!Creator){Notify("Abrí MainMenu para iniciar una partida.");return;}
            ApplyActiveCharacter();
            if(Social.Self.Id!=Social.LeaderId){Notify("Solo el líder puede iniciar la partida.");return;}
            int map=Array.FindIndex(Creator.Maps,m=>m&&m.Scene.Contains("map_rom_horde"));if(map<0){Notify("El Rift no está configurado.");return;}
            Creator.MapselectionDropdown.value=map;Creator.MapselectionDropdown.onValueChanged.Invoke(map);
            int raid=Creator.GamemodeSelectionDropdown.options.FindIndex(o=>o.text=="Raid");if(raid>=0)Creator.GamemodeSelectionDropdown.value=raid;
            int count=Array.FindIndex(Creator.Maps[map].MaxPlayersPresets,p=>p==4);if(count>=0)Creator.PlayerNumberDropdown.value=count;
            Creator.BotsToggle.isOn=false;ApplyAppearance();
            Dialog("Entrar en El Rift",Social.Party.Count>1?"Se abrirá una partida real como host. Los perfiles de la party local son de prueba; otros equipos se unen por dirección.":"Se abrirá una partida cooperativa como host. Otros jugadores podrán unirse por dirección.",()=>{ROMLoadingScreen.Show("Entrando en El Rift","El Rift");Creator.HostGameButton.onClick.Invoke();});
        }
        void ApplyAppearance()
        {
            var character=CurrentCharacter();if(character==null)return;UserSettings.CharacterSkinID=character.SkinIndex;PlayerPrefs.SetInt("_playerPrefs_CharacterSkinID",character.SkinIndex);
            UserSettings.UserNickname=Roster.Active.DisplayName;PlayerPrefs.SetString("_playerPrefs_Username",Roster.Active.DisplayName);
        }
        int CurrentClassId=>InGame&&MultiFPS.UI.ClientFrontend.ClientPlayerInstance?MultiFPS.UI.ClientFrontend.ClientPlayerInstance.ClassId:PlayerPrefs.GetInt(MultiFPS.Gameplay.PlayerInstance.ClassPreference,0);
        void ChooseClass(int id){Notify("Solo Warrior está disponible. Elegí un personaje desde el menú principal.");}
        System.Collections.IEnumerator RefreshClassSelection(){yield return new WaitForSecondsRealtime(.3f);if(Page==HubPage.Loadout)Navigate(HubPage.Loadout);}
        public void ShowClasses(){if(!content)Build();if(ArsenalPanel)ArsenalPanel.gameObject.SetActive(false);content.gameObject.SetActive(true);Navigate(HubPage.Loadout);}
        void Equipment(Transform body)
        {
            var character=CurrentCharacter();var rules=MultiFPS.Gameplay.RiftClassRules.Load();
            if(Roster.Active.ClassKey=="warrior"){DrawWarriorEquipment(body);return;}
            if(rules&&rules.Classes.Length==4){
                var def=rules.Find(Mathf.Clamp(CurrentClassId,0,3));
                W.Label(body,"CLASE · "+def.Role,0,0,650,30,22,W.Gold);
                W.Dropdown(body,"Clase",0,40,650,rules.Classes.Select(c=>c.Name).ToArray(),CurrentClassId,ChooseClass);
                W.Label(body,def.Description,0,105,650,66,21);
                W.Label(body,"[F] "+def.FirstAbility+" · "+def.FirstCooldown+" s\n"+def.FirstDescription,0,180,650,68,19);
                W.Label(body,"[Z] "+def.SecondAbility+" · "+def.SecondCooldown+" s\n"+def.SecondDescription,0,252,650,68,19);
                W.Label(body,def.Health+" vida · Todas las armas convencionales\n"+def.Passive,0,328,650,58,18,W.Muted);
            }
            W.Label(body,"SKIN: "+(character?.Name??"Sin personaje"),0,405,650,35,24,W.Gold);
            W.Button(body,"ANTERIOR",0,449,250,45,()=>ChangeCharacter(-1));W.Button(body,"SIGUIENTE",270,449,250,45,()=>ChangeCharacter(1));
            int row=0;foreach(var item in Services.Inventory.Items){var captured=item;W.Button(body,item.Name,0,510+row++*44,650,40,()=>{selectedModel=captured.CosmeticReference;Navigate(HubPage.Loadout);});}
            string key=string.IsNullOrEmpty(selectedModel)?character?.Key:selectedModel;
            ShowModel(body,Catalog?Catalog.Find(key):null,750,0,1020,550);
            W.Label(body,InGame?"Clases: solo se cambian en zonas seguras.\nLas habilidades usan F y Z; las armas conservan sus controles.":"Clase y skin son independientes.\nTodas las clases pueden usar armas convencionales y jugar solas.",770,575,980,90,21,W.Muted);
        }
        void ChangeCharacter(int step)
        {
            if(!Catalog||Catalog.Characters.Length==0)return;selectedModel="";int count=Catalog.Characters.Length;int skin=(Roster.Active.Skin+step+count)%count;if(!MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Playable(skin)){Notify("Este aspecto aún no está disponible para jugar.");return;}Roster.SetSkin(skin);Social.SetAppearance(skin);ApplyAppearance();
        }
        void ShowModel(Transform parent,PreviewModel model,float x,float y,float w,float h)
        {
            var panel=W.Panel(parent,"ModelPreview",x,y,w,h);
            if(model==null||!model.Prefab){W.Label(panel,"Modelo no disponible",30,30,w-60,80,25,W.Muted);return;}
            if(Application.isPlaying){productView=RiftModelView.Create(panel,2,2,w-4,h-4,Catalog,false);productView.Show(model);}
            W.Label(panel,model.Name,22,16,w-44,42,25);
            W.Button(panel,"GIRAR",w-160,h-62,140,44,()=>{if(productView)productView.Turn(45);});
        }
        void Shop(Transform body)
        {
            string[] tabs={"Marketplace","Subastas","Mis anuncios","Intercambio"};
            for(int i=0;i<tabs.Length;i++){string tab=tabs[i];W.Button(body,tab.ToUpperInvariant(),i*285,0,270,50,()=>{shopTab=tab;selectedModel="";if(tab=="Intercambio")Navigate(HubPage.Exchange);else Navigate(HubPage.Marketplace);},shopTab==tab);}
            W.Label(body,shopTab=="Marketplace"?"Cosméticos oficiales · Precio en AG · Compras de prueba":"Anuncios entre jugadores · Precio en RC · Operaciones de prueba",0,65,1700,36,19,W.Muted);
            if(shopTab=="Mis anuncios"){Sell(body);return;}
            var products=Services.Marketplace.Products;
            int y=0;
            var productList=W.ScrollArea(body,"ProductList",0,125,660,545,products.Count*80);
            if(shopTab=="Marketplace")foreach(var p in products){var product=p;W.Button(productList,p.Name+"  ·  "+p.GoldPrice+" AG",0,y,650,65,()=>{selectedModel=product.ModelKey;Navigate(HubPage.Marketplace);},selectedModel==p.ModelKey);y+=80;}
            else{
                var scrollRoot=(RectTransform)productList.parent.parent;scrollRoot.anchoredPosition=new Vector2(0,-185);scrollRoot.sizeDelta=new Vector2(660,400);((RectTransform)productList.parent).sizeDelta=new Vector2(660,400);productList.sizeDelta=new Vector2(660,Mathf.Max(400,Services.Auction.Listings.Count*80));
                W.Input(body,"Buscar…",0,115,650,v=>query=v);
                y=0;foreach(var listing in Services.Auction.Listings.Where(l=>l.Item.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0)){
                    var captured=listing;W.Button(productList,listing.Item+" · "+listing.Price+" RC",0,y,650,65,()=>{selectedModel=captured.ModelKey;Navigate(HubPage.Marketplace);});y+=80;
                }
                W.Button(body,"ACTUALIZAR BÚSQUEDA",0,625,650,50,()=>Navigate(HubPage.Marketplace));
            }
            var first=shopTab=="Marketplace"?products.FirstOrDefault()?.ModelKey:Services.Auction.Listings.FirstOrDefault()?.ModelKey;
            if(string.IsNullOrEmpty(selectedModel))selectedModel=first;
            var model=Catalog?Catalog.Find(selectedModel):null;ShowModel(body,model,720,120,1050,410);
            if(shopTab=="Marketplace"){
                var p=products.FirstOrDefault(v=>v.ModelKey==selectedModel);if(p==null)return;selectedOwned=p.Owned;
                W.Label(body,p.Description+"\n"+p.GoldPrice+" AG",740,546,1000,70,22);
                var buy=W.Button(body,p.Owned?"ADQUIRIDO EN LA DEMO":"COMPRAR COSMÉTICO",1050,625,690,60,()=>Dialog(p.Name,"Confirmar compra de prueba por "+p.GoldPrice+" AG. No se modifica el saldo real.",()=>{var result=Services.Marketplace.PreviewPurchase(p.Id);Navigate(HubPage.Marketplace);Notify(result.Message);}),true);buy.interactable=!p.Owned;
            }else{
                var l=Services.Auction.Listings.FirstOrDefault(v=>v.ModelKey==selectedModel);if(l==null)return;
                W.Label(body,"Vendedor: "+l.Seller+"\nPrecio fijo: "+l.Price+" RC · "+l.Rarity,740,545,1000,70,22);
                W.Button(body,"COMPRAR ANUNCIO · DEMO",1050,625,690,60,()=>Dialog(l.Item,"Simular la compra de este anuncio por "+l.Price+" RC.",()=>Notify(Services.Auction.PreviewTransaction(l.Id).Message)),true);
            }
        }
        void Sell(Transform body)
        {
            W.Label(body,"Elegí un objeto transferible para revisar el anuncio.",0,125,1300,50,25);
            int row=0;foreach(var item in Services.Inventory.Items.Where(i=>i.Tradable)){
                var captured=item;W.Button(body,item.Name,0,205+row++*80,650,65,()=>{selectedModel=captured.CosmeticReference;Navigate(HubPage.Marketplace);});
            }
            ShowModel(body,Catalog?Catalog.Find(selectedModel):null,720,125,1050,410);
            var eligible=Services.Inventory.Items.FirstOrDefault(i=>i.CosmeticReference==selectedModel&&i.Tradable);int price=100;
            var input=W.Input(body,"Precio en RC",740,555,380,v=>int.TryParse(v,out price));input.contentType=InputField.ContentType.IntegerNumber;
            W.Button(body,"REVISAR ANUNCIO",1150,555,590,60,()=>{if(eligible==null){Notify("Seleccioná un objeto.");return;}Dialog("Anuncio de prueba",eligible.Name+" · "+price+" RC · 24 h\nTarifas pendientes de definición. Sin publicación real.",()=>Notify(Services.Auction.PreviewListing(eligible.Id,price,24).Message));});
        }
        void Exchange(Transform body){W.Label(body,"El intercambio de Ancient Gold entre jugadores estará disponible cuando se definan sus reglas.\n\nNo hay ofertas activas ni compra directa oficial de AG.",20,70,1600,200,27,W.Muted);W.Button(body,"VOLVER A LA TIENDA",20,340,550,60,()=>{shopTab="Marketplace";Navigate(HubPage.Marketplace);});}
        void Progress(Transform body){AchievementJournal(body);}
        void Settings(Transform body)
        {
            string[] tabs=InGame?new[]{"Video","Sonido","Controles","Interfaz"}:new[]{"Video","Sonido","Controles","Perfil"};for(int i=0;i<tabs.Length;i++){string tab=tabs[i];W.Button(body,tab.ToUpperInvariant(),i*350,0,330,50,()=>{settingsTab=tab;Navigate(HubPage.Settings);},settingsTab==tab);}
            var video=Application.isPlaying?RiftVideoSettings.Ensure():null;if(draft==null)draft=video?video.Current.Copy():new VideoPreferences();
            var panel=W.Rect(body,"Options",0,85,1750,590);
            if(settingsTab=="Video")Video(panel,video);
            else if(settingsTab=="Sonido"){W.Label(panel,"Volumen general",0,10,700,40,25);W.Slider(panel,"MasterVolume",0,70,1100,0,1,draft.Volume,v=>draft.Volume=v);SaveSettings(panel,video);}
            else if(settingsTab=="Controles"){ControlsSettings(panel,video);
            }else if(settingsTab=="Interfaz"&&InGame)InterfaceOptions(panel);else Profile(panel);
        }
        void Choice(Transform parent,string label,string value,float x,float y,float width,Action change)
        {W.Label(parent,label,x,y,width,30,20,W.Muted);W.Button(parent,value,x,y+37,width,52,change);}
        void Video(Transform p,RiftVideoSettings video)
        {
            string[] windows={"Pantalla completa","Sin bordes","Ventana"};string[] quality=QualitySettings.names;
            void Select(string name,string[] values,int index,float x,float y,Action<int> change){W.Label(p,name,x,y,525,30,20,W.Muted);W.Dropdown(p,name,x,y+37,525,values,index,change);}
            Select("Calidad gráfica",quality,draft.Quality,0,0,v=>draft.Quality=v);
            Select("Modo de pantalla",windows,draft.WindowMode,600,0,v=>draft.WindowMode=v);
            var resolutions=Screen.resolutions.Select(r=>new Vector2Int(r.width,r.height)).Distinct().ToList();
            var current=new Vector2Int(draft.Width,draft.Height);if(!resolutions.Contains(current))resolutions.Add(current);
            Select("Resolución",resolutions.Select(r=>r.x+" × "+r.y).ToArray(),resolutions.IndexOf(current),1200,0,v=>{draft.Width=resolutions[v].x;draft.Height=resolutions[v].y;});
            string[] switches={"Desactivado","Activado"};
            Select("VSync",switches,draft.VSync?1:0,0,115,v=>draft.VSync=v==1);
            int[] limits={30,60,90,120,144,240};Select("Límite de FPS",limits.Select(v=>v.ToString()).ToArray(),Array.IndexOf(limits,draft.FrameLimit),600,115,v=>draft.FrameLimit=limits[v]);
            Select("Antialiasing",new[]{"Ninguno","FXAA","SMAA"},draft.AA,1200,115,v=>draft.AA=v);
            Select("Bloom",switches,draft.Bloom?1:0,0,230,v=>draft.Bloom=v==1);
            Select("Desenfoque de movimiento",switches,draft.MotionBlur?1:0,600,230,v=>draft.MotionBlur=v==1);
            float[] scales={.75f,1f,1.25f};Select("Escala de render",scales.Select(v=>Mathf.RoundToInt(v*100)+" %").ToArray(),Array.IndexOf(scales,draft.RenderScale),1200,230,v=>draft.RenderScale=scales[v]);
            W.Label(p,"Brillo",0,355,500,30,20,W.Muted);W.Slider(p,"Brightness",0,395,1125,-1,1,draft.Exposure,v=>draft.Exposure=v);
            SaveSettings(p,video);
        }
        void SaveSettings(Transform p,RiftVideoSettings video)
        {
            W.Button(p,"APLICAR CAMBIOS",1140,510,580,64,()=>{
                if(!video)return;video.Preview(draft);Dialog("¿Conservar estos ajustes?","Tenés 15 segundos para confirmar. Si no confirmás, se restauran automáticamente.",()=>{videoDialog=false;video.Confirm();draft=video.Current.Copy();Notify("Ajustes guardados.");},()=>{videoDialog=false;video.Revert();draft=video.Current.Copy();Navigate(HubPage.Settings);});
                videoDialog=true;videoTimer=W.Label(modal.Find("Dialog"),"",32,425,870,45,25,W.Gold);
            },true);
            W.Button(p,"RESTABLECER",0,510,450,64,()=>{draft=new VideoPreferences{Width=Screen.width,Height=Screen.height};Navigate(HubPage.Settings);});
        }
        void Profile(Transform p)
        {
            W.Label(p,"Nombre del personaje: "+Roster.Active.DisplayName,0,0,1200,65,25);
            W.Label(p,"PRUEBAS LOCALES\nCambiá de perfil para aceptar invitaciones y leer DM enviados en esta PC.\nEstos perfiles no representan usuarios conectados a un servidor.",0,190,1600,150,24,W.Muted);
            W.Button(p,"PERFIL LOCAL: "+Social.Self.Name,0,395,950,64,()=>{if(!(Social is LocalSocialService local)){Notify("Esta función es solo para pruebas locales.");return;}var profiles=local.Profiles.ToArray();if(profiles.Length<2){Notify("Agregá un contacto desde Amigos para probar otro perfil.");return;}profileTestIndex=(Array.FindIndex(profiles,v=>v.Id==Social.Self.Id)+1)%profiles.Length;local.SwitchLocalProfile(profiles[profileTestIndex].Id);ApplyAppearance();});
        }
        void Friends(Transform body){ModernFriends(body);}
        void Connection()
        {
            if(!RequireWarrior())return;
            if(!Connector){Notify("Conexión disponible desde el hub principal.");return;}
            Dialog("Unirse a El Rift","Ingresá la dirección y el puerto del host.",()=>{
                ApplyAppearance();ROMLoadingScreen.NextDestination="El Rift";ROMLoadingScreen.Show("Conectando","El Rift");Connector.ConnectBtn.onClick.Invoke();
            });
            var panel=modal.Find("Dialog");var connectionRect=(RectTransform)panel;connectionRect.sizeDelta=new Vector2(760,510);connectionRect.anchoredPosition=new Vector2(580,-285);foreach(var b in panel.GetComponentsInChildren<Button>()){var r=(RectTransform)b.transform;r.anchoredPosition=new Vector2(r.anchoredPosition.x,-420);}var messageText=panel.GetComponentsInChildren<Text>().FirstOrDefault(t=>t.rectTransform.anchoredPosition.y==-96);if(messageText)messageText.rectTransform.sizeDelta=new Vector2(696,72);var address=W.Input(panel,"Dirección del host",32,196,696,v=>Connector.Address.text=v);address.text=Connector.Address.text;
            var port=W.Input(panel,"Puerto",32,270,696,v=>Connector.Port.text=v);port.text=Connector.Port.text;port.contentType=InputField.ContentType.IntegerNumber;RiftPanelChrome.Apply(panel);
        }
        public void ReturnFromLegacy(){if(LegacyMenu)LegacyMenu.SetActive(false);gameObject.SetActive(true);Navigate(HubPage.Home);}
        Action modalCancel;
        public void Dialog(string title,string message,Action confirm,Action cancel=null)
        {
            modalCancel=cancel;if(modal)DestroyImmediate(modal.gameObject);modal=W.Rect(Design,"Modal",0,0,1920,1080);
            var shade=W.Image(modal,"Blocker",0,0,1920,1080,new Color(0,0,0,.8f));shade.raycastTarget=true;
            var panel=W.Panel(modal,"Dialog",580,355,760,370);W.Label(panel,title,32,26,696,54,32);W.Label(panel,message,32,96,696,154,22);RiftPanelChrome.Apply(panel);
            RiftSkillStyle.Button(panel,confirm==null?"CERRAR [Esc]":"CANCELAR [Esc]",32,280,328,58,()=>{if(!InGame){StartCoroutine(DeferMainMenuAction(DismissModal));return;}DismissModal();});
            if(confirm!=null)RiftSkillStyle.Button(panel,"CONFIRMAR",384,280,344,58,()=>{if(!InGame){ConfirmMainMenuDialog(confirm);return;}DestroyImmediate(modal.gameObject);modalCancel=null;confirm();},true);
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
        }
        void OnDestroy(){RiftControls.Changed-=RefreshBoundLabels;ROM.Progression.RiftJournalView.Changed-=JournalChanged;CancelBinding();if(Social!=null)Social.Changed-=SocialChanged;}
    }
}

