using System;
using System.Collections.Generic;
using System.Linq;
using Fusion;

namespace Snowballers.Network
{
    public class NetworkGameBoard : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        public event Action<PlayerRef, bool> PlayerJoinedCallback;
        public event Action<PlayerRef, bool> PlayerLeftCallback;
        public event Action<Dictionary<PlayerRef, int>> PlayerScoresChangedCallback;
        
        [Networked] private NetworkDictionary<PlayerRef, Int32> PlayerScores { get; }

        public override void Spawned()
        {
            base.Spawned();
            foreach (var playerRef in Runner.ActivePlayers)
            {
                PlayerJoined(playerRef);
            }
        }

        public void PlayerJoined(PlayerRef player)
        {
            var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
            if (!networkRig)
            {
                return;
            }
            
            var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
            if (!networkPlayerHealth)
            {
                return;
            }
            
            PlayerScores.Add(player, 0);
            networkPlayerHealth.NoHealthLeft += OnPlayerDied;
            var isLocal = Runner.LocalPlayer == player;
            PlayerJoinedCallback?.Invoke(player, isLocal);
        }
        
        public void PlayerLeft(PlayerRef player)
        {
            if (PlayerScores.ContainsKey(player))
            {
                var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
                var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
                networkPlayerHealth.NoHealthLeft -= OnPlayerDied;
                
                PlayerScores.Remove(player);
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