using UnityEngine;

namespace ROM.Horde
{
    /// <summary>
    /// Portal de la grieta por donde entra la horda. Agrupa sus puntos de aparición
    /// y maneja la parte visual: apagado cuando está cerrado, brillando cuando está
    /// activo y con un pulso cada vez que escupe un enemigo.
    ///
    /// No es un objeto de red: el gamemode le avisa por RPC qué portales están
    /// abiertos y cuándo pulsar, así que la lógica de acá es puramente local.
    /// </summary>
    public class HordeRiftPortal : MonoBehaviour
    {
        [SerializeField] Transform[] _spawnPoints;

        [Header("Visual")]
        [SerializeField] Renderer[] _glowRenderers;
        [SerializeField] Light _light;
        [SerializeField] Transform _spinningPart;
        [SerializeField] Color _activeColor = new Color(0.75f, 0.2f, 1f);
        [SerializeField] Color _inactiveColor = new Color(0.08f, 0.03f, 0.12f);
        [SerializeField] float _activeEmission = 4f;
        [SerializeField] float _pulseEmission = 14f;
        [SerializeField] float _spinSpeed = 35f;
        [SerializeField] float _lightIntensity = 6f;

        public Transform[] SpawnPoints => _spawnPoints;
        public bool IsOpen { private set; get; }

        MaterialPropertyBlock _block;
        float _pulse;
        float _openBlend;

        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            _block = new MaterialPropertyBlock();
            Apply();
        }

        public void SetOpen(bool open)
        {
            IsOpen = open;
        }

        public void Pulse()
        {
            _pulse = 1f;
        }

        public Transform GetRandomSpawnPoint()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0)
                return transform;

            return _spawnPoints[Random.Range(0, _spawnPoints.Length)];
        }

        void Update()
        {
            _openBlend = Mathf.MoveTowards(_openBlend, IsOpen ? 1f : 0f, Time.deltaTime * 1.5f);
            _pulse = Mathf.MoveTowards(_pulse, 0f, Time.deltaTime * 2.5f);

            if (_spinningPart)
                _spinningPart.Rotate(0f, _spinSpeed * (0.2f + _openBlend + _pulse * 3f) * Time.deltaTime, 0f, Space.Self);

            Apply();
        }

        void Apply()
        {
            Color color = Color.Lerp(_inactiveColor, _activeColor, _openBlend);
            float emission = Mathf.Lerp(0.2f, _activeEmission, _openBlend) + _pulse * _pulseEmission;

            if (_glowRenderers != null)
            {
                for (int i = 0; i < _glowRenderers.Length; i++)
                {
                    Renderer r = _glowRenderers[i];
                    if (!r) continue;

                    r.GetPropertyBlock(_block);
                    _block.SetColor(BaseColorId, color);
                    _block.SetColor(EmissionColorId, color * emission);
                    r.SetPropertyBlock(_block);
                }
            }

            if (_light)
            {
                _light.color = color;
                _light.intensity = _lightIntensity * (_openBlend + _pulse * 2f);
                _light.enabled = _light.intensity > 0.01f;
            }
        }
    }
}
