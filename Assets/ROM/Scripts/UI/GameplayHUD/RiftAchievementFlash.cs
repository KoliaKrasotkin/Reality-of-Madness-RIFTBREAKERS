using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI.Gameplay
{
    // A local UI mesh: no post-processing, world lights, input or progression changes.
    [RequireComponent(typeof(CanvasRenderer))]
    [DisallowMultipleComponent]
    public sealed class RiftAchievementFlash : MaskableGraphic
    {
        public RiftAchievementFlashSettings Settings;
        float started, progress = 1f;
        bool playing;
        public bool Playing => playing;
        public float Progress => progress;
        RiftAchievementFlashSettings Config => Settings ? Settings : RiftAchievementFlashSettings.Load;

        protected override void Awake()
        {
            base.Awake();
            raycastTarget = false;
            maskable = false;
        }

        public void Play()
        {
            if (!Config.Enabled) return;
            started = Time.unscaledTime;
            progress = 0f;
            playing = true;
            SetVerticesDirty();
        }

        public void Stop()
        {
            playing = false;
            progress = 1f;
            SetVerticesDirty();
        }

        void Update()
        {
            if (!playing) return;
            progress = Mathf.Clamp01((Time.unscaledTime - started) / Mathf.Max(.01f, Config.Duration));
            if (progress >= 1f || !Config.Enabled) playing = false;
            SetVerticesDirty();
        }

        // Deterministic visual inspection, without triggering a journal/unlock operation.
        public void SampleAt(float normalizedTime)
        {
            playing = false;
            progress = Mathf.Clamp01(normalizedTime);
            SetVerticesDirty();
        }

        protected override void OnDisable()
        {
            playing = false;
            progress = 1f;
            base.OnDisable();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var cfg = Config;
            if (!cfg.Enabled || progress <= 0f || progress >= 1f) return;
            const int columns = 48, bands = 7;
            float spread = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress / .68f), 3f);
            float width = Mathf.Lerp(50f, cfg.Width, spread);
            float height = cfg.Height * Mathf.Lerp(1f, .12f, progress);
            float envelope = Mathf.SmoothStep(0f, 1f, progress / .08f) * Mathf.Pow(1f - progress, 1.6f) * cfg.Opacity;
            for (int x = 0; x <= columns; x++)
            {
                float u = x * 2f / columns - 1f, distance = Mathf.Abs(u);
                float taper = Mathf.Pow(1f - distance, .72f);
                for (int y = 0; y < bands; y++)
                {
                    float v = y * 2f / (bands - 1) - 1f;
                    float vertical = Mathf.Pow(1f - Mathf.Abs(v), 2.8f);
                    var tint = Color.Lerp(cfg.Core, cfg.Tail, Mathf.Clamp01(distance * 1.05f + Mathf.Abs(v) * .4f + progress * .2f));
                    tint.a *= envelope * vertical * Mathf.Pow(1f - distance, .65f);
                    var vertex = UIVertex.simpleVert;
                    vertex.position = new Vector3(u * width * .5f, v * height * .5f * taper);
                    vertex.color = tint;
                    vh.AddVert(vertex);
                    if (x == 0 || y == 0) continue;
                    int i = x * bands + y;
                    vh.AddTriangle(i - bands - 1, i - bands, i);
                    vh.AddTriangle(i - bands - 1, i, i - 1);
                }
            }
        }
    }
}
