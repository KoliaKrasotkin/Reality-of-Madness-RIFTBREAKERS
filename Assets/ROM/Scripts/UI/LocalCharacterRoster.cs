using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace ROM.UI.Sprint01
{
    [Serializable] public sealed class RiftPlayerCharacter
    {
        public string Id,Name,ClassKey="warrior";
        public int Skin=1;
        public int WalletVersion;
        public bool CreliumCompleted;public string CreliumResume="";
        public string DisplayName=>MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Name(Skin);
        public bool Available=>ClassKey=="warrior"&&MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Playable(Skin);
    }
    [Serializable] public sealed class CharacterRosterData
    {
        public int Version=1;
        public string ActiveId;
        public List<RiftPlayerCharacter> Characters=new List<RiftPlayerCharacter>();
    }
    // Character identities are separate from social/account profiles and specializations.
    public sealed class LocalCharacterRoster
    {
        readonly string key;
        CharacterRosterData data;
        public IReadOnlyList<RiftPlayerCharacter> Characters=>data.Characters;
        public RiftPlayerCharacter Active=>data.Characters.Find(c=>c.Id==data.ActiveId);
        public LocalCharacterRoster(string account,string initialName,int skin,string classKey)
        {
            key="ROM.CharacterRoster.v1."+account;
            try{data=JsonUtility.FromJson<CharacterRosterData>(PlayerPrefs.GetString(key,""));}catch{data=null;}
            if(data==null||data.Characters==null){data=new CharacterRosterData();var first=new RiftPlayerCharacter{Id=Guid.NewGuid().ToString("N"),Name=initialName,Skin=skin,ClassKey=classKey};data.Characters.Add(first);data.ActiveId=first.Id;Save();}
            if(Active==null&&data.Characters.Count>0){data.ActiveId=data.Characters[0].Id;Save();}
        }
        public string Create(string name,string classKey,int skin)
        {
            name=MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Name(skin);
            if(classKey!="warrior"||!MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Playable(skin))return "Solo Warrior está disponible.";
            if(name.Length<2||name.Length>24)return "Usá un nombre de 2 a 24 caracteres.";
            if(data.Characters.Count>=8)return "Ya tenés ocho personajes.";
            if(string.IsNullOrEmpty(classKey)||skin<0)return "Elegí una clase y un aspecto válidos.";
            var character=new RiftPlayerCharacter{Id=Guid.NewGuid().ToString("N"),Name=name,ClassKey=classKey,Skin=skin,WalletVersion=1};data.Characters.Add(character);data.ActiveId=character.Id;Save();return null;
        }
        public string LastDeletedId=>PlayerPrefs.GetString(key+".LastDeleted","");
        public bool DeleteRecoverable(string id){
            var record=data.Characters.Find(c=>c.Id==id);if(record==null)return false;
            string before=JsonUtility.ToJson(data);
            // Save a recovery copy before removal; inventory/journal/avatar keys are retained.
            try{PlayerPrefs.SetString(key+".Recovery."+id,before);PlayerPrefs.Save();}catch{return false;}
            data.Characters.Remove(record);if(data.ActiveId==id)data.ActiveId=data.Characters.FirstOrDefault(c=>c.Available)?.Id??data.Characters.FirstOrDefault()?.Id??"";
            try{Save();PlayerPrefs.SetString(key+".LastDeleted",id);PlayerPrefs.Save();return true;}catch{data=JsonUtility.FromJson<CharacterRosterData>(before);return false;}
        }
        public bool RestoreDeleted(string id){
            if(string.IsNullOrEmpty(id)||data.Characters.Count>=8||data.Characters.Any(c=>c.Id==id))return false;
            CharacterRosterData previous;try{previous=JsonUtility.FromJson<CharacterRosterData>(PlayerPrefs.GetString(key+".Recovery."+id,""));}catch{return false;}
            var record=previous?.Characters?.Find(c=>c.Id==id);if(record==null)return false;var before=JsonUtility.ToJson(data);data.Characters.Add(record);data.ActiveId=id;
            try{Save();return true;}catch{data=JsonUtility.FromJson<CharacterRosterData>(before);return false;}
        }
        public bool Select(string id){if(!data.Characters.Any(c=>c.Id==id&&c.Available))return false;data.ActiveId=id;Save();return true;}
        public void SetClass(string id,int skin){/* Existing identities cannot be converted into another class. */}
        public void SetSkin(int skin){if(!Active.Available||!MultiFPS.Gameplay.RiftCombat.RiftSkinIdentity.Playable(skin))return;Active.Skin=skin;Save();}
        public void SaveIdentity(){Save();}
        public bool SaveCrelium(string id,string resume,bool completed){var c=data.Characters.Find(x=>x.Id==id);if(c==null||c.CreliumCompleted&&!completed)return false;var old=c.CreliumResume;bool was=c.CreliumCompleted;c.CreliumCompleted=completed;c.CreliumResume=completed?"":resume??"";try{Save();return true;}catch{c.CreliumResume=old;c.CreliumCompleted=was;throw;}}
        void Save(){PlayerPrefs.SetString(key,JsonUtility.ToJson(data));PlayerPrefs.Save();}
    }
}
