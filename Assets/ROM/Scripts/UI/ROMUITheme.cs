using System;
using UnityEngine;

namespace ROM.UI
{
    /// <summary>
    /// Arte y paleta de la UI general de ROM (menú principal, pantalla de carga y
    /// retoque de la UI del kit). Vive en Resources para que se pueda cargar sin
    /// referencias de escena: el menú y la pantalla de carga se instalan solos.
    /// </summary>
    [CreateAssetMenu(fileName = "ROMUITheme", menuName = "ROM/UI Theme")]
    public class ROMUITheme : ScriptableObject
    {
        public const string ResourcePath = "ROMUITheme";

        [Serializable]
        public struct Portrait
        {
            [Tooltip("Texto a buscar (sin distinguir mayúsculas) en el nombre de la apariencia.")]
            public string Keyword;
            public Sprite Sprite;
        }

        [Header("Fuentes")]
        public Font TitleFont;
        public Font BodyFont;

        [Header("Fondos")]
        public Sprite MenuBackground;
        public Sprite LoadingBackground;
        public Sprite Shade;

        [Header("Marca")]
        public Sprite Logo;
        public Sprite StudioLogo;
        public Sprite Emblem;

        [Header("Adornos")]
        public Sprite AccentBar;
        public Sprite FrameCorners;
        public Sprite ArrowLeft;
        public Sprite ArrowRight;
        public Sprite Spinner;

        [Header("Retratos de personaje")]
        public Portrait[] Portraits;

        [Header("Paleta")]
        public Color Accent = new Color(0.62f, 0.25f, 1f);
        public Color AccentDark = new Color(0.3f, 0.1f, 0.5f);
        public Color Panel = new Color(0.04f, 0.02f, 0.08f, 0.82f);
        public Color Field = new Color(1f, 1f, 1f, 0.06f);
        public Color Text = new Color(0.95f, 0.94f, 1f);
        public Color Muted = new Color(0.7f, 0.66f, 0.8f);
        public Color Danger = new Color(1f, 0.27f, 0.33f);

        static ROMUITheme _cached;

        /// <summary>
        /// Tema de Resources, o uno vacío con la paleta por defecto si no existe.
        /// </summary>
        public static ROMUITheme Load()
        {
            if (_cached)
                return _cached;

            _cached = Resources.Load<ROMUITheme>(ResourcePath);

            if (!_cached)
            {
                Debug.LogWarning($"[ROM] No se encontró Resources/{ResourcePath}; la UI usa la paleta por defecto sin arte.");
                _cached = CreateInstance<ROMUITheme>();
            }

            return _cached;
        }

        public Font Title => TitleFont ? TitleFont : Builtin;
        public Font Body => BodyFont ? BodyFont : Builtin;

        static Font Builtin => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        public Sprite FindPortrait(string skinName)
        {
            if (Portraits == null || string.IsNullOrEmpty(skinName))
                return null;

            foreach (Portrait portrait in Portraits)
            {
                if (!string.IsNullOrEmpty(portrait.Keyword) &&
                    skinName.IndexOf(portrait.Keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return portrait.Sprite;
            }

            return null;
        }
    }
}
