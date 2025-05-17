using System;
using System.Collections.Generic;
using Fusion;
using Fusion.XR.Shared.Rig;

namespace Snowballers.Network
{
    public class NetworkGameBoard : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        public event Action<PlayerRef, bool> PlayerJoinedCallback;
        public event Action<PlayerRef, bool> PlayerLeftCallback;
        public event Action<Dictionary<PlayerRef, int>> PlayerScoresChangedCallback;
        
        private readonly Dictionary<PlayerRef, int> _playerScores = new Dictionary<PlayerRef, int>();
        
        public void PlayerJoined(PlayerRef player)
        {
            var networkRig = GetPlayerRigFromRef(player);
            var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
            if (!networkPlayerHealth)
            {
                return;
            }
            
            _playerScores.Add(player, 0);
            networkPlayerHealth.NoHealthLeft += OnPlayerDied;
            var isLocal = Runner.LocalPlayer == player;
            PlayerJoinedCallback?.Invoke(player, isLocal);
        }
        
        public void PlayerLeft(PlayerRef player)
        {
            if (_playerScores.ContainsKey(player))
            {
                _playerScores.Remove(player);
                var isLocal = Runner.LocalPlayer == player;
                PlayerLeftCallback?.Invoke(player, isLocal);
            }
        }

        private NetworkRig GetPlayerRigFromRef(PlayerRef playerRef)
        {
            Runner.TryGetPlayerObject(playerRef, out var playerNetworkObject);
            return playerNetworkObject ? playerNetworkObject.GetComponent<NetworkRig>() : null;
        }

        private void OnPlayerDied(PlayerRef playerRef)
        {
            // Right now we are only planning for 2 players!
            foreach (var kvp in _playerScores)
            {
                // When a player dies, give the other player a score
                if (kvp.Key != playerRef)
                {
                    _playerScores[kvp.Key] = kvp.Value + 1;
                }
            }
            
            PlayerScoresChangedCallback?.Invoke(_playerScores);
        }
    }
}