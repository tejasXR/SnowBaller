using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkPlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkPlayerManager networkPlayerManager;
        [SerializeField] private NetworkTransform[] spawnPointTransforms;
        
        [Networked] private NetworkDictionary<PlayerRef, NetworkTransform> PlayerSpawnPointPlacement { get; }

        public override void Spawned()
        {
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

            NetworkTransform openSpawnPoint;
            var firstSpawnPoint = spawnPointTransforms[0];
            var secondSpawnPoint = spawnPointTransforms[1];
            
            if (PlayerSpawnPointPlacement.Count == 0)
            {
                openSpawnPoint = firstSpawnPoint;
            }
            else
            {
                var currentPlayer = Runner.ActivePlayers.First();
                PlayerSpawnPointPlacement.TryGet(currentPlayer, out var spawnPoint);
                openSpawnPoint = spawnPoint == firstSpawnPoint ? secondSpawnPoint : firstSpawnPoint;
            }
            
            if (Runner.ActivePlayers.Count() == 1)
            {
                // Clear values from memory if we are the first player joining
                PlayerSpawnPointPlacement.Clear();
            }
            
            if (!PlayerSpawnPointPlacement.ContainsKey(player))
            {
                PlayerSpawnPointPlacement.Add(player, openSpawnPoint);
                networkPlayerHealth.NoHealthLeft += RespawnPlayers;
                SpawnPlayer(player, openSpawnPoint);
            }
        }

        private void PlayerLeft(PlayerRef player, NetworkPlayer networkPlayer)
        {
            if (PlayerSpawnPointPlacement.ContainsKey(player))
            {
                var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
                var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
                networkPlayerHealth.NoHealthLeft -= RespawnPlayers;

                PlayerSpawnPointPlacement.Remove(player);
            }
        }

        private void RespawnPlayers(PlayerRef _)
        {
            foreach (var kvp in PlayerSpawnPointPlacement)
            {
                SpawnPlayer(kvp.Key, kvp.Value);
            }
        }

        private void SpawnPlayer(PlayerRef playerRef, NetworkTransform spawnTransform)
        {
            if (Runner.LocalPlayer != playerRef)
            {
                return;
            }

            var localPlayer = FindFirstObjectByType<Player>();
            localPlayer.TeleportAsync(spawnTransform.transform.position, spawnTransform.transform.rotation).Forget();
        }
    }
}