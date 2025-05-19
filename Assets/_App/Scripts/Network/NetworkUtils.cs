using Fusion;
using Fusion.XR.Shared.Rig;

namespace Snowballers.Network
{
    public static class NetworkUtils
    {
        public static Player GetPlayerRigFromRef(NetworkRunner runner, PlayerRef playerRef)
        {
            runner.TryGetPlayerObject(playerRef, out var playerNetworkObject);
            return playerNetworkObject ? playerNetworkObject.GetComponent<Player>() : null;
        }
    }
}