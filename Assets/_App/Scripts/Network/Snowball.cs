using Fusion;
using Fusion.Addons.Physics;
using Fusion.XR.Shared.Grabbing.NetworkHandColliderBased;
using UnityEngine;

namespace Snowballers
{
    [RequireComponent(typeof(NetworkHandColliderGrabbable))]
    public class Snowball : NetworkBehaviour
    {
        [SerializeField] private NetworkRigidbody3D networkRigidbody;
        [SerializeField] private float customGravity = 98F;
        [SerializeField] private Collider collider;
        // [SerializeField] private float throwMultiplier = 60F;
        // [SerializeField] private float throwAngularMultiplier = 3F;
        
        private NetworkHandColliderGrabbable _grabbable;
        // private NetworkHandColliderGrabber _grabber;
        // private Rigidbody _grabberRigidbody;

        // private Vector3 _trackedVelocity;
        // private Vector3 _lastPosition;
        // private bool _shouldTrackVelocity;
        private bool _isGravityEnabled;

        private void Awake()
        {
            _grabbable = GetComponent<NetworkHandColliderGrabbable>();
            
            _grabbable.onDidGrab.AddListener(OnDidGrab);
            _grabbable.onWillGrab.AddListener(OnWillGrab);
            _grabbable.onDidUngrab.AddListener(OnDidUngrab);
        }

        private void Start()
        {
            collider.isTrigger = true;
        }
        
        

        private void Update()
        {
            /*if (_shouldTrackVelocity)
            {
                _trackedVelocity = (transform.position - _lastPosition) / Time.deltaTime;
                _lastPosition = transform.position;
            }*/
        }

        private void FixedUpdate()
        {
            if (_isGravityEnabled)
            {
                var gravityForce = Vector3.down * (customGravity * Time.deltaTime);
                networkRigidbody.Rigidbody.AddForce(gravityForce);
            }
        }

        private void OnWillGrab(NetworkHandColliderGrabber grabber)
        {
            networkRigidbody.Rigidbody.isKinematic = true;
        }
        
        private void OnDidGrab(NetworkHandColliderGrabber grabber)
        {
            // TEJAS: We keep the object kinematic until the object is first grabbed
            networkRigidbody.Rigidbody.isKinematic = false;

            /*_shouldTrackVelocity = true;

            _grabber = grabber;
            _grabberRigidbody = _grabber.GetComponent<Rigidbody>();*/
        }
        
        private void OnDidUngrab()
        {
            networkRigidbody.Rigidbody.isKinematic = false;
            _isGravityEnabled = true;

            collider.isTrigger = false;

            // networkRigidbody.Rigidbody.AddForce(_grabbable.); (_trackedVelocity * throwMultiplier, ForceMode.Impulse);
            // networkRigidbody.angularVelocity = _grabberRigidbody.angularVelocity * throwAngularMultiplier;


            // _grabber = null;
            // _grabberRigidbody = null;
        }
    }
}


