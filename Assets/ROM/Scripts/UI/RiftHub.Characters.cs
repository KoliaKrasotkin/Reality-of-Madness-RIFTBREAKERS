using System.Linq;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.UI;
using W=ROM.UI.Sprint01.RiftWidgets;
namespace ROM.UI.Sprint01
{
    public sealed partial class RiftHub
    {
        LocalCharacterRoster roster;
        string rosterAccount;
        LocalCharacterRoster Roster
        {
            get{
                // Also support calls from existing UI buttons before Start or after reload.
                if(Social==null)Initialize();
                var profile=Social.Self;
                if(roster==null||rosterAccount!=profile.Id){rosterAccount=profile.Id;roster=new LocalCharacterRoster(rosterAccount,profile.Name,profile.Skin,PlayerPrefs.GetInt(AbilityController.Preference,0)>0?"warrior":"legacy:"+PlayerPrefs.GetInt(PlayerInstance.ClassPreference,0));}
                return roster;
            }
        }
        string[] ClassKeys=>new[]{"warrior"};
        string ClassName(string key){if(key=="warrior")return "Warrior";if(key==PaladinPrototypeSettings.ClassKey)return "Paladín (prototipo)";if(key!=null&&key.StartsWith("legacy:")&&int.TryParse(key.Substring(7),out int id))return RiftClassRules.Load()?.Find(id)?.Name??"Clase";return "Clase";}
        public void ApplyActiveCharacter()
        {
            var record=Roster.Active;if(record==null)return;RiftCharacterWallet.ImportLegacy(record);Roster.SaveIdentity();PlayerPrefs.SetString("ROM.Character.ActiveId",record.Id);
            if(record.ClassKey=="warrior"||record.ClassKey==PaladinPrototypeSettings.ClassKey){
                // A class selection has one default balance profile; no specialization is selected by the player.
                PlayerPrefs.SetInt(AbilityController.Preference,record.ClassKey==PaladinPrototypeSettings.ClassKey?1:2);PlayerPrefs.SetInt(PlayerInstance.ClassPreference,1);
            }else{PlayerPrefs.SetInt(AbilityController.Preference,0);if(int.TryParse(record.ClassKey.Replace("legacy:",""),out int id))PlayerPrefs.SetInt(PlayerInstance.ClassPreference,id);}
            UserSettings.CharacterSkinID=record.Skin;PlayerPrefs.SetInt("_playerPrefs_CharacterSkinID",record.Skin);UserSettings.UserNickname=record.DisplayName;PlayerPrefs.SetString("_playerPrefs_Username",record.DisplayName);PlayerPrefs.Save();
            if(Social.Self.Skin!=record.Skin)Social.SetAppearance(record.Skin);
        }
        bool RequireWarrior(){if(Roster.Active?.Available==true){ApplyActiveCharacter();return true;}homePanel="Characters";Navigate(HubPage.Home);Notify("Este personaje no está disponible. Creá o seleccioná un Warrior; sus datos se conservan.");return false;}
        void DrawClassSelection(){DrawCharacters();}
        bool creationPortrait;bool creationPaladin;
        RiftModelView characterPreview;
        RectTransform CharacterScreen(string title)
        {
            var root=W.Rect(content,"Rift character screen",-36,-168,1920,1080);
            var background=MenuSurface(root,"Character screen glass",40,40,1840,1000,new Color(.022f,.025f,.043f,.9f),new Color(.38f,.34f,.5f));background.raycastTarget=true;
            W.Label(root,title,270,64,1350,64,38,new Color(.95f,.93f,1));
            W.Label(root,"RIFTBREAKERS",270,122,800,28,15,new Color(.68f,.56f,.85f));
            CharacterButton(root,"VOLVER",72,74,160,48,()=>{homePanel=homePanel=="CreateCharacter"?"Characters":"";Navigate(HubPage.Home);});
            CharacterButton(root,"CERRAR",1690,74,150,48,()=>{homePanel="";Navigate(HubPage.Home);});
            return root;
        }
        Button CharacterButton(Transform parent,string text,float x,float y,float width,float height,System.Action action,bool primary=false)
        {
            var surface=MenuSurface(parent,"Character action "+text,x,y,width,height,new Color(primary?.16f:.055f,primary?.07f:.05f,primary?.26f:.09f,.95f),primary?new Color(.65f,.39f,1):new Color(.4f,.35f,.53f),primary?6:0);surface.raycastTarget=true;
            var button=surface.gameObject.AddComponent<Button>();button.targetGraphic=surface;
            var colors=button.colors;colors.highlightedColor=new Color(1.2f,1.12f,1.35f);colors.selectedColor=colors.highlightedColor;colors.pressedColor=new Color(.72f,.65f,.85f);colors.disabledColor=new Color(.45f,.42f,.5f);button.colors=colors;
            var label=W.Label(surface.transform,text,12,0,width-24,height,height<60?18:22,new Color(.94f,.92f,1));label.alignment=TextAnchor.MiddleCenter;label.raycastTarget=false;
            button.onClick.AddListener(()=>StartCoroutine(DeferMainMenuAction(action)));return button;
        }
        void CharacterPortrait(Transform parent,RiftPlayerCharacter character,float x,float y,float size,bool? selected=null)
        {
            var ring=MenuSurface(parent,"Character portrait",x,y,size,size,new Color(.025f,.02f,.04f),new Color(.48f,.35f,.66f));
            var assets=RiftCharacterPresentationAssets.Load;bool use=character.Skin==1;
            if(use&&assets&&assets.NertorPortrait){var image=W.Rect(ring.transform,"Nertor chosen portrait",3,3,size-6,size-6).gameObject.AddComponent<Image>();image.sprite=assets.NertorPortrait;image.preserveAspect=true;image.raycastTarget=false;}
            else{var icon=W.Rect(ring.transform,"Warrior portrait symbol",size*.2f,size*.2f,size*.6f,size*.6f).gameObject.AddComponent<ROM.UI.Gameplay.RiftHUDGraphic>();icon.Shape=ROM.UI.Gameplay.HUDShape.Helmet;icon.color=new Color(.74f,.66f,.9f);icon.raycastTarget=false;}
        }
        void CharacterHero(Transform root,int skin,RiftPlayerCharacter record=null,bool classCreation=false)
        {
            var model=Catalog?Catalog.Characters.FirstOrDefault(c=>c.SkinIndex==skin):null;
            if(model==null||!model.Prefab){W.Label(root,"Vista de personaje no disponible",580,420,740,80,22,W.Muted);return;}
            characterPreview=RiftModelView.Create(root,530,classCreation?244:154,850,classCreation?650:760,Catalog,false);characterPreview.name="Character hero 3D";characterPreview.Show(model);characterPreview.UseInspectionRendering();characterPreview.SetInspectionZoom(.9f);characterPreview.Turn(-15);
            ApplyCharacterPreviewEquipment(characterPreview,record);RiftLivingMenuAssets.Attach(characterPreview);
            W.Label(root,"Arrastrá para girar · Rueda para acercar",590,900,730,36,16,new Color(.7f,.66f,.8f)).alignment=TextAnchor.MiddleCenter;
        }
        static void ApplyCharacterPreviewEquipment(RiftModelView preview,RiftPlayerCharacter record){
            if(record==null||!preview||!preview.PreviewSubject)return;ROM.Progression.JournalState state=null;
            try{state=JsonUtility.FromJson<ROM.Progression.JournalState>(PlayerPrefs.GetString(ROM.Progression.RiftJournalView.Key(record.Id),""));}catch(System.Exception){}
            if(state==null||state.CharacterId!=record.Id||!state.SwordAndShield)return;
            var sword=WarriorProgressionDefinition.Load.Sword;var animator=preview.PreviewSubject.GetComponentInChildren<Animator>();
            if(!sword||!animator||!animator.isHuman)return;
            var equipped=preview.PreviewSubject.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Equipped Warrior Axe");
            var presentation=animator.gameObject.AddComponent<CharacterWeaponPresentation>();presentation.Animator=animator;presentation.BuiltInWeapon=equipped?equipped.gameObject:null;presentation.Equip(sword);presentation.RefreshPose();
            foreach(var child in preview.PreviewSubject.GetComponentsInChildren<Transform>(true))child.gameObject.layer=RiftModelView.PreviewLayer;
        }
        void DrawCharacters()
        {
            if(InGame){Notify("Volvé al menú para cambiar de personaje.");return;}
            var active=Roster.Active;if(active==null){DrawEmptyCharacters();return;}var root=CharacterScreen("TUS PERSONAJES");
            MenuSurface(root,"Character list panel",70,180,450,684,new Color(.025f,.026f,.045f,.9f),new Color(.35f,.32f,.46f));
            var rows=W.ScrollArea(root,"Character roster list",82,194,426,650,Mathf.Max(650,Roster.Characters.Count*116));
            for(int i=0;i<Roster.Characters.Count;i++){
                var character=Roster.Characters[i];string id=character.Id;int slot=i+1;bool selected=active.Id==id;
                var surface=MenuSurface(rows,"Character row "+slot,0,i*116,410,106,new Color(selected?.12f:.035f,selected?.065f:.035f,selected?.2f:.06f,.95f),selected?new Color(.68f,.4f,1):new Color(.28f,.26f,.36f),selected?4:0);surface.raycastTarget=true;
                var row=surface.gameObject.AddComponent<Button>();row.targetGraphic=surface;var colors=row.colors;colors.disabledColor=new Color(.55f,.52f,.62f);colors.highlightedColor=new Color(1.18f,1.12f,1.3f);row.colors=colors;row.interactable=character.Available;
                CharacterPortrait(surface.transform,character,10,15,74);
                W.Label(surface.transform,character.DisplayName+" · "+slot,98,12,294,30,22,new Color(.94f,.92f,1));
                W.Label(surface.transform,RiftCharacterMetadata.LevelLabel(id)+" · "+ClassName(character.ClassKey),98,46,294,24,16,new Color(.76f,.72f,.85f));
                W.Label(surface.transform,!character.Available?"Clase no disponible":selected?"SELECCIONADO":"",98,75,294,22,13,new Color(.74f,.56f,.97f));
                row.onClick.AddListener(()=>StartCoroutine(DeferMainMenuAction(()=>{if(!Roster.Select(id))return;ApplyActiveCharacter();SocialChanged();})));
            }
            if(active.Available)CharacterHero(root,active.Skin,active);
            MenuSurface(root,"Selected character details",1400,180,440,640,new Color(.03f,.027f,.05f,.92f),new Color(.43f,.34f,.59f));
            CharacterPortrait(root,active,1430,210,92);W.Label(root,active.DisplayName,1540,215,275,45,29,new Color(.96f,.93f,1));W.Label(root,ClassName(active.ClassKey),1540,263,275,32,19,new Color(.74f,.68f,.86f));
            W.Label(root,RiftCharacterMetadata.LevelLabel(active.Id),1430,330,380,42,27,new Color(.85f,.77f,1));var played=RiftCharacterMetadata.PlayedLabel(active.Id);if(!string.IsNullOrEmpty(played))W.Label(root,played,1430,382,380,34,17,new Color(.73f,.7f,.81f));
            W.Label(root,active.ClassKey==PaladinPrototypeSettings.ClassKey?PaladinPrototypeSettings.Load.Description:active.Available?"Guerrero · Furia\n\nHacha a dos manos\no espada y escudo\n\n"+(active.CreliumCompleted?"El Rift te espera.":"Antes del Rift: Crelium."):"Esta clase todavía no está disponible.",1430,445,380,220,21,new Color(.8f,.76f,.87f));
            CharacterButton(root,"BORRAR PERSONAJE",1424,830,392,44,ConfirmDeleteCharacter);
            if(!string.IsNullOrEmpty(Roster.LastDeletedId)&&!Roster.Characters.Any(c=>c.Id==Roster.LastDeletedId))CharacterButton(root,"DESHACER BORRADO",70,850,450,40,()=>{if(Roster.RestoreDeleted(Roster.LastDeletedId)){ApplyActiveCharacter();SocialChanged();}});
            CharacterButton(root,"+ CREAR PERSONAJE",70,900,450,68,()=>{creationPortrait=false;homePanel="CreateCharacter";Navigate(HubPage.Home);});
            var play=CharacterButton(root,active.ClassKey==PaladinPrototypeSettings.ClassKey?"PROBAR EN EL RIFT":active.CreliumCompleted?"ENTRAR A EL RIFT":"CONTINUAR EN CRELIUM",1400,900,440,68,()=>{if(!RequireWarrior())return;if(Roster.Active.ClassKey==PaladinPrototypeSettings.ClassKey||Roster.Active.CreliumCompleted)Deploy();else ROM.Tutorial.Crilium.CriliumSession.Enter(roster);},true);play.interactable=active.Available;
            if(UnityEngine.EventSystems.EventSystem.current)UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(play.interactable?play.gameObject:root.GetComponentsInChildren<Button>().First(b=>b.interactable).gameObject);
            ROM.UI.Gameplay.RiftAbilityTooltip.HideAll();
        }
        void DrawEmptyCharacters(){var root=CharacterScreen("TUS PERSONAJES");W.Label(root,"No tenés personajes",90,260,430,70,30,new Color(.9f,.83f,1));W.Label(root,"Creá un Warrior para comenzar.",90,350,430,100,23,W.Muted);CharacterButton(root,"+ CREAR PERSONAJE",70,900,450,68,()=>{homePanel="CreateCharacter";Navigate(HubPage.Home);},true);CharacterButton(root,"SALIR",1400,900,440,68,MenuExit);if(!string.IsNullOrEmpty(Roster.LastDeletedId)&&!Roster.Characters.Any(c=>c.Id==Roster.LastDeletedId))CharacterButton(root,"DESHACER BORRADO",70,820,450,50,()=>{if(Roster.RestoreDeleted(Roster.LastDeletedId)){ApplyActiveCharacter();SocialChanged();}});}
        void ConfirmDeleteCharacter(){
            var record=Roster.Active;if(record==null)return;string id=record.Id,snapshot=JsonUtility.ToJson(record);
            string message=record.DisplayName+"\n"+ClassName(record.ClassKey)+" · "+RiftCharacterMetadata.LevelLabel(id)+"\n\n¿Borrar este personaje? Podés deshacerlo.";
            Dialog("Borrar personaje",message,()=>{if(Roster.Active?.Id!=id||JsonUtility.ToJson(Roster.Active)!=snapshot){Notify("La selección cambió. No se borró ningún personaje.");return;}if(!Roster.DeleteRecoverable(id)){Notify("No se pudo borrar el personaje.");return;}if(Roster.Active!=null)ApplyActiveCharacter();else{PlayerPrefs.DeleteKey("ROM.Character.ActiveId");PlayerPrefs.Save();}homePanel="Characters";SocialChanged();});
            var panel=modal.Find("Dialog");CharacterPortrait(panel,record,32,108,100);var label=panel.GetComponentsInChildren<Text>().First(t=>t.text==message);label.rectTransform.anchoredPosition=new Vector2(156,-96);label.rectTransform.sizeDelta=new Vector2(560,170);
        }
        void DrawCharacterCreation()
        {
            if(InGame){Notify("La creación está disponible desde el menú principal.");return;}
            var root=CharacterScreen("CREAR PERSONAJE");var assets=RiftCharacterPresentationAssets.Load;bool paladin=creationPaladin&&PaladinPrototypeSettings.Available;int skin=paladin?PaladinPrototypeSettings.Skin:1;
            MenuSurface(root,"Class lore panel",70,180,450,650,new Color(.03f,.027f,.05f,.92f),new Color(.43f,.34f,.59f));
            W.Label(root,paladin?"PALADÍN":"WARRIOR",100,208,390,54,32,new Color(.9f,.8f,1));W.Label(root,RiftSkinIdentity.Name(skin),100,275,390,48,28,new Color(.96f,.94f,1));
            W.Label(root,paladin?PaladinPrototypeSettings.Load.Description:assets?assets.Lore:"El guerrero de Crelium.",100,360,390,300,22,new Color(.79f,.75f,.85f));W.Label(root,paladin?"PROTOTIPO JUGABLE":"CLASE DISPONIBLE",100,740,390,34,15,new Color(.68f,.51f,.91f));
            // Keep the interactive render texture below the class tabs. Its transparent
            // pixels still receive raycasts, so drawing tabs behind it blocks selection.
            CharacterHero(root,skin,null,true);
            CharacterButton(root,"WARRIOR",600,180,270,44,()=>{creationPaladin=false;Navigate(HubPage.Home);},!paladin);if(PaladinPrototypeSettings.Available)CharacterButton(root,"PALADÍN (PRUEBA)",890,180,320,44,()=>{creationPaladin=true;Navigate(HubPage.Home);},paladin);
            MenuSurface(root,"Class playstyle panel",1400,180,440,650,new Color(.03f,.027f,.05f,.92f),new Color(.43f,.34f,.59f));
            W.Label(root,"ESTILO DE JUEGO",1430,208,380,46,26,new Color(.88f,.79f,1));W.Label(root,paladin?"Cuerpo a cuerpo\n\nReposo, caminar y ataque básico.\n\nSin habilidades ni talentos de clase todavía.":assets?assets.Style:"Cuerpo a cuerpo · Furia",1430,292,380,375,21,new Color(.79f,.75f,.85f));
            CharacterPortrait(root,new RiftPlayerCharacter{Skin=skin},1430,714,76);W.Label(root,RiftSkinIdentity.Name(skin),1530,730,275,40,22,new Color(.86f,.78f,1));
            CharacterButton(root,"CANCELAR",70,900,450,68,()=>{creationPortrait=false;homePanel="Characters";Navigate(HubPage.Home);});
            CharacterButton(root,"CREAR Y SELECCIONAR",1400,900,440,68,()=>{string error=Roster.Create(RiftSkinIdentity.Name(skin),paladin?PaladinPrototypeSettings.ClassKey:"warrior",skin);if(error!=null){Notify(error);return;}ApplyActiveCharacter();homePanel="Characters";SocialChanged();Notify("Personaje creado: "+Roster.Active.DisplayName);},true);
            ROM.UI.Gameplay.RiftAbilityTooltip.HideAll();
        }

        void DrawWarriorEquipment(Transform body)
        {
            W.Label(body,"WARRIOR",0,0,650,40,30,W.Gold);
            W.Label(body,Roster.Active.DisplayName+" · Lizardman\nHacha o espada y escudo · Furia\n\nClic / T: autoataque\n1–7: habilidades\nB: personaje y equipo\nK: habilidades y talentos",0,65,650,300,23);
            W.Button(body,"PERSONAJES",0,410,400,55,()=>{homePanel="Characters";Navigate(HubPage.Home);});
            ShowModel(body,CurrentCharacter(),750,0,1020,550);
        }
    }
}


