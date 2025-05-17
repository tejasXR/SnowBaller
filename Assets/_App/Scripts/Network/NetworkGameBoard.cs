using System;
using System.Collections.Generic;
using Fusion;
using Fusion.XR.Shared.Rig;

namespace Snowballers.Network
{
    public class NetworkGameBoard : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        public event Action PlayerJoinedCallback;
        public event Action PlayerLeftCallback;
        public event Action<Dictionary<PlayerRef, int>> PlayerScoresChangedCallback;
        
        private readonly Dictionary<PlayerRef, int> _playerScores = new Dictionary<PlayerRef, int>();
        
        public void PlayerJoined(PlayerRef player)
        {
            var networkRig = GetPlayerRigFromRef(player);
            var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkPlayerHealth>();
            if (!networkPlayerHealth)
            {
                return;
            }
            
            _playerScores.Add(player, 0);
            networkPlayerHealth.PlayerDeadCallback += OnPlayerDied;
            PlayerJoinedCallback?.Invoke();
        }
        
        public void PlayerLeft(PlayerRef player)
        {
            if (_playerScores.ContainsKey(player))
            {
                _playerScores.Remove(player);
                PlayerLeftCallback?.Invoke();
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