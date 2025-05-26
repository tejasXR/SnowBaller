using System;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.XR.Shared.Rig;
using Oculus.Haptics;
using UnityEngine;

namespace Snowballers.Network
{
    public class NetworkThrowable : NetworkBehaviour
    {
        public event Action ThrowableGrabbedCallback;
        public event Action ThrowableThrownCallback;
        public event Action ThrowableDestroyedCallback;

        [Networked] private int ThrownTick { get; set; }
        [Networked] private Vector3 ThrownPosition { get; set; }
        [Networked] private Vector3 ThrownVelocity { get; set; }
        [Networked] public PlayerRef ThrowingPlayer { get; set; }
        
        public CustomNetworkHandColliderGrabbable Grabbable => _grabbable;

        [SerializeField] private NetworkRigidbody3D networkRigidbody;
        [SerializeField] private LayerMask collisionMask;

        [Space] [SerializeField] private bool shouldDestroyOnCollision;
        [SerializeField] private float customGravity = 400F;
        [Space] [SerializeField] private SphereCollider throwableCollider;
        [SerializeField] private SphereCollider distanceGrabCollider;
        [Space] [SerializeField] private AudioSource sfxAudioSource;
        [SerializeField] private AudioClip grabSfx;
        [SerializeField] private AudioClip throwSfx;
        [Space] [SerializeField] private HapticSource hapticSource;

        private const float GrabHapticForce = .4F; 
        private const float ThrowHapticForce = .8F; 
        
        private CustomNetworkHandColliderGrabbable _grabbable;
        private bool _isGravityEnabled;
        
        public override void Spawned()
        {
            base.Spawned();
            
            _grabbable = GetComponent<CustomNetworkHandColliderGrabbable>();

            _grabbable.onDidGrab.AddListener(OnDidGrab);
            _grabbable.onWillGrab.AddListener(OnWillGrab);
            _grabbable.onDidUngrab.AddListener(OnDidUngrab);
            
            throwableCollider.isTrigger = true;
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
            sfxAudioSource.PlayOneShot(grabSfx);
            distanceGrabCollider.enabled = false;
            
            hapticSource.controller = grabber.hand.side == RigPart.LeftController ? Controller.Left : Controller.Right;
            hapticSource.amplitude = GrabHapticForce;
            hapticSource.Play();
            
            ThrowableGrabbedCallback?.Invoke();
        }

        private void OnDidUngrab()
        {
            var grabber = Grabbable.CurrentGrabber;
            hapticSource.controller = grabber.hand.side == RigPart.LeftController ? Controller.Left : Controller.Right;
            hapticSource.amplitude = ThrowHapticForce;
            hapticSource.Play();
            
            sfxAudioSource.PlayOneShot(throwSfx);

            SetThrownState();
        }

        // Same method can be used both for FUN and Render calls
        protected Vector3 GetMovePosition(float currentTick)
        {
            float time = (currentTick - ThrownTick) * Runner.DeltaTime;

            if (time <= 0f)
                return ThrownPosition;

            return ThrownPosition + ThrownVelocity * time;
        }

        public void SetThrownState()
        {

            _isGravityEnabled = true;
            distanceGrabCollider.enabled = false;
            throwableCollider.isTrigger = false;
            ThrowingPlayer = Runner.LocalPlayer;
            if (ThrowingPlayer == Runner.LocalPlayer)
            {
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    gameObject.transform.GetChild(i).gameObject.layer = LayerMask.NameToLayer("LocalThrowable");
                }
            }
            
            // Save throw data
            ThrownTick = Runner.Tick;
            ThrownPosition = transform.position;
            ThrownVelocity = _grabbable.Velocity;
            
            ThrowableThrownCallback?.Invoke();
        }

        protected void Destroy()
        {
            ThrowableDestroyedCallback?.Invoke();
            Runner.Despawn(Object);
            DestroyBehaviour(this); 
            Destroy(gameObject);
        }
    }
}