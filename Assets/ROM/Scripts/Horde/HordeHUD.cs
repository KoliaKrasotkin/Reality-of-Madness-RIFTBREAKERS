using System.Collections.Generic;
using System.Text;
using Mirror;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI;
using UnityEngine;
using UnityEngine.UI;

namespace ROM.Horde
{
    /// <summary>
    /// HUD del modo Horda. Es un prefab (Assets/ROM/UI/HordeHUD.prefab) que vive en
    /// la escena del mapa; este script no crea nada, solo actualiza textos, barras y
    /// transparencias de los objetos que tiene referenciados. Para cambiar el diseño
    /// se edita el prefab.
    ///
    /// Tres capas, cada una con su CanvasGroup:
    /// - Intro: presentación de partida (modo, mapa, objetivo, escuadra, cuenta
    ///   regresiva a la primera oleada).
    /// - Status: widget de la esquina (oleada, enemigos, reservas, anomalía).
    /// - Banner y avisos del jugador (inicio/fin de oleada, protección, sin reservas).
    ///
    /// Arranca invisible y se enciende cuando el gamemode Horde lo vincula en el cliente.
    /// </summary>
    public class HordeHUD : MonoBehaviour
    {
        const float IntroFade = 0.6f;
        const float LateJoinIntroSeconds = 6f;
        const float BannerDuration = 3.6f;

        [Tooltip("Colores que el HUD usa al cambiar de estado (peligro, bien, etc.).")]
        [SerializeField] HordeHUDTheme _theme;

        [Header("Intro")]
        [SerializeField] CanvasGroup _intro;
        [SerializeField] Text _introMapName;
        [SerializeField] Text _introCountdownLabel;
        [SerializeField] Text _introCountdown;
        [Tooltip("Barra que se llena; su ancho en el prefab es el ancho lleno.")]
        [SerializeField] RectTransform _introBar;
        [SerializeField] Text _introSquad;

        [Header("Estado")]
        [SerializeField] CanvasGroup _status;
        [SerializeField] Text _waveNumber;
        [SerializeField] Text _statusLabel;
        [SerializeField] Text _statusValue;
        [SerializeField] Image _statusIcon;
        [SerializeField] RectTransform _statusBar;
        [SerializeField] Text _lives;
        [SerializeField] Text _modifier;

        [Header("Avisos")]
        [SerializeField] CanvasGroup _banner;
        [SerializeField] RectTransform _bannerContent;
        [SerializeField] Image _bannerAccent;
        [SerializeField] Image _bannerEmblem;
        [SerializeField] Text _bannerTitle;
        [SerializeField] Text _bannerSubtitle;

        [Header("Jugador")]
        [SerializeField] CanvasGroup _protection;
        [SerializeField] RectTransform _protectionBar;
        [SerializeField] Text _outOfLives;

        Horde _horde;

        //medidas tomadas del prefab, así un ajuste en el editor no rompe las animaciones
        float _introBarWidth;
        float _statusBarWidth;
        float _protectionBarWidth;
        float _bannerAccentWidth;
        Vector2 _bannerRestPosition;
        float _statusValueX;
        float _statusValueXWithIcon;

        float _introAlpha;
        float _introForcedUntil;
        float _nextSquadRefresh;
        double _intermissionStart;
        HordeState _lastState;
        float _bannerStart = -100f;
        float _protectionStart;
        float _protectionEnd;

        readonly StringBuilder _builder = new StringBuilder();

        void Awake()
        {
            _introBarWidth = _introBar ? _introBar.sizeDelta.x : 0f;
            _statusBarWidth = _statusBar ? _statusBar.sizeDelta.x : 0f;
            _protectionBarWidth = _protectionBar ? _protectionBar.sizeDelta.x : 0f;
            _bannerAccentWidth = _bannerAccent ? _bannerAccent.rectTransform.sizeDelta.x : 0f;
            _bannerRestPosition = _bannerContent ? _bannerContent.anchoredPosition : Vector2.zero;

            if (_statusValue)
            {
                _statusValueXWithIcon = _statusValue.rectTransform.anchoredPosition.x;
                _statusValueX = _statusIcon ? _statusIcon.rectTransform.anchoredPosition.x : _statusValueXWithIcon;
            }

            //invisible hasta que el gamemode lo vincule
            SetAlpha(_intro, 0f);
            SetAlpha(_status, 0f);
            SetAlpha(_banner, 0f);
            SetAlpha(_protection, 0f);
            if (_outOfLives) _outOfLives.text = string.Empty;
        }

