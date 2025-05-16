using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkParticles : NetworkBehaviour
    {
        [SerializeField] private bool shouldPlayOnSpawn;
        [SerializeField] private ParticleSystem particleVfx;

        public override void Spawned()
        {
            base.Spawned();

            if (shouldPlayOnSpawn)
            {
                particleVfx.Play();
            }
        }
    }
}