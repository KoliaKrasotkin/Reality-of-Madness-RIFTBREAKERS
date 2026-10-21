using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace ROM.UI.Sprint01
{
    [Serializable] public class LocalFriend { public string Id, Name; public int Skin; }
    [Serializable] public class LocalMessage { public string Id, From, To, Text, Utc; }
    [Serializable] public class LocalPartyMember { public string ProfileId; public bool Ready; }
    [Serializable] public class LocalInvite { public string From, To; }
    [Serializable] public class LocalSocialState
    {
        public int Version=1;
        public string ActiveId="local-player",LeaderId="local-player";
        public List<LocalFriend> Profiles=new List<LocalFriend>();
        public List<LocalMessage> Messages=new List<LocalMessage>();
        public List<LocalPartyMember> Party=new List<LocalPartyMember>();
        public List<LocalInvite> Invitations=new List<LocalInvite>();
    }
    public interface ISocialService
    {
        event Action Changed;
        LocalFriend Self { get; }
        IReadOnlyList<LocalFriend> Friends { get; }
        IReadOnlyList<LocalPartyMember> Party { get; }
        IReadOnlyList<LocalInvite> Invitations { get; }
        string LeaderId { get; }
        LocalFriend Find(string id);
        IReadOnlyList<LocalMessage> Conversation(string peerId);
        string Send(string peerId,string text);
        string AddFriend(string name);
        string Invite(string peerId);
        string AcceptInvite(string inviterId);
        string SetReady(bool ready);
        string RemoveMember(string profileId);
        void SetAppearance(int skin);
        void Rename(string name);
    }
    // Explicit single-device adapter. There are no fabricated online statuses or replies.
    // Messages are delivered between local profiles sharing this store, not over the internet.
    public sealed class LocalSocialService : ISocialService
    {
        public event Action Changed;
        readonly string path;
        LocalSocialState state;
        public LocalFriend Self=>state.Profiles.First(p=>p.Id==state.ActiveId);
        public IReadOnlyList<LocalFriend> Friends=>state.Profiles.Where(p=>p.Id!=state.ActiveId).ToArray();
        public IReadOnlyList<LocalFriend> Profiles=>state.Profiles;
        public IReadOnlyList<LocalPartyMember> Party=>state.Party;
        public IReadOnlyList<LocalInvite> Invitations=>state.Invitations.Where(i=>i.To==state.ActiveId).ToArray();
        public string LeaderId=>state.LeaderId;
        public LocalSocialService(string file=null)
        {
            path=file??Path.Combine(Application.persistentDataPath,"rom-local-social.json");
            if(File.Exists(path))try{state=JsonUtility.FromJson<LocalSocialState>(File.ReadAllText(path));}catch(Exception e){Debug.LogWarning("Local social data could not be read: "+e.Message);}
            if(state==null||state.Profiles==null||state.Profiles.Count==0)state=new LocalSocialState{Profiles=new List<LocalFriend>{new LocalFriend{Id="local-player",Name=string.IsNullOrWhiteSpace(MultiFPS.UserSettings.UserNickname)?"RiftWalker":MultiFPS.UserSettings.UserNickname}},Party=new List<LocalPartyMember>{new LocalPartyMember{ProfileId="local-player"}}};
            if(!state.Profiles.Any(p=>p.Id==state.ActiveId))state.ActiveId=state.Profiles[0].Id;
        }
        public LocalFriend Find(string id)=>state.Profiles.FirstOrDefault(p=>p.Id==id);
        public IReadOnlyList<LocalMessage> Conversation(string peerId)=>state.Messages.Where(m=>(m.From==state.ActiveId&&m.To==peerId)||(m.From==peerId&&m.To==state.ActiveId)||(peerId=="party"&&m.To=="party"&&state.Party.Any(p=>p.ProfileId==m.From))).OrderBy(m=>m.Utc).TakeLast(100).ToArray();
        public string Send(string peerId,string text)
        {
            text=(text??"").Trim();if(text.Length==0)return "Escribí un mensaje.";if(text.Length>500)return "El máximo es de 500 caracteres.";
            if(peerId!="party"&&!state.Profiles.Any(p=>p.Id==peerId))return "Elegí un contacto.";
            if(peerId=="party"&&!state.Party.Any(p=>p.ProfileId==state.ActiveId))return "No estás en esta party.";
            state.Messages.Add(new LocalMessage{Id=Guid.NewGuid().ToString("N"),From=state.ActiveId,To=peerId,Text=text,Utc=DateTime.UtcNow.ToString("O")});Save();return null;
        }
        public string AddFriend(string name)
        {
            name=(name??"").Trim();if(name.Length<2||name.Length>24)return "Usá un nombre de 2 a 24 caracteres.";
            if(state.Profiles.Any(p=>string.Equals(p.Name,name,StringComparison.OrdinalIgnoreCase)))return "Ese contacto ya existe.";
            state.Profiles.Add(new LocalFriend{Id=Guid.NewGuid().ToString("N"),Name=name});Save();return null;
        }
        public string Invite(string id)
        {
            if(state.LeaderId!=state.ActiveId)return "Solo el líder puede invitar.";
            if(!state.Profiles.Any(p=>p.Id==id)||id==state.ActiveId)return "Contacto inválido.";
            if(state.Party.Count>=4)return "La party ya tiene cuatro jugadores.";
            if(state.Party.Any(p=>p.ProfileId==id))return "Ya está en tu party.";
            if(state.Invitations.Any(i=>i.From==state.ActiveId&&i.To==id))return "Invitación pendiente.";
            state.Invitations.Add(new LocalInvite{From=state.ActiveId,To=id});Save();return null;
        }
        public string AcceptInvite(string inviter)
        {
            var invite=state.Invitations.FirstOrDefault(i=>i.To==state.ActiveId&&i.From==inviter);
            if(invite==null||state.LeaderId!=inviter)return "La invitación ya no está disponible.";
            if(state.Party.Count>=4)return "La party está completa.";
            if(!state.Party.Any(p=>p.ProfileId==state.ActiveId))state.Party.Add(new LocalPartyMember{ProfileId=state.ActiveId});state.Invitations.Remove(invite);Save();return null;
        }
        public string SetReady(bool value){var member=state.Party.FirstOrDefault(p=>p.ProfileId==state.ActiveId);if(member==null)return "No estás en esta party.";member.Ready=value;Save();return null;}
        public string RemoveMember(string id)
        {
            if(state.ActiveId!=state.LeaderId&&id!=state.ActiveId)return "Solo el líder puede quitar jugadores.";
            state.Party.RemoveAll(p=>p.ProfileId==id);if(state.Party.Count==0){state.Party.Add(new LocalPartyMember{ProfileId=state.ActiveId});state.LeaderId=state.ActiveId;}else if(id==state.LeaderId)state.LeaderId=state.Party[0].ProfileId;Save();return null;
        }
        public void SwitchLocalProfile(string id){if(!state.Profiles.Any(p=>p.Id==id))return;state.ActiveId=id;Save();}
        public void SetAppearance(int skin){Self.Skin=Mathf.Max(0,skin);Save();}
        public void Rename(string name){name=(name??"").Trim();if(name.Length<2||name.Length>24)return;Self.Name=name;MultiFPS.UserSettings.UserNickname=name;PlayerPrefs.SetString("_playerPrefs_Username",name);Save();}
        void Save(){Directory.CreateDirectory(Path.GetDirectoryName(path));string temp=path+".tmp";File.WriteAllText(temp,JsonUtility.ToJson(state,true));File.Copy(temp,path,true);File.Delete(temp);Changed?.Invoke();}
    }
}
