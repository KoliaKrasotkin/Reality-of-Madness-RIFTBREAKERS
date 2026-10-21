using UnityEngine;
namespace ROM.UI.Gameplay
{
    [CreateAssetMenu(menuName="ROM/UI/Gameplay HUD Theme")]
    public sealed class RiftHUDTheme:ScriptableObject
    {
        public Font BodyFont,TitleFont;
        public Sprite[] AbilityIcons,Portraits;
        public Sprite White;
        public Material TargetMaterial;
        public Color Gold=new Color(.76f,.59f,.31f),BrightGold=new Color(.96f,.79f,.47f),Text=new Color(.96f,.89f,.69f);
        public int InitialLevel=1;
        [Range(1,10)]public float MinimapRefreshRate=4;
        [Range(128,512)]public int MinimapResolution=256;
        public float MinimapRange=64;
    }
}
