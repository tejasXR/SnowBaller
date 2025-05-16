using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkParticles : NetworkBehaviour
    {
        [SerializeField] private ParticleSystem particleVfx;

        private bool _vfxStarted;
        
        public override void Spawned()
        {
            base.Spawned();
            particleVfx.Play();
            _vfxStarted = true;
        }

        public override void FixedUpdateNetwork()
        {
            base.FixedUpdateNetwork();
            if (!_vfxStarted)
            {
                return;
            }
            
            if (!particleVfx.isStopped || !particleVfx.IsAlive())
            {
                return;
            }
            
            DestroyBehaviour(this);
            Destroy(gameObject);
        }
    }
}