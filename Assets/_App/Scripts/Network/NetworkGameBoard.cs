using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkGameBoard : NetworkBehaviour
    {
        public event Action<PlayerRef, bool> PlayerJoinedCallback;
        public event Action<PlayerRef, bool> PlayerLeftCallback;
        public event Action<Dictionary<PlayerRef, int>> PlayerScoresChangedCallback;
        public event Action<bool> WinnerDeterminedCallback; 

        public int WinningGameScore => 3;
        
        [SerializeField] private NetworkPlayerManager networkPlayerManager;

        [Networked, OnChangedRender(nameof(OnRemotePlayerScoresChanged))]
        private NetworkDictionary<PlayerRef, Int32> PlayerScores { get; }

        public override void Spawned()
        {
            foreach (var playerRef in networkPlayerManager.PlayerRefs)
            {
                var playerRig = NetworkUtils.GetPlayerRigFromRef(Runner, playerRef);
                if (playerRig)
                {
                    PlayerJoined(playerRef, playerRig);
                }
            }

            networkPlayerManager.PlayerJoinedCallback += PlayerJoined;
            networkPlayerManager.PlayerLeftCallback += PlayerLeft;

            PlayerScoresChangedCallback += CheckWinner;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            networkPlayerManager.PlayerJoinedCallback -= PlayerJoined;
            networkPlayerManager.PlayerLeftCallback -= PlayerLeft;
        }

        private void PlayerJoined(PlayerRef player, NetworkPlayer networkPlayer)
        {
            var networkPlayerHealth = networkPlayer.GetComponentInChildren<NetworkHealth>();
            if (!networkPlayerHealth)
            {
                return;
            }

            if (Runner.ActivePlayers.Count() == 1)
            {
                // Clear values from memory if we are the first player joining
                PlayerScores.Clear();
            }

            if (!PlayerScores.ContainsKey(player))
            {
                PlayerScores.Add(player, 0);
            }
            
            networkPlayerHealth.NoHealthLeftCallback += OnPlayerDied;
            var isLocal = Runner.LocalPlayer == player;
            PlayerJoinedCallback?.Invoke(player, isLocal);    
        }

        private void PlayerLeft(PlayerRef player, NetworkPlayer networkPlayer)
        {
            if (PlayerScores.ContainsKey(player))
            {
                var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
                var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
                networkPlayerHealth.NoHealthLeftCallback -= OnPlayerDied;

                if (PlayerScores.ContainsKey(player))
                {
                    PlayerScores.Remove(player);
                }
                
                var isLocal = Runner.LocalPlayer == player;
                PlayerLeftCallback?.Invoke(player, isLocal);
            }
        }
        
        private void OnPlayerDied(PlayerRef playerRef)
        {
            // Right now we are only planning for 2 players!
            foreach (var kvp in PlayerScores)
            {
                // When a player dies, give the other player a score
                if (kvp.Key != playerRef)
                {
                    PlayerScoresChangedRpc(kvp.Key, kvp.Value + 1);
                }
            }
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void PlayerScoresChangedRpc(PlayerRef playerRef, Int32 score)
        {
            if (PlayerScores.ContainsKey(playerRef))
            {
                PlayerScores.Set(playerRef, score);
            }
        }

        private void OnRemotePlayerScoresChanged()
        {
            var dictionaryParse = PlayerScores.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            PlayerScoresChangedCallback?.Invoke(dictionaryParse);
        }

        private void CheckWinner(Dictionary<PlayerRef, int> dictionary)
        {
            foreach (var kvp in dictionary)
            {
                var playerScore = kvp.Value;
                if (playerScore >= WinningGameScore)
                {
                    var didLocalPlayerWin = kvp.Key == Runner.LocalPlayer;
                    WinnerDeterminedCallback?.Invoke(didLocalPlayerWin);
                }
            }
        }
    }
}