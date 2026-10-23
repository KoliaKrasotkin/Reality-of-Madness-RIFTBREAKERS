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
        string ClassName(string key){if(key=="warrior")return "Warrior";if(key!=null&&key.StartsWith("legacy:")&&int.TryParse(key.Substring(7),out int id))return RiftClassRules.Load()?.Find(id)?.Name??"Clase";return "Clase";}
        public void ApplyActiveCharacter()
        {
            var record=Roster.Active;RiftCharacterWallet.ImportLegacy(record);Roster.SaveIdentity();PlayerPrefs.SetString("ROM.Character.ActiveId",record.Id);
            if(record.ClassKey=="warrior"){
                // A class selection has one default balance profile; no specialization is selected by the player.
                PlayerPrefs.SetInt(AbilityController.Preference,2);PlayerPrefs.SetInt(PlayerInstance.ClassPreference,1);
            }else{PlayerPrefs.SetInt(AbilityController.Preference,0);if(int.TryParse(record.ClassKey.Replace("legacy:",""),out int id))PlayerPrefs.SetInt(PlayerInstance.ClassPreference,id);}
            UserSettings.CharacterSkinID=record.Skin;PlayerPrefs.SetInt("_playerPrefs_CharacterSkinID",record.Skin);UserSettings.UserNickname=record.DisplayName;PlayerPrefs.SetString("_playerPrefs_Username",record.DisplayName);PlayerPrefs.Save();
            if(Social.Self.Skin!=record.Skin)Social.SetAppearance(record.Skin);
        }
        bool RequireWarrior(){if(Roster.Active.Available){ApplyActiveCharacter();return true;}homePanel="Characters";Navigate(HubPage.Home);Notify("Este personaje no está disponible. Creá o seleccioná un Warrior; sus datos se conservan.");return false;}
        void DrawClassSelection(){DrawCharacters();}
        void DrawCharacters()
        {
            var panel=Drawer("CharacterRoster","PERSONAJES",0,650,710);
            for(int i=0;i<Roster.Characters.Count;i++){var character=Roster.Characters[i];string id=character.Id;W.Button(panel,character.DisplayName+" · "+(character.Available?"Warrior":"NO DISPONIBLE"),22,75+i*63,606,52,()=>{if(InGame){Notify("Volvé al menú para cambiar de personaje.");return;}if(!Roster.Select(id)){Notify("Clase no disponible. Sus datos se conservan; creá o seleccioná Warrior.");return;}ApplyActiveCharacter();SocialChanged();},character.Id==Roster.Active.Id);}
            W.Button(panel,"+ CREAR PERSONAJE",22,620,606,60,()=>{homePanel="CreateCharacter";Navigate(HubPage.Home);});
        }
        void DrawCharacterCreation()
        {
            var panel=Drawer("CreateCharacter","CREAR PERSONAJE",0,650,510);
            W.Label(panel,"Warrior · "+RiftSkinIdentity.Name(1),22,100,606,70,26,W.Gold);
            W.Label(panel,"El nombre pertenece a la skin. Cada personaje conserva un ID y progreso propios.",22,190,606,90,20,W.Muted);
            W.Label(panel,"Warrior usa Lizardman y un hacha. Podés crear varios personajes\nde esta clase; cada uno conserva su identidad local.",22,295,606,90,20,W.Muted);
            W.Button(panel,"CREAR Y SELECCIONAR",22,404,606,64,()=>{
                if(InGame){Notify("La creación está disponible desde el menú principal.");return;}
                string error=Roster.Create(RiftSkinIdentity.Name(1),"warrior",1);if(error!=null){Notify(error);return;}
                ApplyActiveCharacter();homePanel="Characters";SocialChanged();Notify("Personaje creado: "+Roster.Active.DisplayName);
            },true);
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
