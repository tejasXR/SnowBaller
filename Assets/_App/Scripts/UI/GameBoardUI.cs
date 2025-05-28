using System.Collections.Generic;
using System.Linq;
using Fusion;
using Snowballers.Network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Snowballers.UI
{
    public class GameBoardUI : MonoBehaviour
    {
        [SerializeField] private NetworkGameBoard networkGameBoard;
        [SerializeField] private NetworkGameStarter networkGameStarter;
        [SerializeField] private RectTransform playerScoreContainer;
        [SerializeField] private PlayerScoreUI playerScoreUiPrefab;
        [SerializeField] private GameObject winnerContainer;
        [SerializeField] private TextMeshProUGUI winnerTmp;
        [SerializeField] private AudioSource winnerAudioSource;

        private readonly Dictionary<PlayerRef, PlayerScoreUI> _playerScoreUis = new Dictionary<PlayerRef, PlayerScoreUI>();
        
        private void Awake()
        {
            networkGameBoard.PlayerJoinedCallback += OnPlayerJoined;
            networkGameBoard.PlayerLeftCallback += OnPlayerLeft;
            networkGameBoard.PlayerScoresChangedCallback += OnPlayerScoresChanged;
            networkGameBoard.WinnerDeterminedCallback += OnWinnerDetermined;

            networkGameStarter.GameStartedCallback += OnGameStarted;
        }

        private void Start()
        {
            winnerContainer.SetActive(false);
        }

        private void OnDestroy()
        {
            networkGameBoard.PlayerJoinedCallback -= OnPlayerJoined;
            networkGameBoard.PlayerLeftCallback -= OnPlayerLeft;
            networkGameBoard.PlayerScoresChangedCallback -= OnPlayerScoresChanged;
            
            networkGameStarter.GameStartedCallback -= OnGameStarted;
        }

        private void OnPlayerJoined(PlayerRef playerRef, bool isLocal)
        {
            var playerScore = Instantiate(playerScoreUiPrefab, playerScoreContainer);
            playerScore.Setup(playerRef, isLocal);
            playerScore.SetScore(0);
            _playerScoreUis.Add(playerRef, playerScore);
        }

        private void OnPlayerLeft(PlayerRef playerRef, bool isLocal)
        {
            var playerScoreUi = _playerScoreUis[playerRef];
            _playerScoreUis.Remove(playerRef);
            Destroy(playerScoreUi);
        }

        private void OnPlayerScoresChanged(Dictionary<PlayerRef, int> playerScoreDictionary)
        {
            foreach (var kvp in _playerScoreUis)
            {
                if (playerScoreDictionary.TryGetValue(kvp.Key, out var networkPlayerScore))
                {
                    kvp.Value.SetScore(networkPlayerScore);
                }
            }
        }

        private void OnWinnerDetermined(bool didLocalPlayerWin)
        {
            winnerContainer.SetActive(true);
            var personString = didLocalPlayerWin ? "You won!!" : "You lost, but hopefully had fun!!";
            winnerTmp.text = personString;
            
            winnerAudioSource.Play();
        }

        private void OnGameStarted()
        {
            winnerContainer.SetActive(false);
        }
    }
}