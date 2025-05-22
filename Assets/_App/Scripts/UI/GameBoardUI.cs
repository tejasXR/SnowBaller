using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Snowballers.Network;
using TMPro;
using UnityEngine;

namespace Snowballers
{
    public class GameBoardUI : MonoBehaviour
    {
        [SerializeField] private NetworkGameBoard networkGameBoard;
        [SerializeField] private RectTransform playerScoreContainer;
        [SerializeField] private PlayerScoreUI playerScoreUiPrefab;
        [SerializeField] private TextMeshProUGUI winnerTmp;

        private readonly Dictionary<PlayerRef, PlayerScoreUI> _playerScoreUis = new Dictionary<PlayerRef, PlayerScoreUI>();
        
        private void Awake()
        {
            networkGameBoard.PlayerJoinedCallback += OnPlayerJoined;
            networkGameBoard.PlayerLeftCallback += OnPlayerLeft;
            networkGameBoard.PlayerScoresChangedCallback += OnPlayerScoresChanged;
            networkGameBoard.WinnerDeterminedCallback += OnWinnerDetermined;
        }

        private void OnDestroy()
        {
            networkGameBoard.PlayerJoinedCallback -= OnPlayerJoined;
            networkGameBoard.PlayerLeftCallback -= OnPlayerLeft;
            networkGameBoard.PlayerScoresChangedCallback -= OnPlayerScoresChanged;
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
            foreach (var kvp in _playerScoreUis.Where(kvp => kvp.Key == playerRef))
            {
                _playerScoreUis.Remove(kvp.Key);
            }
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
            var personString = didLocalPlayerWin ? "You won!!" : "You lost, but hopefully had fun!!";
            winnerTmp.text = personString;
        }
    }
}