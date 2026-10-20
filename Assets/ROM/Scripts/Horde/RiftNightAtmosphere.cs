using UnityEngine;
using UnityEngine.Rendering;

namespace ROM.Horde
{
    public sealed class RiftNightAtmosphere : MonoBehaviour
    {
        [Header("Noche legible")]
        public Light Moon;
        [Range(0, 2)] public float MoonIntensity = .8f;
        [Range(0, 2)] public float SkyExposure = .42f;
        [Range(0, 2)] public float BaseExposure = .65f;
        public Color MoonColor = new Color(.66f, .76f, 1);
        public Color SkyAmbient = new Color(.42f, .48f, .60f);
        public Color HorizonAmbient = new Color(.32f, .37f, .47f);
        public Color GroundAmbient = new Color(.16f, .19f, .26f);
        [Header("Bruma de distancia + bancos de humo del grupo hijo")]
        public Color FogColor = new Color(.095f, .115f, .17f);
        [Range(.001f, .009f)] public float FogDensity = .0038f;
        Material runtimeSky;

        void OnEnable() => Apply();

        [ContextMenu("Aplicar atmÃ³sfera")]
        public void Apply()
        {
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = SkyAmbient;
            RenderSettings.ambientEquatorColor = HorizonAmbient;
            RenderSettings.ambientGroundColor = GroundAmbient;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogDensity = FogDensity;
            if (Moon)
            {
                Moon.color = MoonColor;
                Moon.intensity = MoonIntensity;
                Moon.shadows = LightShadows.Soft;
                RenderSettings.sun = Moon;
            }
            if (RenderSettings.skybox)
            {
                if (Application.isPlaying && !runtimeSky)
                {
                    runtimeSky = new Material(RenderSettings.skybox);
                    RenderSettings.skybox = runtimeSky;
                }
                RenderSettings.skybox.SetFloat("_Exposure", SkyExposure);
            }
        }

        void OnDestroy()
        {
            if (runtimeSky) Destroy(runtimeSky);
        }
    }
}
