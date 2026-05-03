using System.Collections;
using UnityEngine;

namespace Bit.Robot
{
    /// <summary>
    /// Third-person rigidbody movement + jump. Starter Assets animator parameters.
    /// Locks body to yaw-only after animator tick to avoid somersaults / barrel rolls in air.
    /// </summary>
    [DefaultExecutionOrder(-200)]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController3D : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintSpeed = 10f;
        [SerializeField] private float acceleration = 35f;
        [SerializeField] private float sprintAccelerationMultiplier = 1.25f;
        [SerializeField] private float rotationSpeed = 720f;

        [Header("Jump")]
        [SerializeField] private float jumpForce = 7f;

        [Header("Ground")]
        [SerializeField] private LayerMask groundMask = ~0;
        [Tooltip("Луч от подошвы вниз: считаем землю, если попадание ближе этого расстояния (метры).")]
        [SerializeField] private float groundedRayLength = 0.85f;
        [SerializeField] private float groundedMaxHitDistance = 0.55f;
        [SerializeField] private float footProbeLift = 0.07f;
        [SerializeField] private float groundStickSkin = 0.05f;
        [SerializeField] private float groundStickPullAccel = 280f;
        [SerializeField] private float groundStickMaxPullSpeed = 8f;
        [SerializeField] private bool snapFeetOnSpawn = true;
        [SerializeField] private float snapRayHeight = 12f;
        [SerializeField] private float snapSkin = 0.03f;
        [SerializeField] private float snapMaxCorrection = 10f;

        [Header("Body (no somersaults)")]
        [SerializeField] private bool lockPitchAndRoll = true;

        [Header("Animator (Starter Assets Kyle)")]
        [SerializeField] private bool driveAnimator = true;
        [SerializeField] private float animSpeedSmoothing = 12f;
        [SerializeField] private float fallAnimTimeout = 0.15f;
        [SerializeField] private float fallAnimVelocity = -0.5f;
        [SerializeField] private bool analogMotionSpeed;
        [SerializeField] private bool useSimpleAirAnimator;

        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        private Rigidbody _rb;
        private CapsuleCollider _cap;
        private bool _grounded;
        private bool _jumpBuffered;
        private float _jumpBufferTime;

        private Animator _animator;
        private bool _hasAnimator;
        private float _animSpeedBlend;
        private float _fallTimer;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

        private void Awake()
        {
            DetachTransformIfUnderCamera(transform);
            DisableConflictingMovement();

            if (!TryGetComponent(out _rb))
                _rb = gameObject.AddComponent<Rigidbody>();

            _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            _rb.interpolation = RigidbodyInterpolation.Interpolate;
            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _rb.useGravity = true;

            if (!TryGetComponent(out _cap))
            {
                _cap = gameObject.AddComponent<CapsuleCollider>();
                _cap.center = new Vector3(0f, 0.93f, 0f);
                _cap.radius = 0.28f;
                _cap.height = 1.8f;
            }

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            CacheAnimator();
            AssignAnimatorParameterIDs();
        }

        private void CacheAnimator()
        {
            _hasAnimator = TryGetComponent(out _animator);
            if (!_hasAnimator)
            {
                _animator = GetComponentInChildren<Animator>(true);
                _hasAnimator = _animator != null;
            }
        }

