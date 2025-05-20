using Cysharp.Threading.Tasks;
using Fusion.XR.Shared.Rig;
using UnityEngine;

namespace Snowballers
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion playerLocomotion;
        [SerializeField] private HardwareRig hardwareRig;
        [SerializeField] private Rigidbody rb;

        private const float FadeTransition = .25F;

        public async UniTask TeleportAsync(Vector3 position, Quaternion rotation)
        {
            playerLocomotion.ToggleMovement(false);

            await UniTask.WaitForEndOfFrame();
            
            rb.isKinematic = true;
            
            hardwareRig.Teleport(position);

            await UniTask.WaitForEndOfFrame();
            
            StartCoroutine(hardwareRig.headset.fader.FadeOut(FadeTransition));

            var yAngleDelta = hardwareRig.headset.transform.localRotation.eulerAngles.y - rotation.eulerAngles.y;
            hardwareRig.Rotate(yAngleDelta);
            
            playerLocomotion.transform.localPosition = Vector3.zero;
            playerLocomotion.transform.localRotation = Quaternion.Euler(Vector3.zero);
            
            await UniTask.WaitForSeconds(FadeTransition);
            
            playerLocomotion.ToggleMovement(true);
            
            await UniTask.WaitForEndOfFrame();
            
            rb.isKinematic = false;
            
            StartCoroutine(hardwareRig.headset.fader.FadeIn(FadeTransition));
        }
    }
}


