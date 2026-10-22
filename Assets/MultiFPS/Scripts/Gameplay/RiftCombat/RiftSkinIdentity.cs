using System;
using UnityEngine;
namespace MultiFPS.Gameplay.RiftCombat {
// Presentation data only. CharacterId, not this name, owns progress.
public static class RiftSkinIdentity {
 [Serializable] public sealed class Entry {public int Skin;public string Name;}
 [Serializable] sealed class Catalog {public Entry[] Skins;}
 static Catalog catalog;
 public static string Name(int skin){if(catalog==null){var data=Resources.Load<TextAsset>("RiftSkinNames");catalog=data?JsonUtility.FromJson<Catalog>(data.text):new Catalog();}if(catalog.Skins!=null)foreach(var entry in catalog.Skins)if(entry.Skin==skin)return entry.Name;return "Aspecto no disponible";}
 public static bool Playable(int skin)=>skin==1;
}
}
