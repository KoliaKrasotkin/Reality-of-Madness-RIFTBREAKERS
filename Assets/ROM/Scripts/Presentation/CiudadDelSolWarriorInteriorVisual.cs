using System.Collections.Generic;
using System.Linq;
using System.Text;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.Gameplay.RiftCombat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ROM.Presentation
{
    // Client-side presentation only. No network component, combat state or shared asset writes.
    [DisallowMultipleComponent]
    public sealed class CiudadDelSolWarriorInteriorVisual : MonoBehaviour
    {
        const string City = "Assets/Scenes/Dungeon_Isla_del_Sol.unity";
        [SerializeField] Bounds footprint;
        [SerializeField] Bounds[] upperEntries;
        [SerializeField] Bounds reserved;
        [SerializeField] float fadeDistance = 2;
        readonly List<Binding> bindings = new List<Binding>();
        readonly List<Material> sharedScratch = new List<Material>(16);
        float nextTick, nextScan;
        bool previewOriginal;
        sealed class Binding
        {
            public CharacterInstance actor;
            public Renderer renderer;
            public Material[] originals, applied;
            public float factor = -1;
        }
        public int ActiveRenderers => bindings.Count;
        public void Configure(Bounds area, Bounds[] entries, Bounds protectedArea)
        { footprint = area; upperEntries = entries; reserved = protectedArea; }
        static float Weight(Bounds bounds, Vector3 point, float fade)
        {
            float d = Vector3.Distance(point, bounds.ClosestPoint(point));
            return 1 - Mathf.SmoothStep(0, 1, Mathf.Clamp01(d / Mathf.Max(.1f, fade)));
        }
        public float EvaluateAt(Vector3 point)
        {
            float depth = 1 - Mathf.SmoothStep(0, 1, Mathf.InverseLerp(-10, -3, point.y));
            float inside = Weight(footprint, point, fadeDistance) * depth;
            if (upperEntries != null) foreach (var entry in upperEntries)
                inside = Mathf.Max(inside, Weight(entry, point, fadeDistance));
            return inside * (1 - Weight(reserved, point, fadeDistance));
        }
        public void PreviewOriginal(bool value)
        { previewOriginal = value; if (value) RestoreAll(); nextTick = nextScan = 0; }
        void Update()
        {
            if (Time.unscaledTime < nextTick) return;
            nextTick = Time.unscaledTime + .1f;
            if (previewOriginal || SceneManager.GetActiveScene().path != City ||
                SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            { if (bindings.Count > 0) RestoreAll(); return; }
            for (int i = bindings.Count - 1; i >= 0; i--)
            {
                var b = bindings[i];
                float factor = b.actor ? EvaluateAt(b.actor.transform.position) : 0;
                bool matched = b.renderer;
                if (matched)
                {
                    b.renderer.GetSharedMaterials(sharedScratch); matched = sharedScratch.Count == b.applied.Length;
                    for (int j = 0; matched && j < sharedScratch.Count; j++) matched = sharedScratch[j] == b.applied[j];
                }
                if (!b.renderer || !b.actor || factor < .001f || !matched)
                { Restore(b); bindings.RemoveAt(i); continue; }
                if (Mathf.Abs(b.factor - factor) > .005f) Apply(b, factor);
            }
            if (Time.unscaledTime < nextScan) return;
            nextScan = Time.unscaledTime + .75f;
            foreach (var player in GameManager.Players)
            {
                var actor = player ? player.MyCharacter : null;
                if (!actor || !actor.GetComponent<WarriorAvatar>()) continue;
                float factor = EvaluateAt(actor.transform.position); if (factor < .001f) continue;
                foreach (var renderer in actor.GetComponentsInChildren<Renderer>(true))
                {
                    if (!(renderer is SkinnedMeshRenderer) && !(renderer is MeshRenderer)) continue;
                    if (!renderer.enabled || !renderer.gameObject.activeInHierarchy || bindings.Any(b => b.renderer == renderer)) continue;
                    var originals = renderer.sharedMaterials; var applied = new Material[originals.Length]; bool changed = false;
                    for (int i = 0; i < originals.Length; i++)
                    {
                        var source = originals[i]; applied[i] = source;
                        if (!source || (!source.HasProperty("_BaseColor") && !source.HasProperty("_Color"))) continue;
                        // Do not recolor text, trails, particles or unsupported effects.
                        if (!source.shader || !source.shader.name.StartsWith("Universal Render Pipeline/")) continue;
                        if (source.shader.name.Contains("Unlit")) continue;
                        applied[i] = new Material(source) { name = source.name + " [Ciudad del Sol runtime]", hideFlags = HideFlags.DontSave };
                        changed = true;
                    }
                    if (!changed) continue;
                    var binding = new Binding { actor = actor, renderer = renderer, originals = originals, applied = applied };
                    Apply(binding, factor); renderer.sharedMaterials = applied; bindings.Add(binding);
                }
            }
        }
        static void Apply(Binding b, float factor)
        {
            for (int i = 0; i < b.originals.Length; i++)
            {
                var source = b.originals[i]; var mat = b.applied[i]; if (!source || mat == source) continue;
                foreach (var property in new[] { "_BaseColor", "_Color" }) if (source.HasProperty(property))
                {
                    var color = source.GetColor(property);
                    var interior = new Color(color.r * .75f, color.g * .72f, color.b * .96f, color.a);
                    mat.SetColor(property, Color.Lerp(color, interior, factor));
                }
                if (source.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", Mathf.Lerp(source.GetFloat("_Smoothness"), Mathf.Min(.30f, source.GetFloat("_Smoothness") * .60f), factor));
                if (source.HasProperty("_Metallic")) mat.SetFloat("_Metallic", source.GetFloat("_Metallic") * Mathf.Lerp(1, .8f, factor));
                foreach (var property in new[] { "_SpecColor", "_EmissionColor" }) if (source.HasProperty(property))
                {
                    var color = source.GetColor(property); var toned = color * (property == "_SpecColor" ? .55f : .65f); toned.a = color.a;
                    if (property == "_SpecColor" && source.shader.name.EndsWith("Simple Lit")) toned.a = Mathf.Min(.30f, color.a * .60f);
                    mat.SetColor(property, Color.Lerp(color, toned, factor));
                }
            }
            b.factor = factor;
        }
        static void Restore(Binding b)
        {
            if (b.renderer)
            {
                var current = b.renderer.sharedMaterials; bool changed = false;
                for (int i = 0; i < current.Length && i < b.applied.Length; i++)
                    if (b.applied[i] != b.originals[i] && current[i] == b.applied[i]) { current[i] = b.originals[i]; changed = true; }
                if (changed) b.renderer.sharedMaterials = current;
            }
            for (int i = 0; i < b.applied.Length; i++) if (b.applied[i] && b.applied[i] != b.originals[i]) Destroy(b.applied[i]);
        }
        void RestoreAll() { foreach (var b in bindings) Restore(b); bindings.Clear(); }
        void OnDisable() { RestoreAll(); }
        void OnDestroy() { RestoreAll(); }
        public string Describe()
        {
            var text = new StringBuilder("Runtime renderers=" + bindings.Count + "; original preview=" + previewOriginal + "\n");
            foreach (var b in bindings) foreach (var m in b.originals.Where(m => m).Distinct())
                text.AppendLine(m.name + "|shader=" + m.shader.name + "|base=" + (m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor").ToString() : "none") + "|spec=" + (m.HasProperty("_SpecColor") ? m.GetColor("_SpecColor").ToString() : "none") + "|emission=" + (m.HasProperty("_EmissionColor") ? m.GetColor("_EmissionColor").ToString() : "none"));
            return text.ToString();
        }
    }
}
