using System;
using Cysharp.Threading.Tasks;
using Fusion;
using Fusion.Addons.Physics;
using Fusion.XR.Shared;
using Fusion.XR.Shared.Grabbing.NetworkHandColliderBased;
using Snowballers.Network;
using UnityEngine;
using UnityEngine.Events;

namespace Snowballers
{
    [DefaultExecutionOrder(NetworkHandColliderGrabbable.EXECUTION_ORDER)]
    public class CustomNetworkHandColliderGrabbable : NetworkBehaviour
    {
        public const int ExecutionOrder = CustomNetworkHandColliderGrabber.ExecutionOrder + 10;
        
        [HideInInspector]
        public NetworkTransform networkTransform;
        public NetworkRigidbody3D networkRigidbody;
        
        [Networked] public NetworkBool InitialIsKinematicState { get; set; }
        [Networked] public CustomNetworkHandColliderGrabber CurrentGrabber { get; set; }
        [Networked] private Vector3 LocalPositionOffset { get; set; }
        [Networked] private Quaternion LocalRotationOffset { get; set; }

        public bool IsGrabbed => CurrentGrabber != null;
        public float ThrowVelocityMultiplier => throwVelocityMultiplier;

        [SerializeField] private GrabbingTypeEnum grabbingType;
        [Tooltip("For object with a rigidbody, if true, apply hand velocity on ungrab")]
        [SerializeField] private bool applyVelocityOnRelease = true;
        [SerializeField] private float throwVelocityMultiplier;

        // Velocity computation
        private const int VelocityBufferSize = 5;
        private Vector3 _lastPosition;
        private Quaternion _previousRotation;
        private readonly Vector3[] _lastMoves = new Vector3[VelocityBufferSize];
        private readonly Vector3[] _lastAngularVelocities = new Vector3[VelocityBufferSize];
        private readonly float[] _lastDeltaTime = new float[VelocityBufferSize];
        private int _lastMoveIndex = 0;
        private NetworkBehaviour.ChangeDetector _funChangeDetector;
        private NetworkBehaviour.ChangeDetector _renderChangeDetector;

        private enum GrabbingTypeEnum
        {
            SnapGrab,
            FreeGrab
        }

        [Header("Events")]
        public UnityEvent onDidUngrab = new UnityEvent();
        [Tooltip("Called only for the local grabber, when they may wait for authority before grabbing. onDidGrab will be called on all users")]
        public UnityEvent<CustomNetworkHandColliderGrabber> onWillGrab = new UnityEvent<CustomNetworkHandColliderGrabber>();
        public UnityEvent<CustomNetworkHandColliderGrabber> onDidGrab = new UnityEvent<CustomNetworkHandColliderGrabber>();

        [Header("Advanced options")]
        public bool extrapolateWhileTakingAuthority = true;
        public bool isTakingAuthority = false;

        private Vector3 _localPositionOffsetWhileTakingAuthority;
        private Quaternion _localRotationOffsetWhileTakingAuthority;
        private CustomNetworkHandColliderGrabber _grabberWhileTakingAuthority;

        private enum Status { 
            NotGrabbed,
            Grabbed,
            WillBeGrabbedUponAuthorityReception
        }

        private Status _status = Status.NotGrabbed;

        public Vector3 Velocity
        {
            get
            {
                Vector3 move = Vector3.zero;
                float time = 0;
                for (int i = 0; i < VelocityBufferSize; i++)
                {
                    if (_lastDeltaTime[i] != 0)
                    {
                        move += _lastMoves[i];
                        time += _lastDeltaTime[i];
                    }
                }
                if (time == 0) return Vector3.zero;
                return move / time;
            }
        }

        public Vector3 AngularVelocity
        {
            get
            {
                Vector3 culmulatedAngularVelocity = Vector3.zero;
                int step = 0;
                for (int i = 0; i < VelocityBufferSize; i++)
                {
                    if (_lastDeltaTime[i] != 0)
                    {
                        culmulatedAngularVelocity += _lastAngularVelocities[i];
                        step++;
                    }
                }
                if (step == 0) return Vector3.zero;
                return culmulatedAngularVelocity / step;
            }
        }

