using System;
using System.Linq;
using UnityEngine;

namespace ROM.UI.Sprint01
{
    [Serializable] public class LobbyEmote
    {
        public string Name;
        public AnimationClip Clip;
        public float Seconds=6;
    }
    [Serializable] public class PreviewModel
    {
        public string Key,Name,Category;
        public GameObject Prefab;
        public int SkinIndex=-1;
        public bool ForSale;
        public int GoldPrice;
    }
    [CreateAssetMenu(menuName="ROM/Front End Catalog")]
    public sealed class FrontEndCatalog : ScriptableObject
    {
        public PreviewModel[] Models=Array.Empty<PreviewModel>();
        public GameObject StagePrefab;
        public LobbyEmote[] Emotes=Array.Empty<LobbyEmote>();
        public RuntimeAnimatorController EmoteController;
        public AnimationClip EmotePlaceholder;
        public PreviewModel Find(string key)=>Models.FirstOrDefault(m=>m.Key==key);
        public PreviewModel[] Characters=>Models.Where(m=>m.Category=="Personaje").ToArray();
    }
}
