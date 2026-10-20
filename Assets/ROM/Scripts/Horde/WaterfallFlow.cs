using UnityEngine;

namespace ROM.Horde
{
    /// <summary>Scrolls the waterfall texture without instantiating materials per frame.</summary>
    public sealed class WaterfallFlow : MonoBehaviour
    {
        public Renderer[] Curtains;
        public float Speed = .45f;
        MaterialPropertyBlock _block;
        static readonly int MapST = Shader.PropertyToID("_BaseMap_ST");

        void Awake() { _block = new MaterialPropertyBlock(); }

        void Update()
        {
            if (Curtains == null) return;
            foreach (Renderer curtain in Curtains)
            {
                if (!curtain) continue;
                curtain.GetPropertyBlock(_block);
                _block.SetVector(MapST, new Vector4(2, 4, 0, -Time.time * Speed));
                curtain.SetPropertyBlock(_block);
            }
        }
    }
}
