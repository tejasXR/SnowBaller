using System;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkPlayer : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] private NetworkObject networkObject;

        private PlayerRef _playerRef;
        private NetworkPlayerManager _networkPlayerManager;
        
        public void PlayerJoined(PlayerRef player)
        {
            _playerRef = player;
            Runner.SetPlayerObject(player, networkObject);

            _networkPlayerManager = FindAnyObjectByType<NetworkPlayerManager>(FindObjectsInactive.Include);
            if (!_networkPlayerManager)
            {
                Debug.LogError("Can't find Network Player Manager in scene!");
                return;
            }
            
            _networkPlayerManager.RegisterPlayer(player);
        }

        public void PlayerLeft(PlayerRef player)
        {
            _networkPlayerManager.UnregisterPlayer(_playerRef);
        }
    }
}