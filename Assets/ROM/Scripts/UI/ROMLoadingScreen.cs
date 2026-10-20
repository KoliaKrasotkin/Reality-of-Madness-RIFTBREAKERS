using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ROM.UI
{
    /// <summary>
    /// Pantalla de carga de transición entre escenas. Es un prefab
    /// (Assets/ROM/UI/ROM_LoadingScreen.prefab) que está en el hub y al arrancar se
    /// vuelve persistente (DontDestroyOnLoad). Este script no crea UI: solo mueve el
    /// fundido, la barra y los textos del prefab.
    ///
    /// No es una escena intermedia porque Mirror carga la escena de red él mismo y
    /// sincroniza a los clientes con ese nombre; una escena en el medio rompería ese
    /// flujo. El overlay tapa el corte, muestra el progreso real de la carga y se va
    /// con un fundido cuando la escena nueva ya está lista.
    ///
    /// Se abre sola en cada carga de Mirror (NetworkManager.loadingSceneAsync),
    /// incluida la vuelta al menú, y el menú la abre apenas se aprieta "jugar".
    /// </summary>
    public class ROMLoadingScreen : MonoBehaviour
    {
        const float FadeInSeconds = 0.35f;
        const float FadeOutSeconds = 0.7f;
        const float MinVisibleSeconds = 1.6f;
        const float SettleSeconds = 0.6f;
        const float TimeoutSeconds = 15f;

        [SerializeField] CanvasGroup _group;
        [SerializeField] Text _title;
        [SerializeField] Text _subtitle;
        [SerializeField] Text _tip;
        [SerializeField] Text _percent;
        [Tooltip("Barra que se llena; su ancho en el prefab es el ancho lleno.")]
        [SerializeField] RectTransform _bar;
        [Tooltip("Opcional: gira mientras carga.")]
        [SerializeField] RectTransform _spinner;
        [SerializeField] float _spinnerSpeed = 220f;

        [SerializeField] string[] _tips =
        {
            "Las armas pesadas esperan en el centro de la plataforma.",
            "Los Desgarradores vienen a cuchillo: mantenelos a distancia.",
            "Reaparecer durante una oleada consume una reserva de la escuadra.",
            "Superar una oleada devuelve a los caídos y repone el arsenal.",
            "Los Colosos aparecen cada cinco oleadas. Guardá munición pesada.",
            "Al aparecer tenés unos segundos de protección: usalos para reposicionarte.",
            "Las anomalías cambian las reglas de una oleada. Leé el aviso.",
            "Caer de la plataforma es morir. Cuidado con el retroceso de los cohetes.",
        };

        public static ROMLoadingScreen Instance { private set; get; }

        /// <summary>
        /// Nombre que se muestra en la próxima carga. Lo setea el menú al elegir mapa.
        /// </summary>
        public static string NextDestination { set; get; }

        float _barWidth;
        bool _active;
        float _shownAt;
        float _sceneChangedAt = -1f;
        float _shownProgress;

        /// <summary>
        /// Abre la pantalla de carga. Se cierra sola cuando termina la próxima carga
        /// de escena, o por timeout si nunca llega (por ejemplo, si falla la conexión).
        /// </summary>
        public static void Show(string title, string subtitle = null)
        {
            if (Instance)
                Instance.Begin(title, subtitle);
        }

        void Awake()
        {
            //el hub se vuelve a cargar al salir de una partida: la copia nueva se descarta
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            var theme = ROMUITheme.Load();
            foreach(var text in GetComponentsInChildren<Text>(true))
            {text.font=theme.BodyFont;text.color=theme.Text;}
            if(_title){_title.font=theme.TitleFont;_title.fontSize=Mathf.Min(_title.fontSize,48);}
            foreach(var image in GetComponentsInChildren<Image>(true))
            {
                if(image.name=="Logo"&&theme.Logo)image.sprite=theme.Logo;
                if(image.name=="Bar"||image.name.Contains("Accent"))image.color=theme.Accent;
            }
            DontDestroyOnLoad(gameObject);

            _barWidth = _bar ? _bar.sizeDelta.x : 0f;
            SetVisible(0f);

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        void OnDestroy()
        {
            if (Instance != this)
                return;

            SceneManager.activeSceneChanged -= OnActiveSceneChanged;
            Instance = null;
        }

        void OnActiveSceneChanged(Scene previous, Scene next)
        {
            if (_active)
                _sceneChangedAt = Time.unscaledTime;
        }

        void Begin(string title, string subtitle)
        {
            if (!_active)
            {
                _shownAt = Time.unscaledTime;
                _shownProgress = 0f;

                if (_tip && _tips != null && _tips.Length > 0)
                    _tip.text = _tips[Random.Range(0, _tips.Length)];
            }

            _active = true;
            _sceneChangedAt = -1f;

            if (_title) _title.text = string.IsNullOrEmpty(title) ? "CARGANDO" : title.ToUpperInvariant();
            if (_subtitle) _subtitle.text = string.IsNullOrEmpty(subtitle) ? string.Empty : subtitle.ToUpperInvariant();
        }

        void Update()
        {
            AsyncOperation operation = NetworkManager.loadingSceneAsync;
            bool loading = operation != null && !operation.isDone;

            //carga disparada por Mirror sin pasar por el menú (cambio de mapa, vuelta al hub, cliente siguiendo al host)
            if (loading && !_active)
            {
                bool toMenu = NetworkManager.singleton && !NetworkManager.singleton.isNetworkActive;
                Begin(toMenu ? "Volviendo al cuartel" : "Cargando", toMenu ? null : NextDestination);
            }

            float now = Time.unscaledTime;

            if (_active)
            {
                float target = loading
                    ? Mathf.Clamp01(operation.progress / 0.9f)
                    : _sceneChangedAt >= 0f ? 1f : 0.15f + 0.35f * Mathf.Clamp01((now - _shownAt) / 4f); //esperando que arranque la carga

                _shownProgress = Mathf.MoveTowards(_shownProgress, Mathf.Max(_shownProgress, target), Time.unscaledDeltaTime * 1.5f);

                bool settled = !loading && _sceneChangedAt >= 0f && now - _sceneChangedAt >= SettleSeconds && now - _shownAt >= MinVisibleSeconds;
                bool timedOut = !loading && _sceneChangedAt < 0f && now - _shownAt >= TimeoutSeconds;

                if (settled || timedOut)
                    _active = false;
            }

            SetVisible(Mathf.MoveTowards(_group.alpha, _active ? 1f : 0f,
                Time.unscaledDeltaTime / (_active ? FadeInSeconds : FadeOutSeconds)));

            if (_group.alpha <= 0.001f)
                return;

            if (_bar)
            {
                Vector2 size = _bar.sizeDelta;
                size.x = _barWidth * _shownProgress;
                _bar.sizeDelta = size;
            }

            if (_percent)
                _percent.text = $"{Mathf.RoundToInt(_shownProgress * 100f)}%";

            if (_spinner)
                _spinner.Rotate(0f, 0f, -_spinnerSpeed * Time.unscaledDeltaTime);
        }

        void SetVisible(float alpha)
        {
            if (!_group)
                return;

            _group.alpha = alpha;
            _group.blocksRaycasts = alpha > 0.5f;
            _group.interactable = false;
        }
    }
}
