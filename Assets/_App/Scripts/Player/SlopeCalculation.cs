using System;
using UnityEngine;

namespace Snowballers
{
    public class SlopeCalculation : MonoBehaviour
    {
        public bool IsOnSlope { get; private set; }
        
        [SerializeField] private LayerMask locomotionLayer;
        [SerializeField] private Transform playerHead;

        private const float DotProductThreshold = .98F;

        private void OnCollisionStay(Collision other)
        {
            // var colliderTop = playerHead.transform.localPosition + new Vector3(0, bodyCollider.height, 0);
            var rayOrigin = playerHead.position;
            
#if UNITY_EDITOR
            Debug.DrawRay(rayOrigin, Vector3.down, Color.green);
#endif
            
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hitInfo, Mathf.Infinity, locomotionLayer.value))
            {
                var dotProductUp = Vector3.Dot(hitInfo.normal, Vector3.up);
                IsOnSlope = dotProductUp < DotProductThreshold;
            }
        }

        private void OnCollisionExit(Collision other)
        {
            IsOnSlope = false;
        }
    }
}


