using Fusion;
using TMPro;
using UnityEngine;

namespace Snowballers
{
    public class PlayerScoreUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI playerNameTmp;
        [SerializeField] private TextMeshProUGUI playerScoreTmp;

        public PlayerRef PlayerRef => _playerRef;

        private PlayerRef _playerRef;
        private bool _isLocal;
        
        public void Setup(PlayerRef playerRef, bool isLocalPlayer)
        {
            _playerRef = playerRef;
            _isLocal = isLocalPlayer;
            
            playerNameTmp.text = isLocalPlayer ? "You" : "Them";
        }

        public void SetScore(int score)
        {
            playerScoreTmp.text = score.ToString("0");
        }
    }
}


