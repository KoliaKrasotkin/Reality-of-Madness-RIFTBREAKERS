using UnityEngine;

namespace ROM.Horde
{
    /// <summary>
    /// Arte y paleta del HUD de la Horda. Los sprites y fuentes salen de los assets
    /// de UI del proyecto (IDS ASSETS/UI NEW); el HUD se arma por código y toma
    /// todo de acá, así cambiar el look no requiere tocar HordeHUD.
    ///
    /// Todos los campos son opcionales: si falta un sprite, ese adorno no se dibuja;
    /// si falta una fuente, se usa la integrada de Unity.
    /// </summary>
    [CreateAssetMenu(fileName = "HordeHUDTheme", menuName = "ROM/Horda/HUD Theme")]
    public class HordeHUDTheme : ScriptableObject
    {
        [Header("Fuentes")]
        [Tooltip("Títulos y números grandes.")]
        public Font TitleFont;
        [Tooltip("Textos chicos y etiquetas.")]
        public Font BodyFont;

        [Header("Íconos")]
        public Sprite WaveBadge;
        public Sprite EnemyIcon;
        public Sprite LivesIcon;
        public Sprite ModeIcon;
        public Sprite Emblem;
        public Sprite DeathEmblem;
        public Sprite Logo;

        [Header("Adornos")]
        public Sprite AccentBar;
        public Sprite BannerBackground;
        public Sprite FrameCorners;

        [Header("Paleta")]
        public Color Accent = new Color(0.62f, 0.25f, 1f);
        public Color Danger = new Color(1f, 0.27f, 0.33f);
        public Color Good = new Color(0.55f, 1f, 0.65f);
        public Color Text = new Color(0.95f, 0.94f, 1f);
        public Color Muted = new Color(0.72f, 0.68f, 0.8f, 0.85f);
        public Color Shade = new Color(0.03f, 0.01f, 0.06f, 0.55f);
    }
}
