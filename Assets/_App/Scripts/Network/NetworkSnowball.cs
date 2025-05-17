using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkSnowball : NetworkThrowable
    {
        [Header("Snowball Properties")]
        [Networked] public float Damage { get; set; }
        [SerializeField] private TrailRenderer trailRenderer;
        [SerializeField] private NetworkParticles snowballDestroyVfx;

        private void Awake()
        {
            ThrowableThrownCallback += OnThrowableThrown;
            ThrowableDestroyedCallback += OnThrowableDestroyed;
        }

        private void Start()
        {
            trailRenderer.emitting = false;
        }

        private void OnThrowableThrown()
        {
            trailRenderer.emitting = true;
        }

        private void OnThrowableDestroyed()
        {
            Runner.Spawn(snowballDestroyVfx, transform.position, Quaternion.identity);
        }

        protected override void OnCollision(Collision collision)
        {
            var playerHealth = collision.gameObject.GetComponentInParent<PlayerHealth>();
            if (playerHealth)
            {
                playerHealth.Hit(Damage);
            }
        }
    }
}