        private void AssignAnimatorParameterIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
        }

        private void Start()
        {
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            if (snapFeetOnSpawn)
                StartCoroutine(SnapFeetAfterPhysics());
        }

        private IEnumerator SnapFeetAfterPhysics()
        {
            yield return new WaitForFixedUpdate();
            SnapFeetToGroundBelow();
        }

        private void SnapFeetToGroundBelow()
        {
            if (_cap == null || _rb == null)
                return;

            Physics.SyncTransforms();

            LayerMask mask = groundMask.value != 0 ? groundMask : Physics.AllLayers;
            Vector3 origin = transform.position + Vector3.up * snapRayHeight;
            var hits = Physics.RaycastAll(origin, Vector3.down, snapRayHeight + snapMaxCorrection, mask,
                QueryTriggerInteraction.Ignore);
            if (hits.Length == 0)
                return;

            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit groundHit = default;
            bool found = false;
            foreach (RaycastHit h in hits)
            {
                if (h.collider.attachedRigidbody == _rb)
                    continue;

                groundHit = h;
                found = true;
                break;
            }

            if (!found)
                return;

            Physics.SyncTransforms();
            float bottomY = _cap.bounds.min.y;
            float deltaY = bottomY - groundHit.point.y - snapSkin;
            if (Mathf.Abs(deltaY) > 0.0005f && Mathf.Abs(deltaY) < snapMaxCorrection)
            {
                Vector3 p = transform.position;
                p.y -= deltaY;
                transform.position = p;
                Physics.SyncTransforms();
                Vector3 v = _rb.linearVelocity;
                if (Mathf.Abs(v.y) < 0.1f)
                    _rb.linearVelocity = new Vector3(v.x, 0f, v.z);
            }
        }

        private static void DetachTransformIfUnderCamera(Transform t)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
            {
                if (p.GetComponent<Camera>() != null)
                {
                    t.SetParent(null, true);
                    return;
                }
            }
        }

        private void DisableConflictingMovement()
        {
            var cc = GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            foreach (var mb in GetComponents<MonoBehaviour>())
            {
                if (mb == null || mb == this)
                    continue;

                switch (mb.GetType().Name)
                {
                    case "ThirdPersonController":
                    case "StarterAssetsInputs":
                    case "BasicRigidBodyPush":
                    case "PlayerInput":
                        mb.enabled = false;
                        break;
                }
            }
        }

        private void Update()
        {
            _grounded = IsGroundedRay(out _);

            if (BitInput.GetJumpDown())
            {
                _jumpBuffered = true;
                _jumpBufferTime = 0.22f;
            }
            else if (_jumpBuffered)
            {
                _jumpBufferTime -= Time.deltaTime;
                if (_jumpBufferTime <= 0f)
                    _jumpBuffered = false;
            }

            if (driveAnimator)
                UpdateAnimator();
        }

        private void LateUpdate()
        {
            if (lockPitchAndRoll)
                FlattenBodyRotationToYaw();
        }

        private void FlattenBodyRotationToYaw()
        {
            Vector3 e = transform.eulerAngles;
            float y = e.y;
            transform.rotation = Quaternion.Euler(0f, y, 0f);
        }

        private void UpdateAnimator()
        {
            if (!_hasAnimator || _animator == null || !_animator.enabled)
                return;

            Vector3 planarVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            float speedMag = planarVel.magnitude;
            float sprintCap = Mathf.Max(moveSpeed, sprintSpeed);
            _animSpeedBlend = Mathf.Lerp(_animSpeedBlend, Mathf.Min(speedMag, sprintCap + 0.5f),
                Time.deltaTime * animSpeedSmoothing);

            Vector2 moveInput = BitInput.GetMoveAxesSmoothed();
            float motionSpeed = 0f;
            if (moveInput.sqrMagnitude > 0.01f)
                motionSpeed = analogMotionSpeed ? Mathf.Clamp01(moveInput.magnitude) : 1f;

            _animator.SetFloat(_animIDSpeed, _animSpeedBlend);
            _animator.SetFloat(_animIDMotionSpeed, motionSpeed);
            _animator.SetBool(_animIDGrounded, _grounded);

            if (_grounded)
            {
                _fallTimer = 0f;
                _animator.SetBool(_animIDJump, false);
                _animator.SetBool(_animIDFreeFall, false);
            }
            else
            {
                _fallTimer += Time.deltaTime;
                if (useSimpleAirAnimator)
                    _animator.SetBool(_animIDFreeFall, false);
                else if (_fallTimer >= fallAnimTimeout && _rb.linearVelocity.y < fallAnimVelocity)
                    _animator.SetBool(_animIDFreeFall, true);
            }
        }

        private void FixedUpdate()
        {
            Physics.SyncTransforms();

            bool groundedPhy = IsGroundedRay(out RaycastHit groundHit);
            ApplyGroundStick(groundedPhy, groundHit);

            if (_jumpBuffered && groundedPhy)
            {
                Vector3 vJump = _rb.linearVelocity;
                vJump.y = 0f;
                _rb.linearVelocity = vJump;
                _rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                _jumpBuffered = false;
                if (driveAnimator && _hasAnimator && _animator != null)
                    _animator.SetBool(_animIDJump, true);
            }

            Vector2 axes = BitInput.GetMoveAxesSmoothed();
            Vector3 moveDir = GetCameraRelativeDirection(axes.x, axes.y);
            bool sprint = BitInput.GetSprintHeld();
            float targetSpeed = sprint ? sprintSpeed : moveSpeed;
            float accel = sprint ? acceleration * sprintAccelerationMultiplier : acceleration;
            ApplyHorizontalMovement(moveDir, targetSpeed, accel);

            if (moveDir.sqrMagnitude > 0.0001f)
                RotateTowards(moveDir);
        }

        private Vector3 GetCameraRelativeDirection(float horizontal, float vertical)
        {
            if (cameraTransform == null)
                return Vector3.zero;

            Vector3 forward = cameraTransform.forward;
            forward.y = 0f;
            forward.Normalize();

            Vector3 right = cameraTransform.right;
            right.y = 0f;
            right.Normalize();

            Vector3 dir = forward * vertical + right * horizontal;
            if (dir.sqrMagnitude > 1f)
                dir.Normalize();

            return dir;
        }

        private void ApplyHorizontalMovement(Vector3 moveDir, float targetMoveSpeed, float accel)
        {
            Vector3 planarVel = new Vector3(_rb.linearVelocity.x, 0f, _rb.linearVelocity.z);
            Vector3 targetVel = moveDir * targetMoveSpeed;
            Vector3 newPlanar = Vector3.MoveTowards(planarVel, targetVel, accel * Time.fixedDeltaTime);

            _rb.linearVelocity = new Vector3(newPlanar.x, _rb.linearVelocity.y, newPlanar.z);
        }

        private Vector3 GetFootRayOrigin()
        {
            if (_cap == null)
                _cap = GetComponent<CapsuleCollider>();

            Vector3 localBottom = _cap.center + Vector3.down * (_cap.height * 0.5f);
            Vector3 footBottom = transform.TransformPoint(localBottom);
            return footBottom + Vector3.up * footProbeLift;
        }

        /// <summary>
        /// Надёжная проверка земли: луч от уровня ступней; без этого прыжок и прижим не срабатывают при «парении».
        /// </summary>
        private bool IsGroundedRay(out RaycastHit bestHit)
        {
            bestHit = default;
            if (_cap == null)
                _cap = GetComponent<CapsuleCollider>();

            LayerMask mask = groundMask.value != 0 ? groundMask : Physics.AllLayers;
            Vector3 origin = GetFootRayOrigin();

            int count = Physics.RaycastNonAlloc(origin, Vector3.down, RaycastHitsBuffer, groundedRayLength, mask,
                QueryTriggerInteraction.Ignore);

            float closest = float.MaxValue;
            int chosen = -1;
            for (int i = 0; i < count; i++)
            {
                RaycastHit h = RaycastHitsBuffer[i];
                if (h.collider == null || h.collider.attachedRigidbody == _rb)
                    continue;
                if (h.distance < closest)
                {
                    closest = h.distance;
                    chosen = i;
                }
            }

            if (chosen < 0)
                return false;

            bestHit = RaycastHitsBuffer[chosen];
            if (bestHit.distance > groundedMaxHitDistance)
                return false;

            return _rb.linearVelocity.y < 0.65f;
        }

        private static readonly RaycastHit[] RaycastHitsBuffer = new RaycastHit[8];

        private void ApplyGroundStick(bool grounded, RaycastHit hit)
        {
            if (!grounded || _cap == null)
                return;

            if (_rb.linearVelocity.y > 0.12f)
                return;

            float gap = hit.distance - groundStickSkin;
            if (gap <= 0f || gap > 0.42f)
                return;

            float force = Mathf.Clamp(gap * groundStickPullAccel, 0f, 950f);
            _rb.AddForce(Vector3.down * force, ForceMode.Acceleration);

            Vector3 v = _rb.linearVelocity;
            if (v.y < -groundStickMaxPullSpeed)
            {
                v.y = -groundStickMaxPullSpeed;
                _rb.linearVelocity = v;
            }
        }

        private void RotateTowards(Vector3 moveDir)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (_cap == null)
                _cap = GetComponent<CapsuleCollider>();
            if (_cap == null)
                return;

            Gizmos.color = Color.green;
            Vector3 o = Application.isPlaying ? GetFootRayOrigin() : transform.TransformPoint(_cap.center + Vector3.down * (_cap.height * 0.5f)) + Vector3.up * footProbeLift;
            Gizmos.DrawLine(o, o + Vector3.down * groundedRayLength);
        }

#if UNITY_EDITOR
        private void Reset()
        {
            if (!TryGetComponent(out Rigidbody rb))
                rb = gameObject.AddComponent<Rigidbody>();

            rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;
        }
#endif
    }
}
