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
        [SerializeField] private NetworkGameStarter networkGameStarter;
        [SerializeField] private NetworkTransform[] spawnPointTransforms;
        
        // [Networked] private NetworkDictionary<PlayerRef, SpawnPoint> PlayerSpawnPointPlacement { get; }

        private SpawnPoint _spawnPoint;

        public override void Spawned()
        {
            networkPlayerManager.PlayerJoinedCallback += PlayerJoined;
            networkPlayerManager.PlayerLeftCallback += PlayerLeft;

            networkGameStarter.GameStartedCallback += OnGameStarted;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            networkPlayerManager.PlayerJoinedCallback -= PlayerJoined;
            networkPlayerManager.PlayerLeftCallback -= PlayerLeft;
            
            networkGameStarter.GameStartedCallback -= OnGameStarted;
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
            
            if (Runner.ActivePlayers.Count() == 1)
            {
                _spawnPoint = firstSpawnPoint;
            }
            else
            {
                // var currentPlayer = Runner.ActivePlayers.First();
                // PlayerSpawnPointPlacement.TryGet(currentPlayer, out var spawnPoint);
                // openSpawnPosition = spawnPoint.position == firstSpawnPoint.position ? secondSpawnPoint : firstSpawnPoint;
                
                // TEJAS: This introduces an intentional bug if one player leaves and rejoins!
                // Okay for now!
                _spawnPoint = secondSpawnPoint;
            }
            
            /*if (Runner.ActivePlayers.Count() == 1)
            {
                // Clear values from memory if we are the first player joining
                PlayerSpawnPointPlacement.Clear();
            }*/
            
            /*if (!PlayerSpawnPointPlacement.ContainsKey(player))
            {
                PlayerSpawnPointPlacement.Add(player, openSpawnPosition);
                networkPlayerHealth.NoHealthLeftCallback += RespawnPlayers;
                SpawnPlayer(player, openSpawnPosition);
            }*/
            
            SpawnPlayer(player, _spawnPoint);
        }

        private void PlayerLeft(PlayerRef player, NetworkPlayer networkPlayer)
        {
            /*if (PlayerSpawnPointPlacement.ContainsKey(player))
            {
                var networkRig = NetworkUtils.GetPlayerRigFromRef(Runner, player);
                var networkPlayerHealth = networkRig.GetComponentInChildren<NetworkHealth>();
                networkPlayerHealth.NoHealthLeftCallback -= RespawnPlayers;

                PlayerSpawnPointPlacement.Remove(player);
            }*/
        }

        private void OnGameStarted()
        {
            /*foreach (var kvp in PlayerSpawnPointPlacement)
            {
                SpawnPlayer(kvp.Key, kvp.Value);
            }*/
            
            SpawnPlayer(Runner.LocalPlayer, _spawnPoint);
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