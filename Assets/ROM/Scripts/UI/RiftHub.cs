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
    public enum HubPage { Home, Missions, Loadout, Lobby, Progression, Auction, Exchange, Marketplace, Settings, Social, Codex, Results }
    public sealed class RiftHub : MonoBehaviour
    {
        public Sprite Background,Logo;
        public ROMUITheme Theme;
        public Font InterfaceFont;
        public FrontEndCatalog Catalog;
        public GameObject LegacyMenu;
        public RoomCreator Creator;
        public RoomConnector Connector;
        public RectTransform Design;
        public HubPage Page {get;private set;}
        public SprintServices Services {get;private set;}
        public ISocialService Social {get;private set;}
        public RiftModelView Stage {get;private set;}
        RectTransform content,modal;
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
        public void Initialize()
        {
            W.Body=InterfaceFont?InterfaceFont:Theme?Theme.Body:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            // The condensed display font is restricted to headings. Body and navigation
            // use the existing Inter font to keep the new UI readable.
            W.Title=W.Body;
            if(Services==null)Services=SprintServices.CreateMock(Catalog);
            if(Social==null){Social=new LocalSocialService();Social.Changed+=SocialChanged;}
        }
        public void UseLocalService(LocalSocialService service)
        {
            if(Social!=null)Social.Changed-=SocialChanged;Social=service;Social.Changed+=SocialChanged;
        }
        public void UseSocialService(ISocialService service){if(Social!=null)Social.Changed-=SocialChanged;Social=service??throw new ArgumentNullException(nameof(service));Social.Changed+=SocialChanged;}
        public void UseServices(SprintServices services){Services=services??throw new ArgumentNullException(nameof(services));}
        void Start(){Initialize();RiftVideoSettings.Ensure();Build();}
        public void Build()
        {
            Initialize();if(Design)DestroyImmediate(Design.gameObject);nav.Clear();productView=null;
            Design=W.Rect(transform,"SafeAreaDesign",0,0,1920,1080);
            fallback=W.Image(Design,"LoadingFallback",0,0,1920,1080,Color.white,Background);
            if(Catalog&&Catalog.StagePrefab&&Application.isPlaying){
                try{Stage=RiftModelView.Create(Design,0,0,1920,1080,Catalog,true);Stage.ShowParty(Catalog,Social);fallback.enabled=!Stage.Ready;}
                catch(Exception e){Debug.LogError("Lobby stage failed: "+e.Message);if(Stage)Stage.gameObject.SetActive(false);fallback.enabled=true;}
            }
            W.Image(Design,"HeaderShade",0,0,1920,110,new Color(.015f,.018f,.027f,.96f));
            W.Image(Design,"HeaderLine",0,108,1920,2,W.Gold);
            W.Image(Design,"Brand",28,16,210,72,Color.white,Logo).preserveAspect=true;
            string[] labels={"JUGAR","EQUIPAMIENTO","PROGRESO","TIENDA","AJUSTES"};
            HubPage[] pages={HubPage.Home,HubPage.Loadout,HubPage.Progression,HubPage.Marketplace,HubPage.Settings};
            for(int i=0;i<labels.Length;i++){
                var page=pages[i];nav.Add(W.Button(Design,labels[i],260+i*235,24,220,58,()=>Navigate(page)));
            }
            W.Button(Design,"AMIGOS",1500,24,180,58,()=>Navigate(HubPage.Social));
            W.Label(Design,$"AG {Services.Balances.AncientGold:N0}  /  RC {Services.Balances.RiftCrystals:N0}",1500,112,390,38,20,W.Gold);
            W.Image(Design,"FooterShade",0,1010,1920,70,new Color(.015f,.018f,.027f,.95f));
            W.Label(Design,"ROM: RIFTBREAKERS   /   EL RIFT",35,1024,680,32,17,W.Muted);
            W.Label(Design,"LOCAL · Sin conexión a servicios de amigos o comercio",1000,1024,880,32,16,W.Muted);
            content=W.Rect(Design,"Screen",36,168,1848,810);
            toast=W.Label(Design,"",460,970,1200,36,20,new Color(1,.78f,.48f));
            Navigate(HubPage.Home);Fit();
        }
        void Update()
        {
            Fit();if(fallback&&Stage){fallback.enabled=!Stage.Ready;Stage.SetDisplay(Stage.Ready);}
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
        }
        public void SetHint(string message){if(toast)toast.text=message;}
        public void Notify(string message){SetHint(message??"Listo.");}
        void SocialChanged()
        {
            if(Stage&&Catalog)Stage.ShowParty(Catalog,Social);
            if(content)Navigate(Page);
        }
        static void Clear(Transform parent){foreach(var child in parent.Cast<Transform>().ToArray())DestroyImmediate(child.gameObject);}
        public void Navigate(HubPage page)
        {
            if(!content)return;
            if(page==HubPage.Missions||page==HubPage.Lobby)page=HubPage.Home;
            Page=page;Clear(content);productView=null;
            int active=page==HubPage.Home?0:page==HubPage.Loadout?1:page==HubPage.Progression?2:page==HubPage.Marketplace||page==HubPage.Auction||page==HubPage.Exchange?3:page==HubPage.Settings?4:-1;
            for(int i=0;i<nav.Count;i++)nav[i].targetGraphic.color=i==active?new Color(.3f,.035f,.065f,.98f):W.Ink;
            if(page==HubPage.Home)Home();
            else{
                var panel=W.Panel(content,"PagePanel",0,0,1848,810);
                W.Label(panel,PageTitle(page),28,14,1700,60,32);
                var body=W.Rect(panel,"Body",28,95,1792,690);
                switch(page){
                    case HubPage.Loadout:Equipment(body);break;
                    case HubPage.Marketplace:case HubPage.Auction:Shop(body);break;
                    case HubPage.Exchange:Exchange(body);break;
                    case HubPage.Settings:Settings(body);break;
                    case HubPage.Social:Friends(body);break;
                    case HubPage.Progression:Progress(body);break;
                    default:W.Label(body,"EL RIFT\nExplorá el destino y defendé las fisuras dimensionales.",20,20,1500,180,28);break;
                }
            }
            var first=content.GetComponentInChildren<Button>();if(first&&EventSystem.current)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        static string PageTitle(HubPage page)=>page switch {HubPage.Loadout=>"EQUIPAMIENTO",HubPage.Progression=>"PROGRESO",HubPage.Settings=>"AJUSTES",HubPage.Social=>"AMIGOS Y MENSAJES",HubPage.Exchange=>"INTERCAMBIO",_=>"TIENDA"};
        void Home()
        {
            var destination=W.Panel(content,"Destination",0,0,425,210);
            W.Label(destination,"EL RIFT",22,12,380,65,46);
            W.Label(destination,"Horda cooperativa · 1–4 jugadores\nUn destino. Todas sus islas conectadas.",22,86,380,100,22,W.Muted);
            var profile=W.Panel(content,"Profile",0,625,425,185);
            W.Label(profile,Social.Self.Name,22,10,380,44,28);
            var appearance=CurrentCharacter();W.Label(profile,appearance?.Name??"Cargando personaje…",22,59,380,33,20,W.Muted);
            W.Button(profile,"PERSONALIZAR",22,112,380,50,()=>Navigate(HubPage.Loadout));
            var party=W.Panel(content,"Party",1398,38,450,520);
            W.Label(party,"TU PARTY  /  "+Social.Party.Count+" DE 4",24,17,400,42,26);
            for(int i=0;i<4;i++){
                if(i<Social.Party.Count){var member=Social.Party[i];var friend=Social.Find(member.ProfileId);bool self=member.ProfileId==Social.Self.Id;
                    W.Label(party,(self?"TÚ · ":"")+(friend?.Name??"Jugador"),24,83+i*82,330,34,23);
                    W.Label(party,(member.ProfileId==Social.LeaderId?"Líder · ":"")+(member.Ready?"Listo":"Preparando"),24,120+i*82,350,25,17,member.Ready?new Color(.5f,.9f,.68f):W.Muted);
                    if(!self&&Social.Self.Id==Social.LeaderId){string id=member.ProfileId;W.Button(party,"×",350,83+i*82,75,35,()=>Notify(Social.RemoveMember(id)));}
                }else W.Button(party,"+ INVITAR",24,87+i*82,400,62,()=>Navigate(HubPage.Social));
            }
            bool ready=Social.Party.Any(p=>p.ProfileId==Social.Self.Id&&p.Ready);
            W.Button(party,ready?"LISTO ✓":"MARCAR COMO LISTO",24,433,400,60,()=>Notify(Social.SetReady(!ready)));
            W.Button(content,"JUGAR EL RIFT",1398,620,450,88,Deploy,true);
            W.Button(content,"UNIRSE POR DIRECCIÓN",1398,730,450,60,Connection);
            if(Social.Party.Count>1)W.Label(content,"Party local de prueba · los perfiles no se conectan automáticamente a la partida.",465,745,880,56,17,W.Muted);
        }
        PreviewModel CurrentCharacter()
        {
            if(!Catalog)return null;var list=Catalog.Characters;return list.Length==0?null:list[Mathf.Clamp(Social.Self.Skin,0,list.Length-1)];
        }
        public void Deploy()
        {
            if(!Creator){Notify("Abrí la escena hub para iniciar una partida.");return;}
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
            UserSettings.UserNickname=Social.Self.Name;PlayerPrefs.SetString("_playerPrefs_Username",Social.Self.Name);
        }
        void Equipment(Transform body)
        {
            var character=CurrentCharacter();W.Label(body,"PERSONAJE",0,0,600,36,22,W.Gold);
            W.Label(body,character?.Name??"Sin personaje",0,45,600,50,30);
            W.Button(body,"ANTERIOR",0,112,250,50,()=>ChangeCharacter(-1));W.Button(body,"SIGUIENTE",270,112,250,50,()=>ChangeCharacter(1));
            W.Label(body,"Tu apariencia se aplica en la partida.",0,174,620,28,18,W.Muted);
            W.Button(body,"VER PERSONAJE",0,200,650,45,()=>{selectedModel="";Navigate(HubPage.Loadout);});
            int row=0;foreach(var item in Services.Inventory.Items){var entry=Catalog?Catalog.Find(item.CosmeticReference):null;var captured=item;
                W.Button(body,item.Name,0,250+row++*72,650,58,()=>{selectedModel=captured.CosmeticReference;Navigate(HubPage.Loadout);});
            }
            string key=string.IsNullOrEmpty(selectedModel)?character?.Key:selectedModel;
            ShowModel(body,Catalog?Catalog.Find(key):null,750,0,1020,550);
            var record=Services.Inventory.Items.FirstOrDefault(i=>i.CosmeticReference==key);
            W.Label(body,record==null?"Personaje IDS · Arrastrá para girar. Rueda para acercar.":record.Name+"\n"+record.Stats+"\nVista del modelo · El equipamiento de combate sigue a cargo del kit.",770,575,980,95,21,W.Muted);
        }
        void ChangeCharacter(int step)
        {
            if(!Catalog||Catalog.Characters.Length==0)return;selectedModel="";int count=Catalog.Characters.Length;Social.SetAppearance((Social.Self.Skin+step+count)%count);ApplyAppearance();
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
        void Progress(Transform body)
        {
            W.Label(body,"NIVEL "+Services.Profile.Level,0,15,900,70,44);W.Label(body,"5.600 / 10.000 XP · Datos de prueba",0,100,1300,45,24,W.Muted);
            W.Image(body,"XPTrack",0,175,1720,12,W.Gold);W.Image(body,"XP",0,175,1720*Services.Profile.Experience,12,W.Red);
            W.Label(body,"LOGROS\nExplorador de fisuras · Desbloqueado\nDefensor de El Rift · En progreso\n\nRECOMPENSAS\n"+Services.Rewards.State+" · Pendientes de validación",0,230,1600,285,27);
            W.Button(body,"REVISAR RECOMPENSAS",0,560,620,60,()=>Notify(Services.Rewards.PreviewClaim().Message));
        }
        void Settings(Transform body)
        {
            string[] tabs={"Video","Sonido","Controles","Perfil"};for(int i=0;i<tabs.Length;i++){string tab=tabs[i];W.Button(body,tab.ToUpperInvariant(),i*350,0,330,50,()=>{settingsTab=tab;Navigate(HubPage.Settings);},settingsTab==tab);}
            var video=Application.isPlaying?RiftVideoSettings.Ensure():null;if(draft==null)draft=video?video.Current.Copy():new VideoPreferences();
            var panel=W.Rect(body,"Options",0,85,1750,590);
            if(settingsTab=="Video")Video(panel,video);
            else if(settingsTab=="Sonido"){W.Label(panel,"Volumen general",0,10,700,40,25);W.Slider(panel,"MasterVolume",0,70,1100,0,1,draft.Volume,v=>draft.Volume=v);SaveSettings(panel,video);}
            else if(settingsTab=="Controles"){
                W.Label(panel,"Sensibilidad del mouse",0,0,800,40,25);W.Slider(panel,"MouseSensitivity",0,55,1100,.1f,5,draft.Sensitivity,v=>draft.Sensitivity=v);
                W.Label(panel,"Campo de visión",0,155,800,40,25);W.Slider(panel,"Fov",0,210,1100,60,110,draft.Fov,v=>draft.Fov=v);SaveSettings(panel,video);
            }else Profile(panel);
        }
        void Choice(Transform parent,string label,string value,float x,float y,float width,Action change)
        {W.Label(parent,label,x,y,width,30,20,W.Muted);W.Button(parent,value,x,y+37,width,52,change);}
        void Video(Transform p,RiftVideoSettings video)
        {
            string[] windows={"Pantalla completa","Sin bordes","Ventana"};string[] quality=QualitySettings.names;
            Choice(p,"Calidad gráfica",quality[Mathf.Clamp(draft.Quality,0,quality.Length-1)],0,0,525,()=>{draft.Quality=(draft.Quality+1)%quality.Length;Navigate(HubPage.Settings);});
            Choice(p,"Modo de pantalla",windows[draft.WindowMode],600,0,525,()=>{draft.WindowMode=(draft.WindowMode+1)%3;Navigate(HubPage.Settings);});
            Choice(p,"Resolución",draft.Width+" × "+draft.Height,1200,0,525,()=>{
                var resolutions=Screen.resolutions.Select(r=>new Vector2Int(r.width,r.height)).Distinct().ToList();if(resolutions.Count==0)resolutions.Add(new Vector2Int(Screen.width,Screen.height));int index=resolutions.FindIndex(r=>r.x==draft.Width&&r.y==draft.Height);var next=resolutions[(index+1)%resolutions.Count];draft.Width=next.x;draft.Height=next.y;Navigate(HubPage.Settings);
            });
            Choice(p,"VSync",draft.VSync?"Activado":"Desactivado",0,115,525,()=>{draft.VSync=!draft.VSync;Navigate(HubPage.Settings);});
            Choice(p,"Límite de FPS",draft.FrameLimit.ToString(),600,115,525,()=>{int[] limits={30,60,90,120,144,240};draft.FrameLimit=limits[(Array.IndexOf(limits,draft.FrameLimit)+1)%limits.Length];Navigate(HubPage.Settings);});
            Choice(p,"Antialiasing",new[]{"Ninguno","FXAA","SMAA"}[draft.AA],1200,115,525,()=>{draft.AA=(draft.AA+1)%3;Navigate(HubPage.Settings);});
            Choice(p,"Bloom",draft.Bloom?"Activado":"Desactivado",0,230,525,()=>{draft.Bloom=!draft.Bloom;Navigate(HubPage.Settings);});
            Choice(p,"Desenfoque de movimiento",draft.MotionBlur?"Activado":"Desactivado",600,230,525,()=>{draft.MotionBlur=!draft.MotionBlur;Navigate(HubPage.Settings);});
            Choice(p,"Escala de render",Mathf.RoundToInt(draft.RenderScale*100)+" %",1200,230,525,()=>{draft.RenderScale=draft.RenderScale>=1.25f?.75f:draft.RenderScale+.25f;Navigate(HubPage.Settings);});
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
            W.Label(p,"Nombre del jugador",0,0,700,40,25);string name=Social.Self.Name;var input=W.Input(p,name,0,65,650,v=>name=v);input.text=name;
            W.Button(p,"GUARDAR NOMBRE",690,65,500,50,()=>{Social.Rename(name);ApplyAppearance();Notify("Nombre guardado.");});
            W.Label(p,"PRUEBAS LOCALES\nCambiá de perfil para aceptar invitaciones y leer DM enviados en esta PC.\nEstos perfiles no representan usuarios conectados a un servidor.",0,190,1600,150,24,W.Muted);
            W.Button(p,"PERFIL LOCAL: "+Social.Self.Name,0,395,950,64,()=>{if(!(Social is LocalSocialService local)){Notify("Esta función es solo para pruebas locales.");return;}var profiles=local.Profiles.ToArray();if(profiles.Length<2){Notify("Agregá un contacto desde Amigos para probar otro perfil.");return;}profileTestIndex=(Array.FindIndex(profiles,v=>v.Id==Social.Self.Id)+1)%profiles.Length;local.SwitchLocalProfile(profiles[profileTestIndex].Id);ApplyAppearance();});
        }
        void Friends(Transform body)
        {
            W.Label(body,"CONTACTOS",0,0,620,35,22,W.Gold);
            W.Input(body,"Nombre del contacto local",0,52,480,v=>friendDraft=v);
            W.Button(body,"AGREGAR",495,52,205,48,()=>{string value=friendDraft;friendDraft="";Notify(Social.AddFriend(value));});
            W.Button(body,"CHAT DE PARTY",0,125,700,55,()=>{peer="party";Navigate(HubPage.Social);});
            var contacts=W.ScrollArea(body,"Contacts",0,195,700,325,Social.Friends.Count*70);int y=0;
            foreach(var friend in Social.Friends){var f=friend;
                W.Button(contacts,f.Name,0,y,430,55,()=>{peer=f.Id;Navigate(HubPage.Social);});
                W.Button(contacts,"INVITAR",445,y,255,55,()=>Notify(Social.Invite(f.Id)));y+=70;
            }
            y=525;
            foreach(var invite in Social.Invitations.Take(2)){var i=invite;W.Button(body,"ACEPTAR: "+Social.Find(i.From)?.Name,0,y,700,55,()=>Notify(Social.AcceptInvite(i.From)));y+=65;}
            W.Label(body,"Los contactos y mensajes se guardan en esta PC.\nNo se envían por internet.",0,590,700,70,19,W.Muted);
            var console=W.Panel(body,"ChatConsole",750,0,1020,675);
            W.Label(console,peer=="party"?"PARTY":"DM / "+Social.Find(peer)?.Name,24,12,960,50,26);
            string transcript=string.Join("\n\n",Social.Conversation(peer).Select(m=>(Social.Find(m.From)?.Name??"Jugador")+": "+m.Text));
            var history=W.ScrollArea(console,"Messages",24,80,970,450,450,true);
            var log=W.Label(history,string.IsNullOrEmpty(transcript)?"Todavía no hay mensajes.":transcript,0,0,940,450,23);log.alignment=TextAnchor.UpperLeft;
            log.rectTransform.sizeDelta=new Vector2(940,Mathf.Max(450,log.preferredHeight+24));history.sizeDelta=log.rectTransform.sizeDelta;
            var text=W.Input(console,"Escribí un mensaje…",24,560,710,v=>chatDraft=v);text.characterLimit=500;text.text=chatDraft;
            W.Button(console,"ENVIAR",760,560,235,48,()=>{var message=chatDraft;chatDraft="";Notify(Social.Send(peer,message));});
            W.Label(console,"Perfil actual: "+Social.Self.Name+" · Cambiar desde Ajustes > Perfil",24,620,970,30,17,W.Muted);
        }
        void Connection()
        {
            if(!Connector){Notify("Conexión disponible desde el hub principal.");return;}
            Dialog("Unirse a El Rift","Ingresá la dirección y el puerto del host.",()=>{
                ApplyAppearance();ROMLoadingScreen.NextDestination="El Rift";ROMLoadingScreen.Show("Conectando","El Rift");Connector.ConnectBtn.onClick.Invoke();
            });
            var panel=modal.Find("Dialog");var address=W.Input(panel,"Dirección del host",32,250,820,v=>Connector.Address.text=v);address.text=Connector.Address.text;
            var port=W.Input(panel,"Puerto",32,320,820,v=>Connector.Port.text=v);port.text=Connector.Port.text;port.contentType=InputField.ContentType.IntegerNumber;
        }
        public void ReturnFromLegacy(){if(LegacyMenu)LegacyMenu.SetActive(false);gameObject.SetActive(true);Navigate(HubPage.Home);}
        public void Dialog(string title,string message,Action confirm,Action cancel=null)
        {
            if(modal)DestroyImmediate(modal.gameObject);modal=W.Rect(Design,"Modal",0,0,1920,1080);
            var shade=W.Image(modal,"Blocker",0,0,1920,1080,new Color(0,0,0,.8f));shade.raycastTarget=true;
            var panel=W.Panel(modal,"Dialog",490,240,940,600);W.Label(panel,title,32,24,876,70,32);W.Label(panel,message,32,115,876,310,24);
            W.Button(panel,confirm==null?"CERRAR":"CANCELAR",32,500,410,64,()=>{DestroyImmediate(modal.gameObject);cancel?.Invoke();});
            if(confirm!=null)W.Button(panel,"CONFIRMAR",490,500,418,64,()=>{DestroyImmediate(modal.gameObject);confirm();},true);
            if(EventSystem.current)EventSystem.current.SetSelectedGameObject(panel.GetComponentInChildren<Button>().gameObject);
        }
        void OnDestroy(){if(Social!=null)Social.Changed-=SocialChanged;}
    }
}
