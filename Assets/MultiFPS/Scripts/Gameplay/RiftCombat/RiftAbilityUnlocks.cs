using System;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// One data-driven rule for spellbook, assignment and authoritative casts. No save migration.
public static class RiftAbilityUnlocks {
 [Serializable]public sealed class Entry {public string Id;public int Level=1;}
 [Serializable]public sealed class Configuration {public Entry[] Abilities;}
 static Configuration cached;
 static Configuration Load {get{if(cached==null){var json=Resources.Load<TextAsset>("RiftAbilityUnlocks");try{cached=json?JsonUtility.FromJson<Configuration>(json.text):new Configuration();}catch(Exception e){Debug.LogError("Ability unlock configuration: "+e.Message);cached=new Configuration();}}return cached;}}
 public static int Required(string id){if(Load.Abilities!=null)foreach(var e in Load.Abilities)if(e!=null&&e.Id==id)return Mathf.Max(1,e.Level);return 1;}
 public static bool Available(string id,int level)=>level>=Required(id);
}
}
