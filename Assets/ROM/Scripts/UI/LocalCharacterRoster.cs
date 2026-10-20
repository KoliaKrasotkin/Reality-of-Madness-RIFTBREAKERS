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
            if(data==null||data.Characters==null||data.Characters.Count==0){data=new CharacterRosterData();var first=new RiftPlayerCharacter{Id=Guid.NewGuid().ToString("N"),Name=initialName,Skin=skin,ClassKey=classKey};data.Characters.Add(first);data.ActiveId=first.Id;Save();}
            if(Active==null){data.ActiveId=data.Characters[0].Id;Save();}
        }
        public string Create(string name,string classKey,int skin)
        {
            name=(name??"").Trim();
            if(name.Length<2||name.Length>24)return "Usá un nombre de 2 a 24 caracteres.";
            if(data.Characters.Count>=8)return "Ya tenés ocho personajes.";
            if(data.Characters.Any(c=>string.Equals(c.Name,name,StringComparison.OrdinalIgnoreCase)))return "Ya existe un personaje con ese nombre.";
            if(string.IsNullOrEmpty(classKey)||skin<0)return "Elegí una clase y un aspecto válidos.";
            var character=new RiftPlayerCharacter{Id=Guid.NewGuid().ToString("N"),Name=name,ClassKey=classKey,Skin=skin};data.Characters.Add(character);data.ActiveId=character.Id;Save();return null;
        }
        public bool Select(string id){if(!data.Characters.Any(c=>c.Id==id))return false;data.ActiveId=id;Save();return true;}
        public void SetClass(string id,int skin){Active.ClassKey=id;Active.Skin=skin;Save();}
        public void SetSkin(int skin){Active.Skin=skin;Save();}
        void Save(){PlayerPrefs.SetString(key,JsonUtility.ToJson(data));PlayerPrefs.Save();}
    }
}
