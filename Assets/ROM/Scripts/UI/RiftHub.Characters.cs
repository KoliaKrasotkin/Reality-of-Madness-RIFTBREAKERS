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
        string rosterAccount,createName="",createClass="warrior";
        int createSkin=1;
        LocalCharacterRoster Roster
        {
            get{
                if(roster==null||rosterAccount!=Social.Self.Id){rosterAccount=Social.Self.Id;roster=new LocalCharacterRoster(rosterAccount,Social.Self.Name,Social.Self.Skin,PlayerPrefs.GetInt(AbilityController.Preference,0)>0?"warrior":"legacy:"+PlayerPrefs.GetInt(PlayerInstance.ClassPreference,0));}
                return roster;
            }
        }
        string[] ClassKeys=>new[]{"warrior","legacy:0","legacy:1","legacy:2","legacy:3"};
        string ClassName(string key){if(key=="warrior")return "Warrior";if(key!=null&&key.StartsWith("legacy:")&&int.TryParse(key.Substring(7),out int id))return RiftClassRules.Load()?.Find(id)?.Name??"Clase";return "Clase";}
        public void ApplyActiveCharacter()
        {
            var record=Roster.Active;PlayerPrefs.SetString("ROM.Character.ActiveId",record.Id);
            if(record.ClassKey=="warrior"){
                if(CombatSpecies&&!CombatSpecies.AllowsSkin(record.Skin))Roster.SetSkin(CombatSpecies.SkinIndices[0]);
                // A class selection has one default balance profile; no specialization is selected by the player.
                PlayerPrefs.SetInt(AbilityController.Preference,2);PlayerPrefs.SetInt(PlayerInstance.ClassPreference,1);
            }else{PlayerPrefs.SetInt(AbilityController.Preference,0);if(int.TryParse(record.ClassKey.Replace("legacy:",""),out int id))PlayerPrefs.SetInt(PlayerInstance.ClassPreference,id);}
            UserSettings.CharacterSkinID=record.Skin;PlayerPrefs.SetInt("_playerPrefs_CharacterSkinID",record.Skin);UserSettings.UserNickname=record.Name;PlayerPrefs.SetString("_playerPrefs_Username",record.Name);PlayerPrefs.Save();
            if(Social.Self.Skin!=record.Skin)Social.SetAppearance(record.Skin);
        }
        void DrawClassSelection()
        {
            var panel=Drawer("ChooseClass","CLASE",0,500,510);
            for(int i=0;i<ClassKeys.Length;i++){string key=ClassKeys[i];W.Button(panel,ClassName(key),22,76+i*66,456,54,()=>{
                if(InGame){Notify("Elegí tu personaje y su clase desde el menú principal.");return;}
                int skin=key=="warrior"?CombatSpecies.SkinIndices[0]:Roster.Active.Skin;Roster.SetClass(key,skin);ApplyActiveCharacter();SocialChanged();Notify("Clase: "+ClassName(key));
            },Roster.Active.ClassKey==key);}
            W.Label(panel,"Elegí una clase para tu personaje.",22,430,456,45,18,W.Muted);
        }
        void DrawCharacters()
        {
            var panel=Drawer("CharacterRoster","PERSONAJES",0,650,710);
            for(int i=0;i<Roster.Characters.Count;i++){var character=Roster.Characters[i];string id=character.Id;W.Button(panel,character.Name+" · "+ClassName(character.ClassKey),22,75+i*63,606,52,()=>{if(InGame){Notify("Volvé al menú para cambiar de personaje.");return;}Roster.Select(id);ApplyActiveCharacter();SocialChanged();},character.Id==Roster.Active.Id);}
            W.Button(panel,"+ CREAR PERSONAJE",22,620,606,60,()=>{createName="";homePanel="CreateCharacter";Navigate(HubPage.Home);});
        }
        void DrawCharacterCreation()
        {
            var panel=Drawer("CreateCharacter","CREAR PERSONAJE",0,650,510);
            W.Label(panel,"Nombre",22,76,600,30,20,W.Gold);W.Input(panel,"Nombre del personaje",22,114,606,v=>createName=v).text=createName;
            W.Label(panel,"Clase",22,177,600,30,20,W.Gold);var keys=ClassKeys;
            W.Dropdown(panel,"Clase",22,214,606,keys.Select(ClassName).ToArray(),System.Array.IndexOf(keys,createClass),id=>{createClass=keys[id];if(createClass=="warrior")createSkin=CombatSpecies.SkinIndices[0];});
            W.Label(panel,"Warrior usa Lizardman y un hacha. Podés crear varios personajes\nde esta clase; cada uno conserva su identidad local.",22,295,606,90,20,W.Muted);
            W.Button(panel,"CREAR Y SELECCIONAR",22,404,606,64,()=>{
                if(InGame){Notify("La creación está disponible desde el menú principal.");return;}
                int skin=createClass=="warrior"?CombatSpecies.SkinIndices[0]:createSkin;
                string error=Roster.Create(createName,createClass,skin);if(error!=null){Notify(error);return;}
                ApplyActiveCharacter();homePanel="Characters";SocialChanged();Notify("Personaje creado: "+Roster.Active.Name);
            },true);
        }
        void DrawWarriorEquipment(Transform body)
        {
            W.Label(body,"WARRIOR",0,0,650,40,30,W.Gold);
            W.Label(body,Roster.Active.Name+" · Lizardman\nHacha equipada · Furia\n\nClic: ataque básico\nF: Furia cortante\nZ: Rompetierra\nT: Rugido de honor",0,65,650,300,23);
            W.Button(body,"PERSONAJES",0,410,400,55,()=>{homePanel="Characters";Navigate(HubPage.Home);});
            ShowModel(body,CurrentCharacter(),750,0,1020,550);
        }
    }
}
