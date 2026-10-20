using UnityEngine;
using MultiFPS;
using MultiFPS.Gameplay;
using MultiFPS.UI;

namespace ROM.Gameplay
{
    /// <summary>
    /// Provisional on-screen readout of the incursion systems: the revive prompt,
    /// the revive progress, the bleed-out countdown and the squad revives left.
    ///
    /// This is deliberately a placeholder and it looks like one. It draws with the
    /// legacy GUI instead of the canvas of the kit, so there is nothing to build in
    /// the editor and nothing to wire: the systems become playable and testable
    /// without waiting for art direction. The real HUD belongs on the canvas of the
    /// kit, following the shape of UIRespawnCooldown and the rest, and is separate
    /// work.
    ///
    /// It reads state and never changes it, so deleting this file cannot break
    /// anything but the display.
    ///
    /// Self-installing: nothing to attach.
    /// </summary>
    public class IncursionHudProvisional : MonoBehaviour
    {
        const int Width = 460;

        CharacterInstance _character;
        DownedState _downed;
        ReviveInteraction _revive;

        GUIStyle _big;
        GUIStyle _normal;
        GUIStyle _small;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            GameObject go = new GameObject("[ROM] IncursionHudProvisional");
            go.AddComponent<IncursionHudProvisional>();
            Object.DontDestroyOnLoad(go);
        }

        void OnGUI()
        {
            //the observed character, not the owned one, so it keeps working while
            //spectating a teammate
            CharacterInstance observed = ClientFrontend.ObservedCharacterInstance;

            if (observed == null) return;

            if (observed != _character)
            {
                _character = observed;
                _downed = observed.GetComponent<DownedState>();
                _revive = observed.GetComponent<ReviveInteraction>();
            }

            if (_downed == null) return;

            BuildStyles();

            if (_downed.IsOut)
                DrawOut();
            else if (_downed.IsDowned)
                DrawDowned();
            else
                DrawStanding();

            DrawBudget();
        }

        void DrawOut()
        {
            Rect area = CenteredArea(90);

            GUI.Label(area, "FUERA DE LA INCURSIÓN", _big);
            GUI.Label(new Rect(area.x, area.y + 44, area.width, 30),
                      "Te desangraste. Solo la escuadra puede terminar la incursión.", _small);
        }

        void DrawDowned()
        {
            Rect area = CenteredArea(120);

            GUI.Label(area, "CAÍDO", _big);

            GUI.Label(new Rect(area.x, area.y + 44, area.width, 24),
                      $"Te desangrás en {Mathf.CeilToInt(_downed.BleedOutRemaining)} s", _normal);

            //how much of the bleed-out window is left, so the bar emptying is the
            //thing the player is actually watching
            float left = _downed.BleedOutDuration > 0f
                ? _downed.BleedOutRemaining / _downed.BleedOutDuration
                : 0f;

            DrawBar(new Rect(area.x, area.y + 72, area.width, 14), left, new Color(0.75f, 0.15f, 0.15f));

            if (!_downed.IsBeingRevived) return;

            GUI.Label(new Rect(area.x, area.y + 92, area.width, 24), "Te están levantando", _normal);
            DrawBar(new Rect(area.x, area.y + 118, area.width, 14), _downed.ReviveProgress, new Color(0.2f, 0.7f, 0.3f));
        }

        void DrawStanding()
        {
            if (_revive == null) return;

            DownedState candidate = _revive.Candidate;

            if (candidate == null) return;

            Rect area = CenteredArea(60);

            Health candidateHealth = candidate.GetComponent<Health>();
            string name = candidateHealth != null ? candidateHealth.CharacterName : "un compañero";

            if (candidate.IsBeingRevived)
            {
                GUI.Label(area, $"Levantando a {name}", _normal);
                DrawBar(new Rect(area.x, area.y + 26, area.width, 14), candidate.ReviveProgress, new Color(0.2f, 0.7f, 0.3f));
                return;
            }

            GUI.Label(area, $"Mantené  E  para levantar a {name}", _normal);
        }

        void DrawBudget()
        {
            ReviveBudget budget = ReviveBudget.Instance;

            if (budget == null) return;

            string text = budget.IsUnlimited
                ? "Revividas: sin límite"
                : $"Revividas de la escuadra: {budget.Remaining}";

            GUI.Label(new Rect(16, 16, 320, 24), text, _normal);
        }

        #region drawing helpers
        void BuildStyles()
        {
            if (_big != null) return;

            _big = new GUIStyle(GUI.skin.label);
            _big.fontSize = 34;
            _big.alignment = TextAnchor.MiddleCenter;
            _big.fontStyle = FontStyle.Bold;

            _normal = new GUIStyle(GUI.skin.label);
            _normal.fontSize = 18;
            _normal.alignment = TextAnchor.MiddleCenter;

            _small = new GUIStyle(GUI.skin.label);
            _small.fontSize = 14;
            _small.alignment = TextAnchor.MiddleCenter;
        }

        static Rect CenteredArea(int height)
        {
            return new Rect((Screen.width - Width) * 0.5f, Screen.height * 0.62f, Width, height);
        }

        static void DrawBar(Rect rect, float fill, Color color)
        {
            fill = Mathf.Clamp01(fill);

            Color previous = GUI.color;

            GUI.color = new Color(0f, 0f, 0f, 0.5f);
            GUI.DrawTexture(rect, Texture2D.whiteTexture);

            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width * fill, rect.height), Texture2D.whiteTexture);

            GUI.color = previous;
        }
        #endregion
    }
}
