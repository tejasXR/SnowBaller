using System;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkGameStarter : NetworkBehaviour
    {
        public event Action GameStartedCallback;

        private const float StartGameDelay = 5F;
        
        [SerializeField] private NetworkGameBoard networkGameBoard;

        public override void Spawned()
        {
            networkGameBoard.WinnerDeterminedCallback += OnWinnerDetermined;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            networkGameBoard.WinnerDeterminedCallback -= OnWinnerDetermined;
        }

        private void OnWinnerDetermined(bool isLocalPlayer)
        {
            StartGameAfterSecondsAsync(StartGameDelay).Forget();
        }

        private async UniTask StartGameAfterSecondsAsync(float secondsDelay)
        {
            await UniTask.WaitForSeconds(StartGameDelay);
            GameStartedCallback?.Invoke();
        }
    }
}