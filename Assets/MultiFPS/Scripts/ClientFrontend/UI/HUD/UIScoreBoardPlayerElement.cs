using MultiFPS.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace MultiFPS.UI.HUD
{
    public class UIScoreBoardPlayerElement : MonoBehaviour
    {
        [SerializeField] Text _playerName;
        [SerializeField] Text _kills;
        [SerializeField] Text _deaths;
        [SerializeField] Text _assists;
        [SerializeField] Text _latency;
        [SerializeField] Image _background;
        [SerializeField] Color _localPlayerColor = Color.yellow;
        [SerializeField] Color _rowColor = new Color(.025f,.024f,.034f,.94f);
        [SerializeField] Color _textColor = new Color(.94f,.91f,.87f);

        public void WriteData(PlayerInstance player)
        {
            _playerName.text = player.PlayerInfo.Username;
            _kills.text = player.Kills.ToString();
            _deaths.text = player.Deaths.ToString();
            _assists.text = player.Assists.ToString();
            //assign appropriate color for player in scoreboard depending on team, if player is not in any team, give him white color
            Color teamColor = _textColor;

            _playerName.color = teamColor;
            _kills.color = teamColor;
            _deaths.color = teamColor;
            _assists.color = teamColor;
            _latency.color = teamColor;

            _latency.text = player.BOT ? "BOT" : player.Latency.ToString();

            _background.color=player==ClientFrontend.ClientPlayerInstance?_localPlayerColor:_rowColor;
        }
    }
}