        /// <summary>
        /// Lo llama Horde en el cliente al arrancar.
        /// </summary>
        public void Bind(Horde horde)
        {
            if (_horde)
            {
                _horde.OnBanner -= ShowBanner;
                _horde.OnSpawnProtection -= ShowSpawnProtection;
            }

            _horde = horde;
            _horde.OnBanner += ShowBanner;
            _horde.OnSpawnProtection += ShowSpawnProtection;

            if (_introMapName)
                _introMapName.text = horde.MapDisplayName.ToUpperInvariant();

            _introForcedUntil = Time.time + LateJoinIntroSeconds;
            _introAlpha = 1f;
        }

        void OnDestroy()
        {
            if (_horde)
            {
                _horde.OnBanner -= ShowBanner;
                _horde.OnSpawnProtection -= ShowSpawnProtection;
            }
        }

        #region actualización
        void Update()
        {
            if (!_horde)
                return;

            HordeSnapshot s = _horde.Snapshot;
            float countdown = Mathf.Max(0f, (float)(s.CountdownEnd - NetworkTime.time));

            if (s.State != _lastState)
            {
                if (s.State == HordeState.Intermission)
                    _intermissionStart = NetworkTime.time;

                _lastState = s.State;
            }

            UpdateIntro(s, countdown);
            UpdateStatus(s, countdown);
            UpdateBanner();
            UpdatePlayerNotices(s);
        }

        void UpdateIntro(HordeSnapshot s, float countdown)
        {
            bool preGame = s.State == HordeState.WaitingForPlayers || (s.State == HordeState.Intermission && s.Wave == 0);
            bool visible = preGame || Time.time < _introForcedUntil;

            _introAlpha = Mathf.MoveTowards(_introAlpha, visible ? 1f : 0f, Time.deltaTime / IntroFade);
            SetAlpha(_intro, _introAlpha);

            if (_introAlpha <= 0.001f)
                return;

            if (s.State == HordeState.WaitingForPlayers)
            {
                _introCountdownLabel.text = "ESPERANDO A LA ESCUADRA";
                _introCountdown.text = "· · ·";
                SetBar(_introBar, _introBarWidth, 0f);
            }
            else if (preGame)
            {
                _introCountdownLabel.text = "LA PRIMERA OLEADA COMIENZA EN";
                _introCountdown.text = FormatTime(countdown);
                SetBar(_introBar, _introBarWidth, IntermissionProgress(s));
            }
            else
            {
                _introCountdownLabel.text = "TE UNISTE A UNA PARTIDA EN CURSO";
                _introCountdown.text = s.Wave > 0 ? $"OLEADA {s.Wave}" : string.Empty;
                SetBar(_introBar, _introBarWidth, 1f - Mathf.Clamp01((_introForcedUntil - Time.time) / LateJoinIntroSeconds));
            }

            if (_introSquad && Time.time >= _nextSquadRefresh)
            {
                _nextSquadRefresh = Time.time + 0.5f;
                _introSquad.text = SquadNames();
            }
        }

