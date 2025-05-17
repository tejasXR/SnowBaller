using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkPlayer : NetworkBehaviour
    {
        [SerializeField] private NetworkObject networkObject;
    
        public override void Spawned()
        {
            base.Spawned();
            Runner.SetPlayerObject(Runner.LocalPlayer, networkObject);
        }
    }
}