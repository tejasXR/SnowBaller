using System;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkThrowable : NetworkBehaviour
    {
        public event Action ThrowableGrabbedCallback;
        public event Action ThrowableThrownCallback;
        public event Action ThrowableDestroyedCallback;

        [Networked] public PlayerRef ThrowingPlayer { get; set; }

        public CustomNetworkHandColliderGrabbable Grabbable => _grabbable;

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

        private void OnCollisionEnter(Collision other)
        {
            if (Physics.CheckSphere(throwableCollider.transform.position, throwableCollider.radius, collisionMask))
            {
                // Don't do anything if we've colliding with another object our local player has thrown
                var collidingThrowable = other.collider.GetComponentInParent<NetworkThrowable>();
                if (collidingThrowable)
                {
                    if (collidingThrowable.ThrowingPlayer == Runner.LocalPlayer)
                    {
                        return;
                    }
                }
                
                if (shouldDestroyOnCollision)
                {
                    OnCollision(other);
                    Destroy();
                }
            }
        }

        protected virtual void OnCollision(Collision collision) { }

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
            ThrowableGrabbedCallback?.Invoke();
        }

        private void OnDidUngrab()
        {
            SetThrownState();
        }

        public void SetThrownState()
        {
            _isGravityEnabled = true;
            throwableCollider.enabled = true;
            distanceGrabCollider.enabled = false;
            ThrowingPlayer = Runner.LocalPlayer;
            if (ThrowingPlayer == Runner.LocalPlayer)
            {
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("LocalThrowable");
                }
            }
            
            ThrowableThrownCallback?.Invoke();
        }

        public void Destroy()
        {
            ThrowableDestroyedCallback?.Invoke();
            DestroyBehaviour(this); // TEJAS: Does this actually destroy over network? 
            Destroy(gameObject);
        }
    }
}