        void UpdateStatus(HordeSnapshot s, float countdown)
        {
            float target = s.State == HordeState.WaitingForPlayers ? 0f : 1f - _introAlpha;
            SetAlpha(_status, Mathf.MoveTowards(_status.alpha, target, Time.deltaTime * 3f));

            _waveNumber.text = s.Wave > 0 ? s.Wave.ToString() : "–";

            bool showIcon = false;
            bool showBar = false;

            switch (s.State)
            {
                case HordeState.Wave:
                    _statusLabel.text = s.BossWave ? "COLOSO EN LA PLATAFORMA" : "ENEMIGOS RESTANTES";
                    _statusLabel.color = s.BossWave ? _theme.Danger : _theme.Muted;
                    _statusValue.text = s.EnemiesRemaining.ToString();
                    showIcon = true;
                    break;

                case HordeState.Intermission:
                    _statusLabel.text = "PRÓXIMA OLEADA";
                    _statusLabel.color = _theme.Muted;
                    _statusValue.text = FormatTime(countdown);
                    showBar = true;
                    SetBar(_statusBar, _statusBarWidth, IntermissionProgress(s));
                    break;

                case HordeState.GameOver:
                    _statusLabel.text = "DERROTA";
                    _statusLabel.color = _theme.Danger;
                    _statusValue.text = $"REINICIO {FormatTime(countdown)}";
                    break;

                default:
                    _statusLabel.text = string.Empty;
                    _statusValue.text = string.Empty;
                    break;
            }

            if (_statusIcon) _statusIcon.enabled = showIcon;
            if (_statusBar) _statusBar.gameObject.SetActive(showBar);

            //sin ícono el número ocupa su lugar
            Vector2 valuePos = _statusValue.rectTransform.anchoredPosition;
            valuePos.x = showIcon ? _statusValueXWithIcon : _statusValueX;
            _statusValue.rectTransform.anchoredPosition = valuePos;

            _lives.color = s.Lives > 0 ? _theme.Good : _theme.Danger;
            _lives.text = $"{s.Lives}  {(s.Lives == 1 ? "RESERVA" : "RESERVAS")}";

            _modifier.text = s.State == HordeState.Wave && s.Modifier != HordeModifier.None
                ? HordeDifficulty.DisplayName(s.Modifier)
                : string.Empty;
        }

        void UpdateBanner()
        {
            float t = Time.time - _bannerStart;

            if (t > BannerDuration)
            {
                SetAlpha(_banner, 0f);
                return;
            }

            float fadeIn = Mathf.Clamp01(t / 0.25f);
            float fadeOut = Mathf.Clamp01((BannerDuration - t) / 0.5f);
            SetAlpha(_banner, Mathf.Min(fadeIn, fadeOut));

            //entra deslizándose desde arriba y la línea de acento se abre desde el centro
            float ease = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.45f), 3f);
            _bannerContent.anchoredPosition = _bannerRestPosition + new Vector2(0, (1f - ease) * 30f);

