using System;
using System.Collections.Generic;
using Fusion;

namespace Snowballers.Network
{
    public class NetworkPlayerManager : NetworkBehaviour
    {
        public event Action<PlayerRef, NetworkPlayer> PlayerJoinedCallback;
        public event Action<PlayerRef, NetworkPlayer> PlayerLeftCallback;

        public List<PlayerRef> PlayerRefs => _playerRefs;

        // Intentionally non-networked
        private readonly List<PlayerRef> _playerRefs = new List<PlayerRef>();
        
        public void RegisterPlayer(PlayerRef playerRef)
        {
            if (_playerRefs.Contains(playerRef))
            {
                return;
            }
            
            var playerRig = NetworkUtils.GetPlayerRigFromRef(Runner, playerRef);
            _playerRefs.Add(playerRef);
            PlayerJoinedCallback?.Invoke(playerRef, playerRig);
        }

        public void UnregisterPlayer(PlayerRef playerRef)
        {
            if (!_playerRefs.Contains(playerRef))
            {
                return;
            }
            
            var playerRig = NetworkUtils.GetPlayerRigFromRef(Runner, playerRef);
            _playerRefs.Remove(playerRef);
            PlayerLeftCallback?.Invoke(playerRef, playerRig);
        }
    }
}