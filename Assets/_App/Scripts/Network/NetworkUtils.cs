using System;
using Fusion;
using Fusion.XR.Shared.Rig;
using UnityEngine;

namespace Snowballers.Network
{
    public static class NetworkUtils
    {
        public static NetworkPlayer GetPlayerRigFromRef(NetworkRunner runner, PlayerRef playerRef)
        {
            runner.TryGetPlayerObject(playerRef, out var playerNetworkObject);

            if (playerNetworkObject != null)
            {
                return playerNetworkObject.GetComponent<NetworkPlayer>();
            }
            
            if (playerNetworkObject == null)
            {
                var foundNetworkPlayer = GameObject.FindObjectsByType<NetworkPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (var networkPlayer in foundNetworkPlayer)
                {
                    if (networkPlayer.PlayerRef == playerRef)
                    {
                        return networkPlayer;
                    }
                }
            }

            throw new ApplicationException($"Can't find player object with PlayerRef {playerRef}");
        }
    }
}