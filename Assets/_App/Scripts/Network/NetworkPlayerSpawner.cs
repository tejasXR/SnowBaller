using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    [Serializable]
    public struct SpawnPoint: INetworkStruct
    {
        public Vector3 position;
        public Quaternion rotation;
    }
    
    public class NetworkPlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkPlayerManager networkPlayerManager;
        [SerializeField] private NetworkTransform[] spawnPointTransforms;
        
        [Networked] private NetworkDictionary<PlayerRef, SpawnPoint> PlayerSpawnPointPlacement { get; }

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
            // Only handle spawning for local players
            if (Runner.LocalPlayer != player)
            {
                return;
            }
            
            var networkPlayerHealth = networkPlayer.GetComponentInChildren<NetworkHealth>();
            if (!networkPlayerHealth)
            {
                return;
            }

            SpawnPoint openSpawnPosition;
            var firstSpawnPoint = new SpawnPoint()
            {
                position = spawnPointTransforms[0].transform.position,
                rotation = spawnPointTransforms[0].transform.rotation
            };

            var secondSpawnPoint = new SpawnPoint()
            {
                position = spawnPointTransforms[1].transform.position,
                rotation = spawnPointTransforms[1].transform.rotation
            };
            
            if (PlayerSpawnPointPlacement.Count == 0)
            {
                openSpawnPosition = firstSpawnPoint;
            }
            else
            {
                var currentPlayer = Runner.ActivePlayers.First();
                PlayerSpawnPointPlacement.TryGet(currentPlayer, out var spawnPoint);
                openSpawnPosition = spawnPoint.position == firstSpawnPoint.position ? secondSpawnPoint : firstSpawnPoint;
            }
            
            if (Runner.ActivePlayers.Count() == 1)
            {
                // Clear values from memory if we are the first player joining
                PlayerSpawnPointPlacement.Clear();
            }
            
            if (!PlayerSpawnPointPlacement.ContainsKey(player))
            {
                PlayerSpawnPointPlacement.Add(player, openSpawnPosition);
                networkPlayerHealth.NoHealthLeftCallback += RespawnPlayers;
                SpawnPlayer(player, openSpawnPosition);
            }
        }

        private void PlayerLeft(PlayerRef player, NetworkPlayer networkPlayer)
        {
            if (PlayerSpawnPointPlacement.ContainsKey(player))
            {
                var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
                var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
                networkPlayerHealth.NoHealthLeftCallback -= RespawnPlayers;

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

        private void SpawnPlayer(PlayerRef playerRef, SpawnPoint spawnPoint)
        {
            if (Runner.LocalPlayer != playerRef)
            {
                return;
            }

            var localPlayer = FindFirstObjectByType<Player>();
            localPlayer.TeleportAsync(spawnPoint.position, spawnPoint.rotation).Forget();
        }
    }
}