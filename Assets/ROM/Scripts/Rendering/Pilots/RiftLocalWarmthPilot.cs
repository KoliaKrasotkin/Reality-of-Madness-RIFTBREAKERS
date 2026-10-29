using System;
using System.Collections.Generic;
using UnityEngine;

namespace ROM.Rendering
{
    // Staged only. No automatic discovery, OnEnable mutation, ambient or material changes.
    [DisallowMultipleComponent]
    public sealed class RiftLocalWarmthPilot : MonoBehaviour
    {
        [Tooltip("Explicit lantern/torch lights verified in the bridge's runtime hierarchy.")]
        public Light[] Targets = Array.Empty<Light>();
        [Tooltip("Required world-space anchor for this bounded preview.")]
        public Transform ZoneOrigin;
        [Min(1)] public float Radius = 45f;
        public Color WarmColor = new Color(1f, .58f, .25f, 1f);
        [Range(1, 1.35f)] public float IntensityScale = 1.15f;
        [Min(0)] public float AdditionalIntensityCeiling = 12f;
        public bool Applied { get; private set; }
        public string Status { get; private set; } = "Not applied";

        private struct Snapshot
        {
            public Light Light;
            public Color Color;
            public float Intensity;
            public bool UseTemperature;
        }

        private readonly List<Snapshot> snapshots = new List<Snapshot>();

        public bool ApplyPreview()
        {
            if (!Application.isPlaying || !ZoneOrigin)
            {
                Status = "Requires Play and explicit zone anchor";
                return false;
            }
            if (Applied) return true;
            foreach (var light in Targets ?? Array.Empty<Light>())
            {
                if (!light || !light.enabled || !light.gameObject.activeInHierarchy ||
                    (light.type != LightType.Point && light.type != LightType.Spot) ||
                    Vector3.Distance(light.transform.position, ZoneOrigin.position) > Radius ||
                    snapshots.Exists(s => s.Light == light)) continue;
                snapshots.Add(new Snapshot { Light = light, Color = light.color,
                    Intensity = light.intensity, UseTemperature = light.useColorTemperature });
            }
            foreach (var saved in snapshots)
            {
                saved.Light.useColorTemperature = false;
                saved.Light.color = WarmColor;
                // Never brighten an already stronger source beyond its original intensity.
                saved.Light.intensity = Mathf.Min(saved.Intensity * Mathf.Clamp(IntensityScale, 1f, 1.35f),
                    Mathf.Max(saved.Intensity, AdditionalIntensityCeiling));
            }
            Applied = snapshots.Count > 0;
            Status = Applied ? "Explicit local warmth applied to " + snapshots.Count + " lights" :
                "No eligible explicitly assigned local lights; nothing changed";
            return Applied;
        }

        public void Restore()
        {
            foreach (var saved in snapshots)
            {
                if (!saved.Light) continue;
                saved.Light.color = saved.Color;
                saved.Light.intensity = saved.Intensity;
                saved.Light.useColorTemperature = saved.UseTemperature;
            }
            snapshots.Clear();
            Applied = false;
            Status = "Restored";
        }

        private void OnDisable() { Restore(); }
        private void OnDestroy() { Restore(); }
    }
}
