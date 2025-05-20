using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using Photon.Realtime;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkGameBoard : NetworkBehaviour
    {
        public event Action<PlayerRef, bool> PlayerJoinedCallback;
        public event Action<PlayerRef, bool> PlayerLeftCallback;
        public event Action<Dictionary<PlayerRef, int>> PlayerScoresChangedCallback;

        [SerializeField] private NetworkPlayerManager networkPlayerManager;

        [Networked] private NetworkDictionary<PlayerRef, Int32> PlayerScores { get; }

        public override void Spawned()
        {
            foreach (var playerRef in Runner.ActivePlayers)
            {
                var playerRig = NetworkUtils.GetPlayerRigFromRef(Runner, playerRef);
                if (playerRig)
                {
                    PlayerJoined(playerRef, playerRig);
                }
            }

            networkPlayerManager.PlayerJoinedCallback += PlayerJoined;
            networkPlayerManager.PlayerLeftCallback += PlayerLeft;
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
            
            networkPlayerHealth.NoHealthLeft += OnPlayerDied;
            var isLocal = Runner.LocalPlayer == player;
            PlayerJoinedCallback?.Invoke(player, isLocal);    
        }

        private void PlayerLeft(PlayerRef player, NetworkPlayer networkPlayer)
        {
            if (PlayerScores.ContainsKey(player))
            {
                var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
                var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
                networkPlayerHealth.NoHealthLeft -= OnPlayerDied;

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
                    PlayerScores.Set(kvp.Key, kvp.Value + 1);
                }
            }

            var dictionaryParse = PlayerScores.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            PlayerScoresChangedCallback?.Invoke(dictionaryParse);
        }
    }
}