            Vector2 size = _bannerAccent.rectTransform.sizeDelta;
            size.x = _bannerAccentWidth * ease;
            _bannerAccent.rectTransform.sizeDelta = size;
        }

        void UpdatePlayerNotices(HordeSnapshot s)
        {
            CharacterInstance local = ClientFrontend.OwnedCharacterInstance;

            float remaining = _protectionEnd - Time.time;
            bool protectedNow = remaining > 0f && local && local.Health.CurrentHealth > 0;

            SetAlpha(_protection, Mathf.MoveTowards(_protection.alpha, protectedNow ? 1f : 0f, Time.deltaTime * 5f));
            if (protectedNow)
                SetBar(_protectionBar, _protectionBarWidth, remaining / Mathf.Max(0.01f, _protectionEnd - _protectionStart));

            _outOfLives.text = LocalPlayerIsOutOfLives(s)
                ? "SIN RESERVAS  ·  AGUANTÁ HASTA QUE TU ESCUADRA SUPERE LA OLEADA"
                : string.Empty;
        }
        #endregion

        #region eventos
        void ShowBanner(HordeBanner banner, int wave, HordeModifier modifier, int bosses)
        {
            _bannerStart = Time.time;
            _bannerEmblem.enabled = false;
            _bannerTitle.color = _theme.Text;
            _bannerSubtitle.color = _theme.Muted;
            _bannerAccent.color = _theme.Accent;

            switch (banner)
            {
                case HordeBanner.WaveStart:
                    _bannerTitle.text = $"OLEADA {wave}";

                    if (bosses > 0)
                    {
                        _bannerEmblem.enabled = true;
                        _bannerSubtitle.color = _theme.Danger;
                        _bannerSubtitle.text = bosses > 1 ? $"{bosses} COLOSOS SE ACERCAN" : "UN COLOSO SE ACERCA";
                        _bannerAccent.color = _theme.Danger;
                    }
                    else if (modifier != HordeModifier.None)
                    {
                        _bannerSubtitle.color = _theme.Accent;
                        _bannerSubtitle.text = $"ANOMALÍA · {HordeDifficulty.DisplayName(modifier)}  —  {HordeDifficulty.Description(modifier).ToUpperInvariant()}";
                    }
                    else
                    {
                        _bannerSubtitle.text = "LA GRIETA SE ABRE";
                    }
                    break;

                case HordeBanner.WaveCleared:
                    _bannerTitle.text = $"OLEADA {wave} SUPERADA";
                    _bannerSubtitle.color = _theme.Good;
                    _bannerSubtitle.text = "+1 RESERVA  ·  LOS CAÍDOS REGRESAN  ·  ARSENAL REPUESTO";
                    _bannerAccent.color = _theme.Good;
                    break;

                case HordeBanner.GameOver:
                    _bannerEmblem.enabled = true;
                    _bannerTitle.color = _theme.Danger;
                    _bannerTitle.text = "LA GRIETA LOS CONSUMIÓ";
                    _bannerSubtitle.text = $"SOBREVIVIERON {wave} {(wave == 1 ? "OLEADA" : "OLEADAS")}";
                    _bannerAccent.color = _theme.Danger;
                    break;

                case HordeBanner.RunRestart:
                    _bannerTitle.text = "LA GRIETA VUELVE A ABRIRSE";
                    _bannerSubtitle.text = "REAGRUPENSE EN EL ARSENAL";
                    break;

                default:
                    _bannerStart = -100f;
                    break;
            }
        }

        void ShowSpawnProtection(CharacterInstance character, float seconds)
        {
            if (character != ClientFrontend.OwnedCharacterInstance && !(character && character.isOwned))
                return;

            _protectionStart = Time.time;
            _protectionEnd = Time.time + seconds;
        }
        #endregion

        #region helpers
        float IntermissionProgress(HordeSnapshot s)
        {
            double total = s.CountdownEnd - _intermissionStart;
            if (total <= 0.01)
                return 0f;

            return Mathf.Clamp01((float)((NetworkTime.time - _intermissionStart) / total));
        }

        string SquadNames()
        {
            _builder.Clear();

            List<PlayerInstance> players = GameManager.Players;
            for (int i = 0; i < players.Count; i++)
            {
                PlayerInstance player = players[i];

                //en cliente PlayerInstance.BOT no se sincroniza; los bots de la horda están en el otro equipo
                if (!player || player.Team != Horde.PlayerTeam)
                    continue;

                string name = string.IsNullOrEmpty(player.PlayerInfo.Username) ? "JUGADOR" : player.PlayerInfo.Username;
                _builder.AppendLine(name.ToUpperInvariant());
            }

            return _builder.ToString();
        }

        static string FormatTime(float seconds)
        {
            int total = Mathf.CeilToInt(seconds);
            return $"{total / 60}:{total % 60:00}";
        }

        static bool LocalPlayerIsOutOfLives(HordeSnapshot s)
        {
            if (s.State != HordeState.Wave || s.Lives > 0)
                return false;

            PlayerInstance local = ClientFrontend.ClientPlayerInstance;
            return local && (!local.MyCharacter || local.MyCharacter.Health.CurrentHealth <= 0);
        }

        static void SetBar(RectTransform bar, float fullWidth, float fill)
        {
            if (!bar)
                return;

            Vector2 size = bar.sizeDelta;
            size.x = fullWidth * Mathf.Clamp01(fill);
            bar.sizeDelta = size;
        }

        static void SetAlpha(CanvasGroup group, float alpha)
        {
            if (!group)
                return;

            group.alpha = alpha;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        #endregion
    }
}
