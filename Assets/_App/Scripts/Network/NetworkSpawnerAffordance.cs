using Fusion;
using Snowballers.Network;
using UnityEngine;

namespace Snowballers
{
    public class NetworkSpawnerAffordance : NetworkBehaviour
    {
        [SerializeField] private NetworkGrabbableSpawnTimer grabbableSpawnTimer;
        [SerializeField] private GameObject spawnerAffordance;
        [SerializeField] private float spawnerRotationSpeed;
    
        private void Awake()
        {
            grabbableSpawnTimer.TimerStartedCallback += OnTimerStarted;
            grabbableSpawnTimer.TimerExpiredCallback += OnTimerExpired;
        }

        private void OnTimerStarted()
        {
            ScaleAffordance(0);
        }

        private void OnTimerExpired()
        {
            ScaleAffordance(1);
        }

        public override void FixedUpdateNetwork()
        {
            spawnerAffordance.transform.Rotate(Vector3.up, spawnerRotationSpeed * Time.deltaTime);
        }

        private void ScaleAffordance(float affordanceScale)
        {
            spawnerAffordance.transform.localScale = Vector3.one * affordanceScale;
        }
    }
}