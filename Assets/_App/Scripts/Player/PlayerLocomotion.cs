using System;
using GorillaLocomotion;
using UnityEngine;

namespace Snowballer
{
    /// <summary>
    /// Based on the Gorilla Tag Movement system by AnotherAxiom
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class PlayerLocomotion : MonoBehaviour
    {
        // private static Player _instance;
        // public static Player Instance { get { return _instance; } }

        [SerializeField] private bool disableMovement = false;
        
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
        
        private Vector3 _lastLeftHandPosition;
        private Vector3 _lastRightHandPosition;
        private Vector3 _lastHeadPosition;
        private Rigidbody _playerRigidBody;

        [Header("Locomotion Variables")] 
        [SerializeField] private float movementAmplifier = 50F;
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

        private const float GravityForce = 9.8F;
        private const float GravityMultiplier = 2F;
        private const float MinimumMovementVelocityThreshold = .1F;

        private Vector3[] _velocityHistory;
        private int _velocityIndex = 0;
        private Vector3 _currentVelocity;
        private Vector3 _denormalizedVelocityAverage;
        private bool _jumpHandIsLeft;
        private Vector3 _lastPosition;
        private bool _wasLeftHandTouching;
        private bool _wasRightHandTouching;

        private void Awake()
        {
            /*if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
            }
            else
            {
                _instance = this;
            }*/
            InitializeValues();
        }

        private void InitializeValues()
        {
            _playerRigidBody = GetComponent<Rigidbody>();
            
            _velocityHistory = new Vector3[velocityHistorySize];
            _lastLeftHandPosition = leftHandFollower.transform.position;
            _lastRightHandPosition = rightHandFollower.transform.position;
            _lastHeadPosition = headCollider.transform.position;
            _lastPosition = transform.position;
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

        /*private Vector3 CurrentLeftHandPosition()
        {
            if ((PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(leftHandTransform, leftHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(leftHandTransform, leftHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }

        private Vector3 CurrentRightHandPosition()
        {
            if ((PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).magnitude < maxArmLength)
            {
                return PositionWithOffset(rightHandTransform, rightHandOffset);
            }
            else
            {
                return headCollider.transform.position + (PositionWithOffset(rightHandTransform, rightHandOffset) - headCollider.transform.position).normalized * maxArmLength;
            }
        }*/

        private Vector3 PositionWithOffset(Transform transformToModify, Vector3 offsetVector)
        {
            return transformToModify.position + transformToModify.rotation * offsetVector;
        }

        private void Update()
        {
            RotateBodyCollider();

            Vector3 leftMovementProjection = Vector3.zero;
            Vector3 rightMovementProjection = Vector3.zero;
            
            var leftHandPosition = CurrentHandPosition(OVRInput.Handedness.LeftHanded);
            var rightHandPosition = CurrentHandPosition(OVRInput.Handedness.RightHanded);
            var gravityForce = Vector3.down * (GravityForce * GravityMultiplier * Time.deltaTime * Time.deltaTime);

            var leftMovementVector = _lastLeftHandPosition - leftHandPosition;
            var leftMovementDirection = GetMovementAlongSurface(leftHandPosition, minimumRaycastDistance * defaultPrecision, leftMovementVector);
            if (leftMovementDirection.HasValue)
            {
                var direction = leftMovementDirection.Value;
                direction.y = 0;

                leftMovementProjection = 
                    Vector3.ProjectOnPlane(direction, Vector3.up);
            }
            
            
            var rightMovementVector = _lastRightHandPosition - rightHandPosition;
            var rightMovementDirection = GetMovementAlongSurface(rightHandPosition, minimumRaycastDistance * defaultPrecision, rightMovementVector);
            if (rightMovementDirection.HasValue)
            {
                var direction = rightMovementDirection.Value;
                direction.y = 0;

                rightMovementProjection = 
                    Vector3.ProjectOnPlane(direction, Vector3.up);
            }
            
            Move(leftMovementProjection + rightMovementProjection /*+ gravityForce*/);

            _lastLeftHandPosition = leftHandPosition;
            _lastRightHandPosition = rightHandPosition;
            
            leftHandFollower.transform.position = _lastLeftHandPosition;
            rightHandFollower.transform.position = _lastRightHandPosition;
        }

        /*private void Update()
        {
            bool leftHandColliding = false;
            bool rightHandColliding = false;
            Vector3 finalPosition;
            Vector3 rigidBodyMovement = Vector3.zero;
            Vector3 firstIterationLeftHand = Vector3.zero;
            Vector3 firstIterationRightHand = Vector3.zero;

            RotateBodyCollider();
            
            var leftHandPosition = CurrentHandPosition(OVRInput.Handedness.LeftHanded);
            var rightHandPosition = CurrentHandPosition(OVRInput.Handedness.RightHanded);

            var gravityForce = Vector3.down * (GravityForce * GravityMultiplier * Time.deltaTime * Time.deltaTime);

            // Left Hand
            Vector3 distanceTraveled = leftHandPosition - _lastLeftHandPosition + gravityForce;

            if (IterativeCollisionSphereCast(_lastLeftHandPosition, minimumRaycastDistance,
                    distanceTraveled, defaultPrecision, out finalPosition, true))
            {
                // This lets you stick to the position you touch, as long as you keep touching the
                // surface this will be the zero point for that hand
                if (_wasLeftHandTouching)
                {
                    firstIterationLeftHand = _lastLeftHandPosition - leftHandPosition;
                }
                else
                {
                    firstIterationLeftHand = finalPosition - leftHandPosition;
                }
                
                _playerRigidBody.linearVelocity = Vector3.zero;

                leftHandColliding = true;
            }

            // Right Hand
            distanceTraveled = rightHandPosition - _lastRightHandPosition + gravityForce;

            if (IterativeCollisionSphereCast(_lastRightHandPosition, minimumRaycastDistance, 
                    distanceTraveled, defaultPrecision, out finalPosition, true))
            {
                if (_wasRightHandTouching)
                {
                    firstIterationRightHand = _lastRightHandPosition - rightHandPosition;
                }
                else
                {
                    firstIterationRightHand = finalPosition - rightHandPosition;
                }

                _playerRigidBody.linearVelocity = Vector3.zero;

                rightHandColliding = true;
            }

            //average or add

            if ((leftHandColliding || _wasLeftHandTouching) && (rightHandColliding || _wasRightHandTouching))
            {
                //this lets you grab stuff with both hands at the same time
                rigidBodyMovement = (firstIterationLeftHand + firstIterationRightHand) / 2;
            }
            else
            {
                rigidBodyMovement = firstIterationLeftHand + firstIterationRightHand;
            }

            //check valid head movement

            if (IterativeCollisionSphereCast(_lastHeadPosition, headCollider.radius,
                    headCollider.transform.position + rigidBodyMovement - _lastHeadPosition,
                    defaultPrecision, out finalPosition, false))
            {
                rigidBodyMovement = finalPosition - _lastHeadPosition;
                
                // Last check to make sure the head won't phase through geometry
                if (Physics.Raycast(_lastHeadPosition, headCollider.transform.position - _lastHeadPosition + rigidBodyMovement, out _, 
                        (headCollider.transform.position - _lastHeadPosition + rigidBodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = _lastHeadPosition - headCollider.transform.position;
                }
            }

            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position += rigidBodyMovement;
            }

            _lastHeadPosition = headCollider.transform.position;

            // Do final left hand position
            distanceTraveled = leftHandPosition - _lastLeftHandPosition;

            if (IterativeCollisionSphereCast(_lastLeftHandPosition, minimumRaycastDistance, 
                    distanceTraveled, defaultPrecision, out finalPosition,
                    !((leftHandColliding || _wasLeftHandTouching) && (rightHandColliding || _wasRightHandTouching))))
            {
                _lastLeftHandPosition = finalPosition;
                leftHandColliding = true;
            }
            else
            {
                _lastLeftHandPosition = leftHandPosition;
            }

            // Do final right hand position
            distanceTraveled = rightHandPosition - _lastRightHandPosition;

            if (IterativeCollisionSphereCast(_lastRightHandPosition, minimumRaycastDistance, 
                    distanceTraveled, defaultPrecision, out finalPosition,
                    !((leftHandColliding || _wasLeftHandTouching) && (rightHandColliding || _wasRightHandTouching))))
            {
                _lastRightHandPosition = finalPosition;
                rightHandColliding = true;
            }
            else
            {
                _lastRightHandPosition = rightHandPosition;
            }

            StoreVelocities();

            if ((rightHandColliding || leftHandColliding) && !disableMovement)
            {
                if (_denormalizedVelocityAverage.magnitude > velocityLimit)
                {
                    if (_denormalizedVelocityAverage.magnitude * jumpMultiplier > maxJumpSpeed)
                    {
                        _playerRigidBody.linearVelocity = _denormalizedVelocityAverage.normalized * maxJumpSpeed;
                    }
                    else
                    {
                        _playerRigidBody.linearVelocity = jumpMultiplier * _denormalizedVelocityAverage;
                    }
                }
            }

            // Check to see if left hand is stuck and we should unstick it
            if (leftHandColliding && (leftHandPosition - _lastLeftHandPosition).magnitude > unStickDistance && 
                !Physics.SphereCast
                (
                    headCollider.transform.position,
                    minimumRaycastDistance * defaultPrecision,
                    leftHandPosition - headCollider.transform.position,
                    out _,
                    (leftHandPosition - headCollider.transform.position).magnitude - minimumRaycastDistance,
                    locomotionEnabledLayers.value)
                )
            {
                _lastLeftHandPosition = leftHandPosition;
                leftHandColliding = false;
            }

            // Check to see if right hand is stuck and we should unstick it
            if (rightHandColliding && (rightHandPosition - _lastRightHandPosition).magnitude > unStickDistance && 
                !Physics.SphereCast
                (
                    headCollider.transform.position,
                    minimumRaycastDistance * defaultPrecision,
                    rightHandPosition - headCollider.transform.position,
                    out _,
                    (rightHandPosition - headCollider.transform.position).magnitude - minimumRaycastDistance,
                    locomotionEnabledLayers.value)
                )
            {
                _lastRightHandPosition = rightHandPosition;
                rightHandColliding = false;
            }

            leftHandFollower.position = _lastLeftHandPosition;
            rightHandFollower.position = _lastRightHandPosition;

            _wasLeftHandTouching = leftHandColliding;
            _wasRightHandTouching = rightHandColliding;
        }*/

        private bool IterativeCollisionSphereCast
        (
            Vector3 startPosition,
            float sphereRadius,
            Vector3 movementVector,
            float precision,
            out Vector3 endPosition,
            bool singleHand
        )
        {
            // First spherecast from the starting position to the final position
            if (CollisionsSphereCast
            (
                startPosition,
                sphereRadius * precision,
                movementVector,
                precision,
                out endPosition,
                out var hitInfo
            ))
            {
                // If we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%,
                // and if you're pushing along a surface while braced with your head, your hand will slide a bit
                // Take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance
                // away to account for moving perpendicular to that surface
                Vector3 firstPosition = endPosition;
                // var gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                // var slipPercentage = gorillaSurface ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
                var movementToProjectedAboveCollisionPlane = 
                    Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal);
                
                if (CollisionsSphereCast
                (
                    endPosition,
                    sphereRadius,
                    movementToProjectedAboveCollisionPlane,
                    precision * precision,
                    out endPosition,
                    out hitInfo
                ))
                {
                    // If we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                
                // If not, try to move closer towards the true point to account for the fact that the movement along the
                // normal of the hit could have moved you away from the surface
                if (CollisionsSphereCast
                (
                    movementToProjectedAboveCollisionPlane + firstPosition,
                    sphereRadius,
                    startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition),
                    precision * precision * precision,
                    out endPosition,
                    out hitInfo
                ))
                {
                    //if we hit, then return the spot we hit
                    return true;
                }

                // This shouldn't really happen, since this means that the sliding motion got you around some corner or
                // something and let you get to your final point. back off because something strange happened, so just don't do the slide
                endPosition = firstPosition;
                return true;
            }
            
            // As kind of a sanity check, try a smaller spherecast. this accounts for times when the original
            // spherecast was already touching a surface so it didn't trigger correctly
            if (CollisionsSphereCast
            (
                startPosition,
                sphereRadius * precision * 0.66f,
                movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f),
                precision * 0.66f,
                out endPosition,
                out hitInfo
            ))
            {
                endPosition = startPosition;
                return true;
            }
            
            endPosition = Vector3.zero;
            return false;
        }