        private void Awake()
        {
            networkTransform = GetComponent<NetworkTransform>();
            networkRigidbody = GetComponent<NetworkRigidbody3D>();

        }

        public override void Spawned()
        {
            base.Spawned();
            if (networkRigidbody && Object.HasStateAuthority)
            {
                // Save initial kinematic state for later join player
                InitialIsKinematicState = networkRigidbody.Rigidbody.isKinematic;
            }
            _funChangeDetector = GetChangeDetector(NetworkBehaviour.ChangeDetector.Source.SimulationState);
            _renderChangeDetector = GetChangeDetector(NetworkBehaviour.ChangeDetector.Source.SnapshotFrom);
        }

        public void Ungrab()
        {
            _status = Status.NotGrabbed;
            if (Object.HasStateAuthority)
            {
                CurrentGrabber = null;
            }
        }

        public async UniTask Grab(CustomNetworkHandColliderGrabber newGrabber)
        {
            if (onWillGrab != null) onWillGrab.Invoke(newGrabber);

            switch (grabbingType)
            {
                case GrabbingTypeEnum.SnapGrab:
                    // Snap grabbable position to grabber
                    // _localPositionOffsetWhileTakingAuthority = transform.InverseTransformPoint(newGrabber.transform.position);
                    _localPositionOffsetWhileTakingAuthority = Vector3.zero;
                    _localRotationOffsetWhileTakingAuthority = newGrabber.transform.rotation;
                    break;
                case GrabbingTypeEnum.FreeGrab:
                    // Find grabbable position in grabber referential
                    _localPositionOffsetWhileTakingAuthority = newGrabber.transform.InverseTransformPoint(transform.position);
                    _localRotationOffsetWhileTakingAuthority = Quaternion.Inverse(newGrabber.transform.rotation) * transform.rotation;
                    break;
            }
            
            
            _grabberWhileTakingAuthority = newGrabber;

            // Ask and wait to receive the stateAuthority to move the object
            _status = Status.WillBeGrabbedUponAuthorityReception;
            isTakingAuthority = true;
            await Object.WaitForStateAuthority();
            isTakingAuthority = false;
            if (_status == Status.NotGrabbed)
            {
                // Object has been already ungrabbed while waiting for state authority
                return;
            }
            if (Object.HasStateAuthority == false)
            {
                Debug.LogError("Unable to receive state authority");
                return;
            }
            _status = Status.Grabbed;

            // We waited to have the state authority before setting Networked vars
            LocalPositionOffset = _localPositionOffsetWhileTakingAuthority;
            LocalRotationOffset = _localRotationOffsetWhileTakingAuthority;

            // Update the CurrentGrabber in order to start following position in the FixedUpdateNetwork
            CurrentGrabber = _grabberWhileTakingAuthority;
        }

        void LockObjectPhysics()
        {
            // While grabbed, we disable physics forces on the object, to force a position based tracking
            if (networkRigidbody) networkRigidbody.Rigidbody.isKinematic = true;
        }

        void UnlockObjectPhysics()
        {
            // We restore the default isKinematic state if needed
            if (networkRigidbody) networkRigidbody.Rigidbody.isKinematic = InitialIsKinematicState;

            // We apply release velocity if needed
            if (networkRigidbody && networkRigidbody.Rigidbody.isKinematic == false && applyVelocityOnRelease)
            {
                SetVelocity(Velocity * throwVelocityMultiplier , AngularVelocity);
            }

            // Reset velocity tracking
            for (int i = 0; i < VelocityBufferSize; i++) _lastDeltaTime[i] = 0;
            _lastMoveIndex = 0;
        }

        public void SetVelocity(Vector3 linearVelocity, Vector3 angularVelocity)
        {
            networkRigidbody.Rigidbody.linearVelocity = linearVelocity;
            networkRigidbody.Rigidbody.angularVelocity = angularVelocity;
        }

