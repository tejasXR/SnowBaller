using System.Linq;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkPlayerSpawner : NetworkBehaviour, IPlayerJoined, IPlayerLeft
    {
        [SerializeField] private NetworkTransform[] spawnPointTransforms;
        
        [Networked] private NetworkDictionary<PlayerRef, NetworkTransform> PlayerSpawnPointPlacement { get; }
        
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
            
            PlayerSpawnPointPlacement.Add(player, openSpawnPoint);
            networkPlayerHealth.NoHealthLeft += RespawnPlayers;
            // SpawnPlayerAsync(player, openSpawnPoint).Forget();
        }
        
        public void PlayerLeft(PlayerRef player)
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
                SpawnPlayerAsync(kvp.Key, kvp.Value).Forget();
            }
        }

        private async UniTask SpawnPlayerAsync(PlayerRef playerRef, NetworkTransform spawnTransform)
        {
            var player = NetworkUtils.GetPlayerRigFromRef(Runner, playerRef);
            var networkObject = player.GetComponent<NetworkObject>();
            
            await UniTask.WaitForEndOfFrame();
            
            networkObject.RequestStateAuthority();
            
            player.Teleport(spawnTransform.transform.position, spawnTransform.transform.rotation);

            await UniTask.WaitForEndOfFrame();
            
            networkObject.ReleaseStateAuthority();
        }
    }
}