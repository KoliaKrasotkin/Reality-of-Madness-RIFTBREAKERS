using System;
using UnityEngine;

namespace MultiFPS.Gameplay
{
    // Per-model bindings preserve weapon overrides while replacing only this skin's clips.
    public sealed class CharacterAnimationSet : MonoBehaviour
    {
        [Serializable] public struct Binding { public AnimationClip Original, Replacement; }
        public Binding[] Bindings = Array.Empty<Binding>();
        public Binding[] PistolBindings = Array.Empty<Binding>();
        public Binding[] RifleBindings = Array.Empty<Binding>();
        // Imported actions are available to this model without inventing new gameplay states.
        public AnimationClip[] Library = Array.Empty<AnimationClip>();
        public bool HasOwnLocomotion;
        RuntimeAnimatorController source;
        AnimatorOverrideController instance;
        public RuntimeAnimatorController Resolve(RuntimeAnimatorController controller)
        {
            if (source == controller && instance) return instance;
            if (instance) Destroy(instance);
            source = controller;
            instance = new AnimatorOverrideController(controller);
            var clips = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
            instance.GetOverrides(clips);
            Apply(Bindings);
            string weapon = controller.name.ToLowerInvariant();
            if (weapon.Contains("pistol")) Apply(PistolBindings);
            else if (weapon.Contains("rifle") || weapon.Contains("shotgun") || weapon.Contains("lmg")) Apply(RifleBindings);
            instance.ApplyOverrides(clips);
            return instance;

            void Apply(Binding[] bindings)
            {
              for (int i = 0; i < clips.Count; i++)
                foreach (var binding in bindings)
                    if (binding.Original && binding.Replacement && (clips[i].Key == binding.Original || clips[i].Value == binding.Original))
                    { clips[i] = new System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>(clips[i].Key, binding.Replacement); break; }
            }
        }
        void OnDestroy() { if (instance) Destroy(instance); }
    }
}