        bool TryDetectGrabberChange(NetworkBehaviour.ChangeDetector changeDetector, out CustomNetworkHandColliderGrabber previousGrabber, out CustomNetworkHandColliderGrabber currentGrabber)
        {
            previousGrabber = null;
            currentGrabber = null;
            foreach (var changedNetworkedVarName in changeDetector.DetectChanges(this, out var previous, out var current))
            {
                if (changedNetworkedVarName == nameof(CurrentGrabber))
                {
                    var grabberReader = GetBehaviourReader<CustomNetworkHandColliderGrabber>(changedNetworkedVarName);
                    previousGrabber = grabberReader.Read(previous);
                    currentGrabber = grabberReader.Read(current);
                    return true;
                }
            }
            return false;
        }

        public override void FixedUpdateNetwork()
        {
            // Check if the grabber changed
            if (TryDetectGrabberChange(_funChangeDetector, out var previousGrabber, out var currentGrabber))
            {
                if (previousGrabber)
                {
                    // Object ungrabbed
                    UnlockObjectPhysics();
                }
                if (currentGrabber)
                {
                    // Object grabbed
                    LockObjectPhysics();
                }
            }

            // We only update the object position if we have the state authority
            if (!Object.HasStateAuthority) return;

            if (!IsGrabbed) return;
            // Follow grabber, adding position/rotation offsets
            Follow(followedTransform: CurrentGrabber.transform, LocalPositionOffset, LocalRotationOffset);
        }

        private void Update()
        {
            if (Runner)
            {
                // Velocity tracking
                _lastMoves[_lastMoveIndex] = transform.position - _lastPosition;
                _lastAngularVelocities[_lastMoveIndex] = _previousRotation.AngularVelocityChange(transform.rotation, Time.deltaTime);
                _lastDeltaTime[_lastMoveIndex] = Time.deltaTime;
                _lastMoveIndex = (_lastMoveIndex + 1) % 5;
                _lastPosition = transform.position;
                _previousRotation = transform.rotation;
            }
        }

        public override void Render()
        {
            // Check if the grabber changed, to trigger callbacks only (actual grabbing logic in handled in FUN for the state authority)
            // Those callbacks can't be called in FUN, as FUN is not called on proxies, while render is called for everybody
            if (TryDetectGrabberChange(_renderChangeDetector, out var previousGrabber, out var currentGrabber))
            {
                if (previousGrabber)
                {
                    if (onDidUngrab != null) onDidUngrab.Invoke();
                }
                if (currentGrabber)
                {
                    if (onDidGrab != null) onDidGrab.Invoke(currentGrabber);
                }
            }

            if (isTakingAuthority && extrapolateWhileTakingAuthority)
            {
                // If we are currently taking the authority on the object due to a grab, the network info are still not set
                //  but we will extrapolate anyway (if the option extrapolateWhileTakingAuthority is true) to avoid having the grabbed object staying still until we receive the authority
                ExtrapolateWhileTakingAuthority();
                return;
            }

            // No need to extrapolate if the object is not grabbed
            if (!IsGrabbed) return;

            // Extrapolation: Make visual representation follow grabber, adding position/rotation offsets
            // We extrapolate for all users: we know that the grabbed object should follow accuratly the grabber, even if the network position might be a bit out of sync
            Follow(followedTransform: CurrentGrabber.hand.transform, LocalPositionOffset, LocalRotationOffset);
        }

        void ExtrapolateWhileTakingAuthority()
        {
            // No need to extrapolate if the object is not really grabbed
            if (_grabberWhileTakingAuthority == null) return;

            // Extrapolation: Make visual representation follow grabber, adding position/rotation offsets
            // We use grabberWhileTakingAuthority instead of CurrentGrabber as we are currently waiting for the authority transfer: the network vars are not already set, so we use the temporary versions
            Follow(followedTransform: _grabberWhileTakingAuthority.hand.transform, _localPositionOffsetWhileTakingAuthority, _localRotationOffsetWhileTakingAuthority);
        }

        void Follow(Transform followedTransform, Vector3 localPositionOffsetToFollowed, Quaternion localRotationOffsetTofollowed)
        {
            transform.position = followedTransform.TransformPoint(localPositionOffsetToFollowed);
            transform.rotation = followedTransform.rotation * localRotationOffsetTofollowed;
        }
    }
}


