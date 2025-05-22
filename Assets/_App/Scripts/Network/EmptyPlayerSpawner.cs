using Fusion;
using UnityEngine;

namespace Snowballers.Network
{
    public class EmptyPlayerSpawner : NetworkBehaviour
    {
        [SerializeField] private NetworkObject networkUserRig;

        [ContextMenu("Create Networked Player")]
        public void CreateNetworkedPlayer()
        {
            Runner.Spawn(networkUserRig, position: transform.position, rotation: transform.rotation);
        }
    }
}


