using System;
using System.Collections.Generic;
using Fusion;
using Snowballers.Network;
using UnityEngine;

namespace Snowballers
{
    public class GameBoardUI : MonoBehaviour
    {
        [SerializeField] private NetworkGameBoard networkGameBoard;
        [SerializeField] private RectTransform playerScoreContainer;
        [SerializeField] private PlayerScoreUI playerScoreUiPrefab;

        private readonly List<PlayerScoreUI> _playerScoreUis = new List<PlayerScoreUI>();
        
        private void Awake()
        {
            networkGameBoard.PlayerJoinedCallback += OnPlayerJoined;
            networkGameBoard.PlayerScoresChangedCallback += OnPlayerScoresChanged;
        }

        private void OnPlayerJoined(PlayerRef playerRef, bool isLocal)
        {
            var playerScore = Instantiate(playerScoreUiPrefab, playerScoreContainer);
            playerScore.Setup(playerRef, isLocal);
            playerScore.SetScore(0);
            
            _playerScoreUis.Add(playerScore);
        }

        private void OnPlayerScoresChanged(Dictionary<PlayerRef, int> playerScoreDictionary)
        {
            foreach (var playerScoreUi in _playerScoreUis)
            {
                if (playerScoreDictionary.TryGetValue(playerScoreUi.PlayerRef, out var networkPlayerScore))
                {
                    playerScoreUi.SetScore(networkPlayerScore);
                }
            }
        }
    }
}