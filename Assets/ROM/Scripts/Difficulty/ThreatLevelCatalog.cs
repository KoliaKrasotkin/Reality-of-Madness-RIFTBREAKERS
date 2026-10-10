using UnityEngine;

namespace ROM.Difficulty
{
    /// <summary>
    /// Resuelve ThreatLevel -> ThreatLevelDefinition.
    ///
    /// Existe un único catálogo para todo el proyecto, así los niveles no se
    /// duplican mapa por mapa: una escena referencia el catálogo, no la lista de
    /// definiciones.
    /// </summary>
    [CreateAssetMenu(fileName = "threat_catalog", menuName = "ROM/Threat Level Catalog")]
    public class ThreatLevelCatalog : ScriptableObject
    {
        [SerializeField] ThreatLevelDefinition[] _definitions = new ThreatLevelDefinition[0];

        /// <summary>
        /// Devuelve la definición del nivel pedido, o null si el catálogo no la tiene.
        /// Quien llama decide qué hacer con el null: acá no inventamos un fallback,
        /// porque jugar con una dificultad distinta a la pedida es peor que fallar.
        /// </summary>
        public ThreatLevelDefinition Get(ThreatLevel level)
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (_definitions[i] && _definitions[i].Level == level)
                    return _definitions[i];
            }

            return null;
        }

        /// <summary>
        /// Avisa en el editor si el catálogo está mal armado, para que no se
        /// descubra en runtime y en red.
        /// </summary>
        void OnValidate()
        {
            for (int i = 0; i < _definitions.Length; i++)
            {
                if (!_definitions[i])
                {
                    Debug.LogWarning($"[ROM] {name}: la posición {i} está vacía.", this);
                    continue;
                }

                if (_definitions[i].Level == ThreatLevel.None)
                {
                    Debug.LogWarning($"[ROM] {name}: \"{_definitions[i].name}\" tiene Level = None, que no es un nivel jugable.", this);
                }

                for (int j = i + 1; j < _definitions.Length; j++)
                {
                    if (_definitions[j] && _definitions[j].Level == _definitions[i].Level)
                        Debug.LogWarning($"[ROM] {name}: hay dos definiciones para {_definitions[i].Level}. Se va a usar la primera.", this);
                }
            }
        }
    }
}
