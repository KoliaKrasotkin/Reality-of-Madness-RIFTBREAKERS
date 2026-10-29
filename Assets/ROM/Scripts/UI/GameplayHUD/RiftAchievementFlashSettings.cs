using UnityEngine;

namespace ROM.UI.Gameplay
{
    [CreateAssetMenu(menuName = "ROM/UI/Achievement flash", fileName = "RiftAchievementFlash")]
    public sealed class RiftAchievementFlashSettings : ScriptableObject
    {
        public bool Enabled = true;
        [Range(.2f, 1.2f)] public float Duration = .58f;
        [Range(0f, .5f)] public float StartDelay = .18f;
        [Range(240f, 1400f)] public float Width = 1020f;
        [Range(4f, 48f)] public float Height = 22f;
        [Range(.1f, 1f)] public float Opacity = .92f;
        public Color Core = new Color(1f, .98f, 1f, 1f);
        public Color Tail = new Color(.68f, .4f, 1f, .65f);
        static RiftAchievementFlashSettings fallback;

        public static RiftAchievementFlashSettings Load
        {
            get
            {
                var asset = Resources.Load<RiftAchievementFlashSettings>("RiftAchievementFlash");
                if (asset) return asset;
                if (!fallback)
                {
                    fallback = CreateInstance<RiftAchievementFlashSettings>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }
                return fallback;
            }
        }
    }
}
