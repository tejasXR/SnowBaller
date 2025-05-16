using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkSnowball : NetworkThrowable
    {
        [Header("Snowball Properties")]
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
    }
}