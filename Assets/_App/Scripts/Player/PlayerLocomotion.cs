using System;
using Oculus.Haptics;
using UnityEngine;

namespace Snowballers
{
    /// <summary>
    /// Based on the Gorilla Tag Movement system by AnotherAxiom
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerLocomotion : MonoBehaviour
    {
        [SerializeField] private bool disableMovement = true;
        [SerializeField] private SlopeCalculation slopeCalculation;
        
        [Header("Colliders")]
        [SerializeField] private SphereCollider headCollider;
        [SerializeField] private CapsuleCollider bodyCollider;

        [Header("Transforms")]
        [SerializeField] private Transform leftHandTransform;
        [SerializeField] private Transform leftHandFollower;
        [SerializeField] private Transform rightHandTransform;
        [SerializeField] private Transform rightHandFollower;
        
        [Header("Offsets")]
        [SerializeField] private Vector3 rightHandOffset;
        [SerializeField] private Vector3 leftHandOffset;

        [Header("Haptics")] 
        [SerializeField] private HapticSource leftHandHaptics;
        [SerializeField] private HapticSource rightHandHaptics;

        [Header("Audio")] 
        [SerializeField] private float windSfxAmplifier;
        [SerializeField] private AudioSource windSfx;

        private Vector3 LinearVelocity => _playerRigidBody.linearVelocity;
        
        private Vector3 _lastLeftHandPosition;
        private Vector3 _lastRightHandPosition;
        private Vector3 _lastHeadPosition;
        private Rigidbody _playerRigidBody;

        [Header("Locomotion Variables")] 
        [SerializeField] private float movementAmplifier = 50F;
        [SerializeField] [Range(0,.01F)] private float handDragAmplifier = .005F;
        [SerializeField] private float jumpDirectionAmplifier = 30F;
        [SerializeField] private float minimumMovementVelocityThreshold = .3F;
        // [SerializeField] private float minimumJumpVelocityThreshold = .3F;
        [SerializeField] private float maximumJumpVelocity = 5F;

        [SerializeField] private LayerMask locomotionEnabledLayers;
        [SerializeField] private int velocityHistorySize = 8;
        [SerializeField] private float maxArmLength = 1.5f;
        [SerializeField] private float unStickDistance = 1f;
        [SerializeField] private float velocityLimit = .4F;
        [SerializeField] private float maxJumpSpeed = 6.5F;
        [SerializeField] private float jumpMultiplier = 1.1F;
        [SerializeField] private float minimumRaycastDistance = 0.05f;
        [SerializeField] private float defaultSlideFactor = 0.03f;
        [SerializeField] private float defaultPrecision = 0.995f;

        [SerializeField] private float GravityForce = 1960;
        [SerializeField] private float GravityMultiplier = .95F;
        // private const float MinimumMovementVelocityThreshold = .3F;

        private Vector3[] _velocityHistory;
        private int _velocityIndex = 0;
        private Vector3 _currentVelocity;
        private Vector3 _denormalizedVelocityAverage;
        private bool _jumpHandIsLeft;
        private Vector3 _lastPosition;
        private bool _wasLeftHandTouching;
        private bool _wasRightHandTouching;

        private bool _rightHandContact;
        private bool _leftHandContact;

        private void Awake()
        {
            _playerRigidBody = GetComponent<Rigidbody>();
            
            _velocityHistory = new Vector3[velocityHistorySize];
            _lastLeftHandPosition = leftHandFollower.transform.position;
            _lastRightHandPosition = rightHandFollower.transform.position;
            _lastHeadPosition = headCollider.transform.position;
            _lastPosition = transform.position;

            // IMPORTANT: Disable movement on Awake
            // Will re-enable movement once player manager registers networked player instance
            disableMovement = true;
        }

        private void Update()
        {
            if (disableMovement)
            {
                return;
            }
            
            RotateBodyCollider();
            PlayWindSfx();

            Vector3 leftMovementProjection = Vector3.zero;
            Vector3 rightMovementProjection = Vector3.zero;
            
            var leftHandPosition = CurrentHandPosition(OVRInput.Handedness.LeftHanded);
            var rightHandPosition = CurrentHandPosition(OVRInput.Handedness.RightHanded);
            var gravityForce = slopeCalculation.IsOnSlope 
                ? Vector3.zero 
                : Vector3.down * (GravityForce * GravityMultiplier * Time.deltaTime);

            var leftMovementVector = _lastLeftHandPosition - leftHandPosition;
            var leftMovementDirection = GetMovementVectorForHand(leftHandPosition, minimumRaycastDistance * defaultPrecision, leftMovementVector, OVRInput.Handedness.LeftHanded);
            if (leftMovementDirection.HasValue)
            {
                var direction = leftMovementDirection.Value;

                leftMovementProjection = direction * movementAmplifier;
                
                leftMovementProjection.y = Mathf.Clamp(leftMovementProjection.y, 0, maximumJumpVelocity);
                leftMovementProjection.y *= jumpMultiplier;

                leftHandHaptics.amplitude = leftMovementProjection.magnitude;
                leftHandHaptics.Play();
            }
            else
            {
                leftHandHaptics.Stop();
            }
            
            
            var rightMovementVector = _lastRightHandPosition - rightHandPosition;
            var rightMovementDirection = GetMovementVectorForHand(rightHandPosition, minimumRaycastDistance * defaultPrecision, rightMovementVector, OVRInput.Handedness.RightHanded);
            if (rightMovementDirection.HasValue)
            {
                var direction = rightMovementDirection.Value;

                // TEJAS: We don't project our Vector on a plane like the original GT locomotion
                // As a result, our move feels a bit more responsive and natural
                rightMovementProjection = direction * movementAmplifier;

                // Jump height
                rightMovementProjection.y = Mathf.Clamp(rightMovementProjection.y, 0, maximumJumpVelocity);
                rightMovementProjection.y *= jumpMultiplier;

                rightHandHaptics.amplitude = rightMovementProjection.magnitude;
                rightHandHaptics.Play();
            }
            else
            {
                rightHandHaptics.Stop();
            }

            var combinedVectorMovement = leftMovementProjection + rightMovementProjection + gravityForce;
            Move(combinedVectorMovement);

            _lastLeftHandPosition = leftHandPosition;
            _lastRightHandPosition = rightHandPosition;
            
            leftHandFollower.transform.position = _lastLeftHandPosition;
            rightHandFollower.transform.position = _lastRightHandPosition;
        }
        
        public void ToggleMovement(bool shouldEnableMovement)
        {
            disableMovement = !shouldEnableMovement;
        }

        private Vector3 CurrentHandPosition(OVRInput.Handedness handedness)
        {
            var handTransform = handedness == OVRInput.Handedness.LeftHanded ? leftHandTransform : rightHandTransform;
            var handOffset = handedness == OVRInput.Handedness.LeftHanded ? leftHandOffset : rightHandOffset;
            
            if ((PositionWithOffset(handTransform, handOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(handTransform, handOffset);
            }

            return headCollider.transform.position 
                   + (PositionWithOffset(handTransform, handOffset) 
                      - headCollider.transform.position).normalized * maxArmLength;
        }

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private Vector3? GetMovementVectorForHand
        (
            Vector3 startPosition,
            float sphereRadius,
            Vector3 movementVector,
            OVRInput.Handedness handedness
        )
        {
            // If we are touching the ground
            if (Physics.CheckSphere(startPosition, sphereRadius, locomotionEnabledLayers.value))
            {
                switch (handedness)
                {
                    case OVRInput.Handedness.LeftHanded:
                        _leftHandContact = true;
                        break;
                    case OVRInput.Handedness.RightHanded:
                        _rightHandContact = true;
                        break;
                }
                
                // If we are moving our arms enough OR the player itself is moving slowly, increase speed
                if (movementVector.magnitude > minimumMovementVelocityThreshold 
                    || LinearVelocity.magnitude < minimumMovementVelocityThreshold)
                {
                    return movementVector;
                }

                // Else, decrease speed
                if (LinearVelocity.magnitude > 0)
                {
                    return -LinearVelocity * handDragAmplifier;
                }
            }
            else
            {
                _leftHandContact = false;
                _rightHandContact = false;
            }
            
            return null;
        }
        
        public void Turn(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            _denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * _denormalizedVelocityAverage;
            for (int i = 0; i < _velocityHistory.Length; i++)
            {
                _velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * _velocityHistory[i];
            }
        }

        private void SetVelocity(Vector3 velocityVector)
        {
            _playerRigidBody.linearVelocity = velocityVector;
        }

        private void Move(Vector3 moveVector)
        {
            _playerRigidBody.AddForce(moveVector, ForceMode.Acceleration);
        }

        private void StoreVelocities()
        {
            _velocityIndex = (_velocityIndex + 1) % velocityHistorySize;
            Vector3 oldestVelocity = _velocityHistory[_velocityIndex];
            _currentVelocity = (transform.position - _lastPosition) / Time.deltaTime;
            _denormalizedVelocityAverage += (_currentVelocity - oldestVelocity) / (float)velocityHistorySize;
            _velocityHistory[_velocityIndex] = _currentVelocity;
            _lastPosition = transform.position;
        }

        private void RotateBodyCollider()
        {
            bodyCollider.transform.eulerAngles = new Vector3(0, headCollider.transform.eulerAngles.y, 0);
        }

        private void PlayWindSfx()
        {
            if (!windSfx.isPlaying)
            {
                windSfx.Play();
            }
            
            windSfx.volume = windSfxAmplifier * _playerRigidBody.linearVelocity.magnitude;
        }
    }
}