using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkTriSnowball : NetworkThrowable
    {
        [SerializeField] private NetworkSnowball snowballPrefab;
        [SerializeField] private float velocityVariationMagnitude = 1;
        
        private void Awake()
        {
            ThrowableThrownCallback += OnThrowableThrown;
        }
        
        private void OnThrowableThrown()
        {
            OnThrowableThrownAsync().Forget();
        }
        
        private async UniTask OnThrowableThrownAsync()
        {
            var createdSnowballs = new List<NetworkSnowball>();
            var startingVelocity = Grabbable.Velocity;

            for (int i = 0; i < 3; i++)
            {
                var snowball = Runner.Spawn(snowballPrefab, transform.position, transform.rotation);
                createdSnowballs.Add(snowball);

                await UniTask.WaitForEndOfFrame();
            }

            foreach (var snowball in createdSnowballs)
            {
                snowball.SetThrownState();
                
                await UniTask.WaitForEndOfFrame();
                
                var variedVelocity = AddVelocityVariation(startingVelocity, transform.forward);
                var amplifiedVelocity = variedVelocity * Grabbable.ThrowVelocityMultiplier;
                snowball.Grabbable.SetVelocity(Grabbable.Velocity, Grabbable.AngularVelocity);
                // snowball.Grabbable.SetVelocity(amplifiedVelocity, Grabbable.AngularVelocity);
            }
            
            Destroy();
        }

        private Vector3 AddVelocityVariation(Vector3 startingVelocity, Vector3 startingDirection)
        {
            var x = Random.Range(velocityVariationMagnitude / 2, velocityVariationMagnitude);
            var y = Random.Range(velocityVariationMagnitude / 2, velocityVariationMagnitude);
            var z = Random.Range(velocityVariationMagnitude / 2, velocityVariationMagnitude);

            x = startingDirection.x * x;
            y = startingDirection.y * y;
            z = startingDirection.z * z;
            
            return startingVelocity + new Vector3(x, y, z);
        }
    }
}