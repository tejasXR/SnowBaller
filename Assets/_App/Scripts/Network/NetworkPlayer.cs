using System;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkPlayer : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] private NetworkObject networkObject;
        [SerializeField] private Collider bodyCollider;
        
        public PlayerRef PlayerRef => _playerRef;

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

            // TEJAS: Turn off the body collider for the local client
            if (Runner.LocalPlayer == player)
            {
                bodyCollider.enabled = false;
            }

            var localPlayerLocomotion = FindAnyObjectByType<PlayerLocomotion>(FindObjectsInactive.Include);
            localPlayerLocomotion.ToggleMovement(true);
            
            _networkPlayerManager.RegisterPlayer(player);
        }

        public void PlayerLeft(PlayerRef player)
        {
            _networkPlayerManager.UnregisterPlayer(_playerRef);
        }
    }
}