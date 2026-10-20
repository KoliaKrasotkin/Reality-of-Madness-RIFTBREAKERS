using MultiFPS;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.UI
{
    /// <summary>
    /// Menú principal con la identidad de ROM. Es un prefab
    /// (Assets/ROM/UI/ROM_MainMenu.prefab) que está en el hub; este script no crea
    /// UI, solo conecta los controles del prefab con los del kit.
    ///
    /// No reemplaza la lógica del hub: cada control nuevo escribe en el control
    /// original correspondiente (los Dropdown, InputField, Toggle y Button del
    /// RoomCreator, del RoomConnector y del panel de usuario), así que todo lo que el
    /// kit hace al cambiar de mapa, crear la sala o conectarse sigue pasando igual.
    /// La UI vieja sigue en la escena con sus Canvas desactivados.
    /// </summary>
    public class ROMMainMenu : MonoBehaviour
    {
        [Tooltip("Para los retratos de cada apariencia.")]
        [SerializeField] ROMUITheme _theme;

        [Header("Kit (UI vieja del hub)")]
        [SerializeField] RoomCreator _creator;
        [SerializeField] RoomConnector _connector;
        [SerializeField] InputField _kitUsername;
        [SerializeField] Dropdown _kitSkin;

        [Header("Operador")]
        [SerializeField] InputField _username;
        [SerializeField] ROMSelector _skin;
        [SerializeField] Image _portrait;
        [SerializeField] Image _portraitFallback;

        [Header("Crear partida")]
        [SerializeField] ROMSelector _map;
        [SerializeField] Text _mapIndex;
        [SerializeField] ROMSelector _mode;
        [SerializeField] Text _modeDescription;
        [SerializeField] ROMSelector _players;
        [SerializeField] ROMSelector _duration;
        [SerializeField] Toggle _bots;
        [SerializeField] InputField _hostPort;
        [SerializeField] Button _host;
        [SerializeField] Button _server;

        [Header("Unirse")]
        [SerializeField] InputField _address;
        [SerializeField] InputField _connectPort;
        [SerializeField] Button _connect;

        void Start()
        {
            if (!_creator)
            {
                Debug.LogError("[ROM] El menú no tiene RoomCreator asignado; corré ROM > UI > Construir e instalar la UI.", this);
                return;
            }

            BindOperator();
            BindCreate();
            BindJoin();

            //el kit llena sus dropdowns en su Start; leemos un frame después para verlos completos
            Invoke(nameof(Refresh), 0f);
        }

        #region conexión con el kit
        void BindOperator()
        {
            if (_kitUsername && _username)
            {
                _username.text = _kitUsername.text;
                _username.onEndEdit.AddListener(value =>
                {
                    _kitUsername.text = value;
                    _kitUsername.onEndEdit.Invoke(value); //el kit valida y guarda en PlayerPrefs
                    _username.text = _kitUsername.text;
                });
            }

            if (_skin) _skin.Stepped += step => Step(_kitSkin, step);
            if (_kitSkin) _kitSkin.onValueChanged.AddListener(_ => Refresh());
        }

        void BindCreate()
        {
            if (_map) _map.Stepped += step => Step(_creator.MapselectionDropdown, step);
            if (_mode) _mode.Stepped += step => Step(_creator.GamemodeSelectionDropdown, step);
            if (_players) _players.Stepped += step => Step(_creator.PlayerNumberDropdown, step);
            if (_duration) _duration.Stepped += step => Step(_creator.GameDurationDropdown, step);

            //al cambiar de mapa el kit vuelve internamente a la primera opción de modo y de jugadores,
            //pero sus dropdowns quedan mostrando la selección anterior; los alineamos y volvemos a leer
            _creator.MapselectionDropdown.onValueChanged.AddListener(_ =>
            {
                _creator.GamemodeSelectionDropdown.SetValueWithoutNotify(0);
                _creator.PlayerNumberDropdown.SetValueWithoutNotify(0);
                Refresh();
            });

            if (_bots && _creator.BotsToggle)
            {
                _bots.isOn = _creator.BotsToggle.isOn;
                _bots.onValueChanged.AddListener(on => _creator.BotsToggle.isOn = on);
            }

            BindInput(_hostPort, _creator.PortIF);

            if (_host) _host.onClick.AddListener(() =>
            {
                if (!_creator.HostGameButton) return;
                ROMLoadingScreen.Show("Desplegando", SelectedMapName());
                _creator.HostGameButton.onClick.Invoke();
            });

            if (_server) _server.onClick.AddListener(() =>
            {
                if (!_creator.ServeGameButton) return;
                ROMLoadingScreen.Show("Levantando servidor", SelectedMapName());
                _creator.ServeGameButton.onClick.Invoke();
            });
        }

        void BindJoin()
        {
            if (!_connector)
                return;

            BindInput(_address, _connector.Address);
            BindInput(_connectPort, _connector.Port);

            if (_connect) _connect.onClick.AddListener(() =>
            {
                if (!_connector.ConnectBtn) return;
                ROMLoadingScreen.Show("Conectando", _connector.Address.text);
                _connector.ConnectBtn.onClick.Invoke();
            });
        }

        static void BindInput(InputField ours, InputField kit)
        {
            if (!ours || !kit)
                return;

            ours.text = kit.text;
            ours.onValueChanged.AddListener(value => kit.text = value); //el kit escucha onValueChanged de su campo
        }

        void Step(Dropdown dropdown, int step)
        {
            if (!dropdown || dropdown.options.Count == 0)
                return;

            int count = dropdown.options.Count;
            dropdown.value = (dropdown.value + step + count) % count; //dispara los listeners del kit
            dropdown.RefreshShownValue();

            Refresh();
        }
        #endregion

        #region lectura
        void Refresh()
        {
            if (!_creator)
                return;

            int map = _creator.MapselectionDropdown.value;
            int mapCount = _creator.MapselectionDropdown.options.Count;

            if (_map) _map.Value = SelectedMapName().ToUpperInvariant();
            if (_mapIndex) _mapIndex.text = mapCount > 0 ? $"{map + 1} / {mapCount}" : string.Empty;

            string mode = OptionText(_creator.GamemodeSelectionDropdown);
            if (_mode) _mode.Value = ModeName(mode).ToUpperInvariant();
            if (_modeDescription) _modeDescription.text = ModeDescription(mode);

            if (_players) _players.Value = OptionText(_creator.PlayerNumberDropdown).Replace("players", "jugadores").ToUpperInvariant();
            if (_duration) _duration.Value = OptionText(_creator.GameDurationDropdown).Replace("minutes", "min").ToUpperInvariant();

            if (_kitSkin && _skin)
            {
                string skin = OptionText(_kitSkin);
                _skin.Value = skin.ToUpperInvariant();

                Sprite portrait = _theme ? _theme.FindPortrait(skin) : null;
                if (_portrait)
                {
                    _portrait.sprite = portrait;
                    _portrait.enabled = portrait;
                }
                if (_portraitFallback) _portraitFallback.enabled = !portrait;
            }
        }

        string SelectedMapName()
        {
            string name = OptionText(_creator.MapselectionDropdown);
            ROMLoadingScreen.NextDestination = name;
            return name;
        }

        bool SelectedMapIsHorde()
        {
            int map = _creator.MapselectionDropdown.value;
            return _creator.Maps != null && map < _creator.Maps.Length && _creator.Maps[map]
                && _creator.Maps[map].Scene != null && _creator.Maps[map].Scene.Contains("horde");
        }

        string ModeName(string kitName)
        {
            switch (kitName)
            {
                case "Deathmatch": return "Todos contra todos";
                case "TeamDeathmatch": return "Duelo por equipos";
                case "TeamEliminations": return "Eliminación por equipos";
                case "Defuse": return "Desactivación";
                case "Raid": return SelectedMapIsHorde() ? "Horda" : "Incursión";
                case "None":
                case "": return "Práctica libre";
                default: return kitName;
            }
        }

        string ModeDescription(string kitName)
        {
            switch (kitName)
            {
                case "Deathmatch": return "Cada uno por su cuenta. Gana el primero en llegar al límite de bajas.";
                case "TeamDeathmatch": return "Dos equipos, bajas ilimitadas. Gana el equipo con más bajas al final.";
                case "TeamEliminations": return "Rondas sin reaparición. Gana el equipo que elimine al otro.";
                case "Defuse": return "Un equipo planta la bomba, el otro la desactiva.";
                case "Raid":
                    return SelectedMapIsHorde()
                        ? "Cooperativo. Oleadas infinitas de la grieta con dificultad creciente."
                        : "Cooperativo. Avanzá por el mapa, limpiá oleadas y destruí el objetivo.";
                default: return "Sin reglas: recorré el mapa y probá las armas.";
            }
        }

        static string OptionText(Dropdown dropdown)
        {
            if (!dropdown || dropdown.options.Count == 0)
                return string.Empty;

            return dropdown.options[Mathf.Clamp(dropdown.value, 0, dropdown.options.Count - 1)].text;
        }
        #endregion
    }
}
