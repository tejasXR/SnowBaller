using System.Collections.Generic;
using Fusion;
using Fusion.LagCompensation;
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
        // [SerializeField] private LayerMask lagCompensationLayers;

        private List<LagCompensatedHit> _lagHits = new List<LagCompensatedHit>();
        
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
                
            playerHealth.Reduce(Damage);
        }

        /*public override void FixedUpdateNetwork()
        {
            // Previous and next position is calculated based on the initial parameters.
            var previousPosition = GetMovePosition(Runner.Tick - 1);
            var nextPosition = GetMovePosition(Runner.Tick);
            var direction = nextPosition - previousPosition;
            
            int hitCount = Runner.LagCompensation.OverlapSphere(transform.position, .1F, ThrowingPlayer, _lagHits,
                lagCompensationLayers, HitOptions.IncludePhysX | HitOptions.IgnoreInputAuthority);
            
            if (hitCount == 0)
            {
                return;
            }
            
            for (int i = 0; i < hitCount; i++)
            {
                var playerHealth = _lagHits[i].Hitbox.transform.root.GetComponentInParent<NetworkHealth>();
                if (!playerHealth)
                {
                    return;
                }
                
                playerHealth.Reduce(Damage);
                Destroy();
            }
        }*/

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
        }
    }
}