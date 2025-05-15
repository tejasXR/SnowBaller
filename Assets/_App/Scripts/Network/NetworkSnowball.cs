using System;
using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

namespace Snowballers.Network
{
    [RequireComponent(typeof(CustomNetworkHandColliderGrabbable))]
    public class NetworkSnowball : NetworkBehaviour
    {
        [SerializeField] private NetworkRigidbody3D networkRigidbody;
        [SerializeField] private float customGravity = 98F;
        [Space]
        [SerializeField] private Transform visualAffordanceSphere;
        [SerializeField] private float visualAffordanceRotationSpeed = 10F;
        [Space]
        [SerializeField] private SphereCollider sphereCollider;
        [SerializeField] private SphereCollider distanceGrabCollider;
        
        private CustomNetworkHandColliderGrabbable _grabbable;
        private bool _isGravityEnabled;

        private void Awake()
        {
            _grabbable = GetComponent<CustomNetworkHandColliderGrabbable>();
            
            _grabbable.onDidGrab.AddListener(OnDidGrab);
            _grabbable.onWillGrab.AddListener(OnWillGrab);
            _grabbable.onDidUngrab.AddListener(OnDidUngrab);
        }

        private void Start()
        {
            sphereCollider.enabled = false;
        }

        private void Update()
        {
            visualAffordanceSphere.transform.Rotate(Vector3.up, Time.deltaTime * visualAffordanceRotationSpeed);
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
            sphereCollider.enabled = true;
        }
    }
}