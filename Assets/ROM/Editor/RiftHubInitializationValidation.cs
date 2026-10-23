using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using ROM.UI.Sprint01;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class RiftHubInitializationValidation
{
    [MenuItem("ROM/Validation/Validate Hub Initialization")]
    public static void Validate()
    {
        const BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
        var report=new List<string>();
        void Check(bool ok,string message){if(!ok)throw new Exception(message);report.Add("PASS "+message);}
        var strings=new Dictionary<string,(bool,string)>();
        var integers=new Dictionary<string,(bool,int)>();
        void SnapshotString(string key)=>strings[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetString(key));
        void SnapshotInt(string key)=>integers[key]=(PlayerPrefs.HasKey(key),PlayerPrefs.GetInt(key));
        foreach(var key in new[]{"ROM.Character.ActiveId","_playerPrefs_Username"})SnapshotString(key);
        foreach(var key in new[]{AbilityController.Preference,PlayerInstance.ClassPreference,"_playerPrefs_CharacterSkinID"})SnapshotInt(key);
        string previousName=UserSettings.UserNickname;
        int previousSkin=UserSettings.CharacterSkinID;
        string realAccount=new LocalSocialService().Self.Id;
        string realRosterKey="ROM.CharacterRoster.v1."+realAccount;
        SnapshotString(realRosterKey);
        string testAccount="hub-validation-"+Guid.NewGuid().ToString("N");
        string testRosterKey="ROM.CharacterRoster.v1."+testAccount;
        SnapshotString(testRosterKey);
        var root=new GameObject("Hub initialization regression",typeof(RectTransform),typeof(Canvas));
        try
        {
            var hub=root.AddComponent<RiftHub>();
            var rosterProperty=typeof(RiftHub).GetProperty("Roster",flags);
            var cold=(LocalCharacterRoster)rosterProperty.GetValue(hub);
            Check(hub.Social!=null&&cold.Active!=null,"Roster initializes services before Start");
            string activeId=cold.Active.Id;
            hub.Build();
            Check(root.GetComponentsInChildren<Transform>(true).Any(t=>t.name=="Selected skin identity"),"ModernHome renders the selected character");
            typeof(RiftHub).GetField("<Social>k__BackingField",flags).SetValue(hub,null);
            typeof(RiftHub).GetField("<Services>k__BackingField",flags).SetValue(hub,null);
            typeof(RiftHub).GetField("roster",flags).SetValue(hub,null);
            hub.Navigate(HubPage.Home);
            var reloaded=(LocalCharacterRoster)rosterProperty.GetValue(hub);
            Check(hub.Social!=null&&hub.Services!=null&&reloaded.Active.Id==activeId,"Lost service references recover without replacing the active character");
            hub.Initialize();
            Check(((LocalCharacterRoster)rosterProperty.GetValue(hub)).Active.Id==activeId,"Repeated initialization preserves character identity");
            if(strings[realRosterKey].Item1)Check(PlayerPrefs.GetString(realRosterKey)==strings[realRosterKey].Item2,"Existing real roster data stays unchanged");

            // Isolated Warrior account avoids migration of the user's wallet during Deploy.
            var profile=new FixtureSocial(testAccount);
            PlayerPrefs.SetString(testRosterKey,JsonUtility.ToJson(new CharacterRosterData{ActiveId="fixture-warrior",Characters=new List<RiftPlayerCharacter>{new RiftPlayerCharacter{Id="fixture-warrior",Name="Fixture",ClassKey="warrior",Skin=1,WalletVersion=1}}}));
            hub.UseSocialService(profile);
            var creatorObject=new GameObject("Fixture room creator");creatorObject.transform.SetParent(root.transform,false);
            var creator=creatorObject.AddComponent<RoomCreator>();
            var map=AssetDatabase.FindAssets("t:MapRepresenter").Select(g=>AssetDatabase.LoadAssetAtPath<MapRepresenter>(AssetDatabase.GUIDToAssetPath(g))).FirstOrDefault(m=>m&&m.Scene!=null&&m.Scene.Contains("map_rom_horde"));
            Check(map!=null,"El Rift map remains configured");
            creator.Maps=new[]{map};
            Dropdown Dropdown(string name,string[] options){var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(root.transform,false);var dropdown=go.AddComponent<Dropdown>();dropdown.AddOptions(options.ToList());return dropdown;}
            creator.MapselectionDropdown=Dropdown("Map",new[]{map.Name});
            creator.GamemodeSelectionDropdown=Dropdown("Mode",new[]{"Raid"});
            creator.PlayerNumberDropdown=Dropdown("Players",map.MaxPlayersPresets.Select(n=>n.ToString()).ToArray());
            var bots=new GameObject("Bots",typeof(RectTransform));bots.transform.SetParent(creatorObject.transform,false);
            creator.BotsToggle=bots.AddComponent<Toggle>();
            var host=new GameObject("Host",typeof(RectTransform));host.transform.SetParent(creatorObject.transform,false);
            creator.HostGameButton=host.AddComponent<Button>();
            hub.Creator=creator;
            hub.Deploy();
            Check(root.GetComponentsInChildren<Text>(true).Any(t=>t.text=="Entrar en El Rift"),"Deploy passes RequireWarrior and opens the map entry confirmation");
            Debug.Log(string.Join("\n",report));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(root);
            foreach(var item in strings){if(item.Value.Item1)PlayerPrefs.SetString(item.Key,item.Value.Item2);else PlayerPrefs.DeleteKey(item.Key);}
            foreach(var item in integers){if(item.Value.Item1)PlayerPrefs.SetInt(item.Key,item.Value.Item2);else PlayerPrefs.DeleteKey(item.Key);}
            UserSettings.UserNickname=previousName;UserSettings.CharacterSkinID=previousSkin;
            PlayerPrefs.Save();
            File.WriteAllText("Documentation/warrior-rebuild/hub-initialization-validation.txt",string.Join("\n",report));
        }
    }
    private sealed class FixtureSocial:ISocialService
    {
        public event Action Changed{add{}remove{}}
        public LocalFriend Self{get;}
        public string LeaderId=>Self.Id;
        public IReadOnlyList<LocalFriend> Friends=>Array.Empty<LocalFriend>();
        public IReadOnlyList<LocalPartyMember> Party=>new[]{new LocalPartyMember{ProfileId=Self.Id}};
        public IReadOnlyList<LocalInvite> Invitations=>Array.Empty<LocalInvite>();
        public FixtureSocial(string id){Self=new LocalFriend{Id=id,Name="Fixture",Skin=1};}
        public LocalFriend Find(string id)=>id==Self.Id?Self:null;
        public IReadOnlyList<LocalMessage> Conversation(string peerId)=>Array.Empty<LocalMessage>();
        public string Send(string peerId,string text)=>null;
        public string AddFriend(string name)=>null;
        public string Invite(string peerId)=>null;
        public string AcceptInvite(string inviterId)=>null;
        public string SetReady(bool ready)=>null;
        public string RemoveMember(string profileId)=>null;
        public void SetAppearance(int skin){Self.Skin=skin;}
        public void Rename(string name){Self.Name=name;}
    }
}
