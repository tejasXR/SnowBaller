using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkSnowball : NetworkThrowable
    {
        [Header("Snowball Properties")]
        [Networked] public float Damage { get; set; }
        [Networked, OnChangedRender(nameof(OnRemoteTrailIsEmittingChanged))] private bool IsTrailEmitting { get; set; }
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private NetworkParticles snowballDestroyVfx;
        [SerializeField] private NetworkAudioClip snowballDestroySfx;
        
        private void Awake()
        {
            ThrowableThrownCallback += OnThrowableThrown;
            ThrowableDestroyedCallback += OnThrowableDestroyed;
        }

        private void Start()
        {
            ChangeLocalTrailIsEmitting(false);
        }

        protected override void OnCollision(Collision collision)
        {
            if (!Runner.IsRunning)
            {
                return;
            }
            
            if (!HasStateAuthority)
            {
                return;
            }
            
            if (ThrowingPlayer == PlayerRef.None)
            {
                return;
            }
            
            var playerHealth = collision.gameObject.GetComponentInParent<NetworkHealth>();
            if (!playerHealth)
            {
                return;
            }
                
            playerHealth.ReduceHealthRpc(Damage);
        }

        private void ChangeLocalTrailIsEmitting(bool isEmitting)
        {
            IsTrailEmitting = isEmitting;
        }

        private void OnRemoteTrailIsEmittingChanged(NetworkBehaviourBuffer previous)
        {
            var prevValue = GetPropertyReader<bool>(nameof(IsTrailEmitting)).Read(previous);
            trailRenderer.emitting = !prevValue;
        }
        
        private void OnThrowableThrown()
        {
            ChangeLocalTrailIsEmitting(true);
        }

        private void OnThrowableDestroyed()
        {
            Runner.Spawn(snowballDestroyVfx, transform.position, Quaternion.identity);
            Runner.Spawn(snowballDestroySfx, transform.position, Quaternion.identity);
        }
    }
}