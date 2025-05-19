using UnityEngine;

namespace Snowballers
{
    public class Player : MonoBehaviour
    {
        [SerializeField] private PlayerLocomotion playerLocomotion;

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            playerLocomotion.ToggleMovement(false);
            
            playerLocomotion.transform.localPosition = Vector3.zero;
            playerLocomotion.transform.localRotation = Quaternion.Euler(Vector3.zero);
            
            transform.position = position;
            transform.rotation = rotation;
        }
    }
}


