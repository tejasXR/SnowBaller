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

            return headCollider.transform.position + (PositionWithOffset(handTransform, handOffset) - headCollider.transform.position).normalized * maxArmLength;
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
            bool leftHandColliding = false;
            bool rightHandColliding = false;
            Vector3 finalPosition;
            Vector3 rigidBodyMovement = Vector3.zero;
            Vector3 firstIterationLeftHand = Vector3.zero;
            Vector3 firstIterationRightHand = Vector3.zero;

            RotateBodyCollider();
            
            var leftHandPosition = CurrentHandPosition(OVRInput.Handedness.LeftHanded);
            var rightHandPosition = CurrentHandPosition(OVRInput.Handedness.RightHanded);

            // Left Hand
            Vector3 distanceTraveled = leftHandPosition - _lastLeftHandPosition + Vector3.down * (2f * 9.8f * Time.deltaTime * Time.deltaTime);

            if (IterativeCollisionSphereCast(_lastLeftHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition, true))
            {
                //this lets you stick to the position you touch, as long as you keep touching the surface this will be the zero point for that hand
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

            // Right hand
            distanceTraveled = rightHandPosition - _lastRightHandPosition + Vector3.down * (2f * 9.8f * Time.deltaTime * Time.deltaTime);

            if (IterativeCollisionSphereCast(_lastRightHandPosition, minimumRaycastDistance, distanceTraveled,
                    defaultPrecision, out finalPosition, true))
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
                //last check to make sure the head won't phase through geometry
                if (Physics.Raycast(_lastHeadPosition, headCollider.transform.position - _lastHeadPosition + rigidBodyMovement, out _, (headCollider.transform.position - _lastHeadPosition + rigidBodyMovement).magnitude + headCollider.radius * defaultPrecision * 0.999f, locomotionEnabledLayers.value))
                {
                    rigidBodyMovement = _lastHeadPosition - headCollider.transform.position;
                }
            }

            if (rigidBodyMovement != Vector3.zero)
            {
                transform.position += rigidBodyMovement;
            }

            _lastHeadPosition = headCollider.transform.position;

            //do final left hand position

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

            //do final right hand position

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

            //check to see if left hand is stuck and we should unstick it

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

            //check to see if right hand is stuck and we should unstick it

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
        }

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
            if (CollisionsSphereCast(startPosition, sphereRadius * precision, movementVector, precision, out endPosition, out var hitInfo))
            {
                // If we hit a surface, do a bit of a slide. this makes it so if you grab with two hands you don't stick 100%,
                // and if you're pushing along a surface while braced with your head, your hand will slide a bit
                // Take the surface normal that we hit, then along that plane, do a spherecast to a position a small distance
                // away to account for moving perpendicular to that surface
                Vector3 firstPosition = endPosition;
                var gorillaSurface = hitInfo.collider.GetComponent<Surface>();
                var slipPercentage = gorillaSurface ? gorillaSurface.slipPercentage : (!singleHand ? defaultSlideFactor : 0.001f);
                var movementToProjectedAboveCollisionPlane = Vector3.ProjectOnPlane(startPosition + movementVector - firstPosition, hitInfo.normal) * slipPercentage;
                if (CollisionsSphereCast(endPosition, sphereRadius, movementToProjectedAboveCollisionPlane, precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit trying to move perpendicularly, stop there and our end position is the final spot we hit
                    return true;
                }
                //if not, try to move closer towards the true point to account for the fact that the movement along the normal of the hit could have moved you away from the surface

                if (CollisionsSphereCast(movementToProjectedAboveCollisionPlane + firstPosition, sphereRadius, startPosition + movementVector - (movementToProjectedAboveCollisionPlane + firstPosition), precision * precision * precision, out endPosition, out hitInfo))
                {
                    //if we hit, then return the spot we hit
                    return true;
                }

                //this shouldn't really happe, since this means that the sliding motion got you around some corner or something and let you get to your final point. back off because something strange happened, so just don't do the slide
                endPosition = firstPosition;
                return true;
            }
            //as kind of a sanity check, try a smaller spherecast. this accounts for times when the original spherecast was already touching a surface so it didn't trigger correctly

            if (CollisionsSphereCast(startPosition, sphereRadius * precision * 0.66f, movementVector.normalized * (movementVector.magnitude + sphereRadius * precision * 0.34f), precision * 0.66f, out endPosition, out hitInfo))
            {
                endPosition = startPosition;
                return true;
            }
            
            endPosition = Vector3.zero;
            return false;
        }

        private bool CollisionsSphereCast(Vector3 startPosition, float sphereRadius, Vector3 movementVector, float precision, out Vector3 finalPosition, out RaycastHit hitInfo)
        {
            //kind of like a souped up spherecast. includes checks to make sure that the sphere we're using, if it touches a surface, is pushed away the correct distance (the original sphereradius distance). since you might
            //be pushing into sharp corners, this might not always be valid, so that's what the extra checks are for

            //initial spherecase
            RaycastHit innerHit;
            if (Physics.SphereCast(startPosition, sphereRadius * precision, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * (1 - precision), locomotionEnabledLayers.value))
            {
                //if we hit, we're trying to move to a position a sphereradius distance from the normal
                finalPosition = hitInfo.point + hitInfo.normal * sphereRadius;

                //check a spherecase from the original position to the intended final position
                if (Physics.SphereCast(startPosition, sphereRadius * precision * precision, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * (1 - precision * precision), locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition + (finalPosition - startPosition).normalized * Mathf.Max(0, hitInfo.distance - sphereRadius * (1f - precision * precision));
                    hitInfo = innerHit;
                }
                //bonus raycast check to make sure that something odd didn't happen with the spherecast. helps prevent clipping through geometry
                else if (Physics.Raycast(startPosition, finalPosition - startPosition, out innerHit, (finalPosition - startPosition).magnitude + sphereRadius * precision * precision * 0.999f, locomotionEnabledLayers.value))
                {
                    finalPosition = startPosition;
                    hitInfo = innerHit;
                    return true;
                }
                return true;
            }
            //anti-clipping through geometry check

            if (Physics.Raycast(startPosition, movementVector, out hitInfo, movementVector.magnitude + sphereRadius * precision * 0.999f, locomotionEnabledLayers.value))
            {
                finalPosition = startPosition;
                return true;
            }

            finalPosition = Vector3.zero;
            return false;
        }

        public bool IsHandTouching(bool forLeftHand)
        {
            if (forLeftHand)
            {
                return _wasLeftHandTouching;
            }
            else
            {
                return _wasRightHandTouching;
            }
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