        private Vector3? GetMovementAlongSurface
        (
            Vector3 startPosition,
            float sphereRadius,
            Vector3 movementVector
            // float precision,
            // out Vector3 finalPosition,
            // out RaycastHit hitInfo
        )
        {
            if (Physics.CheckSphere(startPosition, sphereRadius, locomotionEnabledLayers.value))
            {
                return movementVector;
            }
          
            /*{
                // If we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;
            }*/

            return null;

        }

        private bool CollisionsSphereCast
        (
            Vector3 startPosition,
            float sphereRadius,
            Vector3 movementVector,
            float precision,
            out Vector3 finalPosition,
            out RaycastHit hitInfo
        )
        {
            // kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
            // be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial spherecase
            if (Physics.SphereCast
            (
                startPosition,
                sphereRadius * precision,
                movementVector,
                out hitInfo,
                movementVector.magnitude + sphereRadius * (1 - precision),
                locomotionEnabledLayers.value)
            )
            {
                //if we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                // Check a spherecase from the original position to the intended final position
                if (Physics.SphereCast
                (
                    startPosition,
                    sphereRadius * precision * precision,
                    finalPosition - startPosition,
                    out var innerHit,
                    (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision),
                    locomotionEnabledLayers.value
                ))
                {
                    finalPosition = startPosition 
                                    + (finalPosition - startPosition).normalized 
                                    * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    
                    hitInfo = innerHit;
                }
                // Bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
                else if (Physics.Raycast
                (
                    startPosition,
                    finalPosition - startPosition,
                    out innerHit,
                    (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f,
                    locomotionEnabledLayers.value
                ))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }
                return true;
            }
            
            // Anti-clipping through geometry check
            if (Physics.Raycast
            (
                startPosition,
                movementVector,
                out hitInfo,
                movementVector.magnitude + sphereRadius * precision * 0.999f,
                locomotionEnabledLayers.value
            ))
            {
                finalPosition = startPosition;
                return true;
            }

            finalPosition = Vector3.zero;
            return false;
        }

        /*public bool IsHandTouching(bool forLeftHand)
        {
            if (forLeftHand)
            {
                return _wasLeftHandTouching;
            }
            else
            {
                return _wasRightHandTouching;
            }
        }*/

        public bool IsOnGround()
        {
            return true;
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
            var force = moveVector * movementAmplifier;
            _playerRigidBody.AddRelativeForce(force, ForceMode.Acceleration);
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
    }
}