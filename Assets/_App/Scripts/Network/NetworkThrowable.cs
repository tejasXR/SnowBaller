using System;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkThrowable : NetworkBehaviour
    {
        public event Action ThrowableDestroyedCallback;
        public event Action ThrowableThrownCallback;
        
        [SerializeField] private NetworkRigidbody3D networkRigidbody;
        [SerializeField] private LayerMask collisionMask;

        [Space] [SerializeField] private bool shouldDestroyOnCollision;
        [SerializeField] private float customGravity = 400F;
        [Space] [SerializeField] private SphereCollider throwableCollider;
        [SerializeField] private SphereCollider distanceGrabCollider;

        private CustomNetworkHandColliderGrabbable _grabbable;
        private bool _isGravityEnabled;
        
        public override void Spawned()
        {
            base.Spawned();
            
            _grabbable = GetComponent<CustomNetworkHandColliderGrabbable>();

            _grabbable.onDidGrab.AddListener(OnDidGrab);
            _grabbable.onWillGrab.AddListener(OnWillGrab);
            _grabbable.onDidUngrab.AddListener(OnDidUngrab);
            
            throwableCollider.enabled = false;
        }

        private void OnCollisionStay(Collision other)
        {
            if (Physics.CheckSphere(throwableCollider.transform.position, throwableCollider.radius, collisionMask))
            {
                if (shouldDestroyOnCollision)
                {
                    DestroyBehaviour(this); // TEJAS: Does this actually destroy over network? 
                    Destroy(gameObject);
                    ThrowableDestroyedCallback?.Invoke();
                }
            }
        }

        private void FixedUpdate()
        {
            if (_isGravityEnabled)
            {
                var gravityForce = Vector3.down * (customGravity * Time.deltaTime);
                networkRigidbody.Rigidbody.AddForce(gravityForce);
            }
        }

        private void OnWillGrab(CustomNetworkHandColliderGrabber grabber)
        {
            networkRigidbody.Rigidbody.isKinematic = true;
        }

        private void OnDidGrab(CustomNetworkHandColliderGrabber grabber)
        {
            distanceGrabCollider.enabled = false;
        }

        private void OnDidUngrab()
        {
            _isGravityEnabled = true;
            throwableCollider.enabled = true;
            ThrowableThrownCallback?.Invoke();
        }
    }
}