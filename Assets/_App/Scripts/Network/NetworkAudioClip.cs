using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkAudioClip : NetworkBehaviour
    {
        [SerializeField] private AudioSource audioSfx;

        private bool _audioSfx;
        
        public override void Spawned()
        {
            base.Spawned();
            audioSfx.Play();
            _audioSfx = true;
        }

        public override void FixedUpdateNetwork()
        {
            if (!_audioSfx)
            {
                return;
            }

            if (!Mathf.Approximately(audioSfx.time, audioSfx.clip.length))
            {
                return;
            }
            
            Runner.Despawn(Object);
            Destroy(gameObject);
        }
    }
}


