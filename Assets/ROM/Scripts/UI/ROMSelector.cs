using System;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI
{
    /// <summary>
    /// Selector de una opción con flechas a los costados (◀ valor ▶), como el de
    /// los menús de consola. Solo avisa el paso (-1 / +1); quien lo usa decide qué
    /// valor mostrar.
    /// </summary>
    public class ROMSelector : MonoBehaviour
    {
        [SerializeField] Text _value;
        [SerializeField] Button _previous;
        [SerializeField] Button _next;

        public event Action<int> Stepped;

        public string Value
        {
            get => _value ? _value.text : string.Empty;
            set { if (_value) _value.text = value; }
        }

        void Awake()
        {
            if (_previous) _previous.onClick.AddListener(() => Stepped?.Invoke(-1));
            if (_next) _next.onClick.AddListener(() => Stepped?.Invoke(+1));
        }
    }
}
