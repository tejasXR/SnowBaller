using System;
using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkGrabbableSpawnTimer : NetworkBehaviour
    {
        public event Action TimerStartedCallback;
        public event Action TimerExpiredCallback;

        [SerializeField] private CustomNetworkHandColliderGrabbable networkGrabbablePrefab;
        [SerializeField] private int ticksToSpawnNextItem;

        [Networked] private NetworkTimer SpawnTimeCountdown { get; set; }
        [Networked] private int TicksToSpawnNextItem { get; set; }
        
        private CustomNetworkHandColliderGrabbable _currentSpawnedGrabbable;
        
        private void StartTimer()
        {
            if (Runner.IsServer)
            {
                return;
            }

            TicksToSpawnNextItem = ticksToSpawnNextItem;
            SpawnTimeCountdown = NetworkTimer.CreateFromTicks(Runner, TicksToSpawnNextItem);
            TimerStartedCallback?.Invoke();
        }
        
        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();

            var t = SpawnTimeCountdown.ElapsedTicks(Runner);
            
            if (!SpawnTimeCountdown.Expired(Runner))
            {
                return;
            }

            if (_currentSpawnedGrabbable)
            {
                return;
            }
            
            // Spawn!
            SpawnNetworkObject();
                
            // Reset Timer
            SpawnTimeCountdown = default;
            TimerExpiredCallback?.Invoke();
        }

        private void SpawnNetworkObject()
        {
            _currentSpawnedGrabbable = Runner.Spawn<CustomNetworkHandColliderGrabbable>(networkGrabbablePrefab,
                transform.position, Quaternion.identity, Runner.LocalPlayer);

            _currentSpawnedGrabbable.onDidGrab.AddListener(OnSpawnedGrabbableGrabbed);
        }

        private void OnSpawnedGrabbableGrabbed(CustomNetworkHandColliderGrabber grabber)
        {
            _currentSpawnedGrabbable.onDidGrab.RemoveListener(OnSpawnedGrabbableGrabbed);
            _currentSpawnedGrabbable = null;
            
            StartTimer();
        }
    }
}