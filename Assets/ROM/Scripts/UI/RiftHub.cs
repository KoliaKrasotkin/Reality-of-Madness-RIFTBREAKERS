using System;
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
        public Sprite Background, Logo;
        public ROMUITheme Theme;
        public GameObject LegacyMenu;
        public RoomCreator Creator;
        public RectTransform Design;
        public HubPage Page { get; private set; }
        public SprintServices Services { get; private set; }
        RectTransform content,modal;
        Text balances;
        Text hint;
        MissionRecord selected;
        string query="",category="Todas",rarity="Todas";
        bool descending;
        string auctionTab="Explorar";
        int maxPrice=10000;
        int draftPrice=100,draftHours=24;
        void Awake() { Initialize(); }
        public void Initialize()
        {
            if(Services!=null) return;
            Services=SprintServices.CreateMock(); selected=Services.Missions.Missions[0];
            W.Body=Theme?Theme.Body:Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            W.Title=Theme?Theme.Title:W.Body;
        }
        void Start() { Build(); }
        public void Build()
        {
            Initialize();
            if(Design) DestroyImmediate(Design.gameObject);
            Design=W.Rect(transform,"SafeAreaDesign",0,0,1920,1080);
            W.Image(Design,"CinematicBackground",0,0,1920,1080,Color.white,Background);
            W.Image(Design,"Atmosphere",0,0,1920,1080,new Color(.015f,.01f,.025f,.13f));
            W.Label(Design,Services.Profile.Name,142,23,330,36,28,null,true);
            W.Label(Design,"Nv. 42  ·  Caminante de grietas",142,66,340,26,16,W.Muted);
            W.Image(Design,"Rank",47,28,72,72,Color.white,Theme?Theme.Emblem:null);
            W.Image(Design,"Experience",142,62,270,4,new Color(.3f,.25f,.26f));
            W.Image(Design,"ExperienceProgress",142,62,270*Services.Profile.Experience,4,W.Red);
            balances=W.Label(Design,$"AG  {Services.Balances.AncientGold:N0}       RC  {Services.Balances.RiftCrystals:N0}",1230,28,550,44,24,W.White);
            W.Label(Design,"PROTOTIPO · PERFIL Y ECONOMÍA DE PRUEBA",1230,73,620,24,14,W.Muted);
            var logo=W.Image(Design,"ROMLogo",565,22,720,250,Color.white,Logo);logo.preserveAspect=true;
            string[] nav={"JUGAR","EQUIPAMIENTO","PROGRESO","SUBASTAS","MARKETPLACE","CÓDICE","AJUSTES"};
            HubPage[] pages={HubPage.Missions,HubPage.Loadout,HubPage.Progression,HubPage.Auction,HubPage.Marketplace,HubPage.Codex,HubPage.Settings};
            string[] hints={"Elegí una misión y prepará tu despliegue.","Inspeccioná armas, equipo y consumibles.","Consultá experiencia, logros y recompensas.","Explorá anuncios de otros jugadores.","Descubrí cosméticos de la colección oficial.","Conocé la historia de las fisuras.","Ajustá el sonido y los controles."};
            for(int i=0;i<nav.Length;i++){var page=pages[i];var button=W.Button(Design,nav[i],42,224+i*91,365,76,()=>Navigate(page),i==0);button.gameObject.AddComponent<RiftTooltip>().Message=hints[i];}
            hint=W.Label(Design,"",470,252,930,28,16,W.Muted);
            W.Button(Design,"ESCUADRA",42,1000,230,52,()=>Navigate(HubPage.Social));
            W.Button(Design,"TEMPORADA 01",292,1000,255,52,()=>Dialog("La era de las fisuras","Información de temporada de ejemplo. Calendario y recompensas por definir.",null));
            W.Button(Design,"CONEXIÓN / PERFIL",565,1000,290,52,OpenLegacy);
            W.Button(Design,"INICIO",1620,1000,250,52,()=>Navigate(HubPage.Home));
            content=W.Rect(Design,"Screen",445,285,1420,680);
            Navigate(HubPage.Home); Fit();
        }
        void Update(){Fit();}
        public void SetHint(string message){if(hint)hint.text=message;}
        void Fit()
        {
            if(!Design) return;
            var parent=(RectTransform)transform;var safe=Screen.safeArea;
            if(Screen.width<=0||Screen.height<=0)return;
            float scale=Mathf.Min(parent.rect.width*(safe.width/Screen.width)/1920f,parent.rect.height*(safe.height/Screen.height)/1080f);
            Design.anchorMin=Design.anchorMax=Design.pivot=new Vector2(.5f,.5f);Design.localScale=Vector3.one*scale;
            Design.anchoredPosition=new Vector2((safe.center.x/Screen.width-.5f)*parent.rect.width,(safe.center.y/Screen.height-.5f)*parent.rect.height);
        }
        void Clear(Transform parent){foreach(Transform t in parent.Cast<Transform>().ToArray())DestroyImmediate(t.gameObject);}
        public void Navigate(HubPage page)
        {
            Page=page;Clear(content);
            if(page==HubPage.Home)Home();
            else {
                var panel=W.Panel(content,"PagePanel",0,0,1420,680);
                W.Label(panel,Title(page),32,18,1250,55,35,null,true);
                W.Label(panel,"ROM / "+Title(page),34,72,1250,28,14,W.Muted);
                var body=W.Rect(panel,"Body",32,120,1356,530);
                switch(page){
                    case HubPage.Missions:Missions(body);break;case HubPage.Loadout:Loadout(body);break;case HubPage.Lobby:Lobby(body);break;
                    case HubPage.Marketplace:Market(body);break;case HubPage.Auction:Auction(body);break;case HubPage.Exchange:Exchange(body);break;
                    case HubPage.Progression:Progression(body);break;case HubPage.Settings:Settings(body);break;case HubPage.Social:Social(body);break;
                    case HubPage.Codex:W.Label(body,"EL BOSQUE FRACTURADO\n\nEl mundo aún recuerda. Las islas conservan los restos de ciudades y templos separados por las fisuras.\n\nExplorá los puentes, las ruinas y la Ciudad del Sol.",16,10,1250,350,26);break;
                    case HubPage.Results:Results(body);break;
                }
            }
            var first=content.GetComponentInChildren<Button>();if(first&&EventSystem.current)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
        static string Title(HubPage p)=>p switch {HubPage.Missions=>"SELECCIÓN DE MISIÓN",HubPage.Loadout=>"EQUIPAMIENTO E INVENTARIO",HubPage.Lobby=>"PREPARAR DESPLIEGUE",HubPage.Progression=>"PROGRESO Y RECOMPENSAS",HubPage.Auction=>"CASA DE SUBASTAS",HubPage.Exchange=>"INTERCAMBIO DE ANCIENT GOLD",HubPage.Marketplace=>"MARKETPLACE OFICIAL",HubPage.Settings=>"AJUSTES",HubPage.Social=>"ESCUADRA",HubPage.Results=>"RESULTADOS · VISTA PREVIA",_=>"CÓDICE"};
        void Home()
        {
            var party=W.Panel(content,"Party",1030,-165,390,250);
            W.Label(party,"ESCUADRA  /  1 DE 4",18,10,360,30,17,W.Gold);
            for(int i=0;i<4;i++)W.Label(party,i==0?"●  "+Services.Profile.Name+"   ·   LÍDER":"+  Plaza libre",18,50+i*47,360,40,20,i==0?W.White:W.Muted);
            var m=W.Panel(content,"FeaturedMission",970,182,450,498);
            var image=W.Image(m,"FeaturedArtwork",6,6,438,185,Color.white,Background);image.preserveAspect=false;
            W.Label(m,"INCURSIÓN",24,208,400,54,42,null,true);
            W.Label(m,"Bosque Fracturado",24,270,400,36,25,W.Red);
            W.Label(m,"Cruzá las fisuras. Defendé el bosque.\nExplorá un mundo hecho fragmentos.",24,316,400,72,21);
            W.Button(m,"PREPARAR MISIÓN",24,414,402,60,()=>Navigate(HubPage.Missions),true);
        }
        void Missions(Transform b)
        {
            for(int i=0;i<Services.Missions.Missions.Count;i++){
                var m=Services.Missions.Missions[i];var p=W.Panel(b,m.Name,i*450,0,430,390);
                W.Label(p,m.Type.ToUpperInvariant(),20,12,390,30,18,W.Red);W.Label(p,m.Name,20,50,390,64,30,null,true);
                W.Label(p,$"{m.Difficulty}  ·  {m.MinPlayers}–{m.MaxPlayers} jugadores\n{m.Duration}\n\n{m.Objectives}\n\n{m.Rewards}",20,120,390,205,18);
                W.Button(p,m.Available?"SELECCIONAR":"VISTA PREVIA",20,330,390,45,()=>{selected=m;Navigate(HubPage.Loadout);});
            }
            W.Label(b,"Incursión usa el mapa Horda existente. Recuperación e Investigación son vistas previas de diseño.",0,410,1300,55,20,W.Muted);
        }
        void Loadout(Transform b)
        {
            W.Label(b,"OPERADOR / "+Services.Profile.Name+"\nArma principal · Juramento de ceniza\nSecundaria · Eco del vacío\nEquipo · Coraza del caminante",10,0,480,160,24);
            W.Label(b,"VISTA PREVIA · No cambia el equipamiento del kit.",10,165,1250,30,18,W.Muted);
            for(int i=0;i<Services.Inventory.Items.Count;i++){
                var item=Services.Inventory.Items[i];var p=W.Panel(b,item.Name,i*335,220,318,175);
                W.Label(p,item.Rarity.ToString().ToUpperInvariant(),16,10,285,28,16,RarityColor(item.Rarity));
                W.Label(p,item.Name,16,45,285,55,23,null,true);
                W.Button(p,"INSPECCIONAR",16,115,285,44,()=>Dialog(item.Name,$"{item.Description}\n{item.Stats}\nCategoría: {item.Category} · Cantidad {item.Quantity}\nTransferible: {(item.Tradable?"Sí":"No")}\nID: {item.Id}\nPropietario: {item.OwnerId}\n\nMetadatos de ejemplo. Validación autoritativa pendiente.",null));
            }
            W.Button(b,"CONTINUAR A LA ESCUADRA",820,440,500,60,()=>Navigate(HubPage.Lobby),true);
        }
        static Color RarityColor(Rarity rarity)=>rarity==Rarity.Legendary?new Color(1,.7f,.28f):rarity==Rarity.Epic?new Color(.75f,.45f,1):rarity==Rarity.Rare?new Color(.35f,.7f,1):W.Muted;
        void Lobby(Transform b)
        {
            W.Label(b,selected.Name+" / "+selected.Type,8,0,1300,45,29,null,true);
            for(int i=0;i<4;i++)W.Label(b,i==0?$"●  {Services.Profile.Name}  ·  LÍDER  ·  {(Services.Party.LocalReady?"LISTO":"PREPARANDO")}":"+  Plaza libre · Conexión desde el menú del kit",16,70+i*60,1290,45,24,i==0?W.White:W.Muted);
            W.Label(b,"Escuadra visual de prueba. El estado remoto Ready aún no se sincroniza con Mirror.",16,325,1290,45,18,W.Muted);
            W.Button(b,Services.Party.LocalReady?"CANCELAR LISTO":"ESTOY LISTO",16,410,350,60,()=>{Services.Party.LocalReady=!Services.Party.LocalReady;Navigate(HubPage.Lobby);});
            var launch=W.Button(b,"DESPLEGAR COMO HOST",390,410,460,60,Deploy,true);launch.interactable=Services.Party.LocalReady&&selected.Available&&Creator;
            W.Button(b,"RESULTADOS DE EJEMPLO",880,410,440,60,()=>Navigate(HubPage.Results));
        }
        public void Deploy()
        {
            if(!Creator||!Services.Party.LocalReady||!selected.Available){Dialog("Despliegue no disponible","Abrí el hub principal para iniciar el mapa existente.",null);return;}
            int index=Array.FindIndex(Creator.Maps,m=>m&&m.Scene!=null&&m.Scene.Contains("map_rom_horde"));
            if(index<0){Dialog("Mapa no disponible","No se encontró map_rom_horde en RoomCreator.",null);return;}
            Creator.MapselectionDropdown.value=index; Creator.MapselectionDropdown.onValueChanged.Invoke(index);
            int mode=Creator.GamemodeSelectionDropdown.options.FindIndex(o=>o.text=="Raid");if(mode>=0)Creator.GamemodeSelectionDropdown.value=mode;
            int players=Array.FindIndex(Creator.Maps[index].MaxPlayersPresets,n=>n==4);if(players>=0)Creator.PlayerNumberDropdown.value=players;
            Creator.BotsToggle.isOn=false;
            Dialog("Desplegar incursión","Se iniciará una partida real como host en Bosque Fracturado. Las recompensas del prototipo no se aplican.",()=>{ROMLoadingScreen.Show("Desplegando",selected.Name);Creator.HostGameButton.onClick.Invoke();});
        }
        void Progression(Transform b)
        {
            W.Label(b,"Nv. 42  /  5.600 de 10.000 XP",10,0,1300,60,32,null,true);
            W.Image(b,"XPTrack",10,80,1280,12,W.Gold);W.Image(b,"XP",10,80,1280*Services.Profile.Experience,12,W.Red);
            W.Label(b,"LOGROS\nExplorador de fisuras · Desbloqueado (ejemplo)\nDefensor del bosque · En progreso\n\nRecompensas de misión y participación activa\nAG / RC · Cantidades pendientes de balance\nEstado: "+Services.Rewards.State+"\nHistorial: sin emisión de moneda real.",10,125,1250,300,24);
            W.Button(b,"SOLICITAR VALIDACIÓN · DEMO",10,440,550,55,()=>{var result=Services.Rewards.PreviewClaim();Navigate(HubPage.Progression);Dialog("Recompensa",result.Message,null);});
        }
        void Market(Transform b)
        {
            string[] categories={"Destacados","Temporada","Personaje","Arma","Bundle","Limitadas"};
            for(int i=0;i<categories.Length;i++){string c=categories[i];W.Button(b,c,i*222,0,210,45,()=>{category=c;Navigate(HubPage.Marketplace);});}
            int column=0;
            foreach(var product in Services.Marketplace.Products.Where(p=>category=="Todas"||category=="Destacados"||category=="Temporada"||p.Category==category)){
                var p=W.Panel(b,product.Name,column++*450,70,430,330);
                var portrait=Theme&&Theme.Portraits!=null&&Theme.Portraits.Length>0?Theme.Portraits[(column-1)%Theme.Portraits.Length].Sprite:null;
                W.Image(p,"ConceptPreview",16,12,100,120,Color.white,portrait).preserveAspect=true;
                W.Label(p,product.Name,130,20,280,80,26,null,true);W.Label(p,product.Season+"\n"+product.GoldPrice+" AG",130,100,280,64,20,W.Gold);
                W.Label(p,"Arte de referencia · Producto de prueba",16,178,395,35,16,W.Muted);
                W.Button(p,product.Owned?"ADQUIRIDO · DEMO":"VER COSMÉTICO",16,245,398,60,()=>Dialog(product.Name,product.Description+"\nPrecio de ejemplo: "+product.GoldPrice+" AG\nSin descuentos del saldo real.",()=>{var result=Services.Marketplace.PreviewPurchase(product.Id);Navigate(HubPage.Marketplace);Dialog("Compra de prueba",result.Message,null);}));
            }
            W.Label(b,"Tienda oficial de cosméticos · Ancient Gold se obtiene jugando. No hay venta directa de AG.",0,430,1300,65,20,W.Muted);
        }
        void Auction(Transform b)
        {
            string[] tabs={"Explorar","Vender","Mis anuncios","Historial","Intercambio"};
            for(int i=0;i<tabs.Length;i++){string tab=tabs[i];W.Button(b,tab,i*270,0,255,45,()=>{if(tab=="Intercambio")Navigate(HubPage.Exchange);else{auctionTab=tab;Navigate(HubPage.Auction);}});}
            if(auctionTab=="Explorar"){
                W.Input(b,"Buscar anuncio…",0,70,300,v=>{query=v;RefreshListings(b);});
                W.Button(b,"Categoría: "+category,320,70,300,48,()=>{category=category=="Arma"?"Todas":"Arma";Navigate(HubPage.Auction);});
                W.Button(b,"Rareza: "+rarity,640,70,300,48,()=>{rarity=rarity=="Epic"?"Todas":"Epic";Navigate(HubPage.Auction);});
                W.Button(b,descending?"Precio ↓":"Precio ↑",960,70,200,48,()=>{descending=!descending;Navigate(HubPage.Auction);});
                W.Button(b,"Máx. "+maxPrice+" RC",1170,70,175,48,()=>{maxPrice=maxPrice==100?10000:100;Navigate(HubPage.Auction);});
                RefreshListings(b);
            } else if(auctionTab=="Vender"){
                W.Label(b,"Seleccioná un objeto transferible. Precio de ejemplo 100 RC · Duración 24 h · Tarifa por definir.",0,85,1330,65,22);
                var price=W.Input(b,"Precio solicitado (RC)",0,160,400,v=>{int.TryParse(v,out draftPrice);});price.contentType=InputField.ContentType.IntegerNumber;price.text=draftPrice.ToString();
                W.Button(b,"Duración: "+draftHours+" h",430,160,350,48,()=>{draftHours=draftHours==24?48:24;Navigate(HubPage.Auction);});
                int row=0;foreach(var item in Services.Inventory.Items.Where(i=>i.Tradable)){var captured=item;W.Button(b,"OFRECER · "+item.Name,0,230+row++*70,820,55,()=>Dialog("Revisar anuncio",captured.Name+"\nPrecio: "+draftPrice+" RC · Duración: "+draftHours+" h\nTarifas pendientes de configuración.\nNo se publicará ni reservará ningún objeto real.",()=>Dialog("Simulación",Services.Auction.PreviewListing(captured.Id,draftPrice,draftHours).Message,null)));}
            } else W.Label(b,auctionTab+"\n\nTodavía no hay transacciones reales.\nLas operaciones de ejemplo no crean anuncios ni mueven objetos.\n\nEstados previstos: activo, vencido, vendido, cancelado.\nCada operación real tendrá ID y registro de auditoría.",0,90,1300,350,25);
        }
        void RefreshListings(Transform b)
        {
            var old=b.Find("Listings");if(old)DestroyImmediate(old.gameObject);var rows=W.Rect(b,"Listings",0,150,1350,350);
            var list=Services.Auction.Listings.Where(l=>l.Item.IndexOf(query,StringComparison.OrdinalIgnoreCase)>=0&&(category!="Arma"||l.Category==category)&&(rarity=="Todas"||l.Rarity.ToString()==rarity)&&l.Price<=maxPrice);
            list=descending?list.OrderByDescending(l=>l.Price):list.OrderBy(l=>l.Price);int y=0;
            foreach(var l in list){W.Label(rows,$"{l.Item}   ·   {l.Seller}   ·   {l.Price} RC",14,y,930,60,24);W.Button(rows,"INSPECCIONAR",1010,y,320,55,()=>Dialog(l.Item,$"Vendedor: {l.Seller}\nRareza: {l.Rarity}\nPrecio fijo: {l.Price} RC\nEstado: {l.Status}\n{l.Id}\n\nEjemplo de anuncio entre jugadores.",()=>Dialog("Operación de prueba",Services.Auction.PreviewTransaction(l.Id).Message,null)));y+=80;}
            if(y==0)W.Label(rows,"Sin anuncios para estos filtros.",10,0,1250,70,24,W.Muted);
            W.Label(rows,"Anuncios de jugadores · Datos de ejemplo · Sin pagos ni transferencias reales",10,290,1300,40,18,W.Muted);
        }
        void Exchange(Transform b){W.Label(b,"ANCIENT GOLD / INTERCAMBIO ENTRE JUGADORES\n\n"+Services.Exchange.UnavailableReason+"\n\nMódulo independiente · Sin ofertas ejecutables\nHistorial: vacío\n\nLa liquidación necesitará reserva de fondos, validación de propiedad,\ntransacciones atómicas, protección contra duplicados y auditoría.",10,10,1300,380,25);W.Button(b,"VOLVER A SUBASTAS",10,430,440,55,()=>Navigate(HubPage.Auction));}
        void Settings(Transform b)
        {
            W.Label(b,"Volumen general",10,0,1200,42,25);W.Slider(b,"Volume",10,55,700,0,1,AudioListener.volume,v=>AudioListener.volume=v);
            W.Label(b,"Sensibilidad del mouse",10,120,1200,42,25);W.Slider(b,"Sensitivity",10,170,700,.1f,5,UserSettings.MouseSensitivity,v=>UserSettings.MouseSensitivity=v);
            W.Label(b,"Campo de visión",10,235,1200,42,25);W.Slider(b,"FOV",10,285,700,60,110,UserSettings.FieldOfView,v=>UserSettings.FieldOfView=v);
            W.Label(b,"Ajustes de sesión conectados al kit. Persistencia y opciones táctiles avanzadas pendientes.",10,365,1300,65,21,W.Muted);
        }
        void Social(Transform b){W.Label(b,"Tu escuadra tiene cuatro plazas.\n\n"+Services.Profile.Name+" · Líder local\nPlaza 2 · Libre\nPlaza 3 · Libre\nPlaza 4 · Libre\n\nInvitaciones, amigos y presencia requieren un servicio social.\nPodés usar la conexión por dirección del menú existente.",10,0,1300,370,26);W.Button(b,"ABRIR CONEXIÓN EXISTENTE",10,425,600,60,OpenLegacy);}
        void Results(Transform b){W.Label(b,"VISTA PREVIA DE RESULTADOS\n\nMisión: "+selected.Name+"\nObjetivos · Estadísticas · Experiencia\nRecompensas AG / RC pendientes de validación\n\nEste panel no está conectado al final de una partida real.",10,10,1300,370,27);W.Button(b,"VOLVER AL HUB",10,430,440,60,()=>Navigate(HubPage.Home),true);}
        void OpenLegacy()
        {
            if(!LegacyMenu){Dialog("Conexión","Disponible desde la escena hub principal.",null);return;}
            LegacyMenu.SetActive(true);gameObject.SetActive(false);
        }
        public void ReturnFromLegacy(){if(LegacyMenu)LegacyMenu.SetActive(false);gameObject.SetActive(true);Navigate(HubPage.Home);}
        public void Dialog(string title,string message,Action confirm)
        {
            if(modal)DestroyImmediate(modal.gameObject);
            modal=W.Rect(Design,"Modal",0,0,1920,1080);var shade=W.Image(modal,"Blocker",0,0,1920,1080,new Color(0,0,0,.78f));shade.raycastTarget=true;
            var panel=W.Panel(modal,"Dialog",510,255,900,570);W.Label(panel,title,32,25,836,65,32,null,true);W.Label(panel,message,32,110,836,320,23);
            W.Button(panel,confirm==null?"CERRAR":"CANCELAR",32,470,380,60,()=>DestroyImmediate(modal.gameObject));
            if(confirm!=null)W.Button(panel,"CONFIRMAR",460,470,408,60,()=>{DestroyImmediate(modal.gameObject);confirm();},true);
            var first=panel.GetComponentInChildren<Button>();if(EventSystem.current&&first)EventSystem.current.SetSelectedGameObject(first.gameObject);
        }
    }
}
