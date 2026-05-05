using UnityEngine;

namespace Bit.Robot
{
    /// <summary>
    /// Third-person orbit camera: mouse look, smooth follow, wall collision pull-in.
    /// </summary>
    [DefaultExecutionOrder(-150)]
    public class CameraFollow : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;

        [Header("Orbit")]
        [SerializeField] private float cameraDistance = 5f;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minPitch = -35f;
        [SerializeField] private float maxPitch = 65f;

        [Header("Follow smoothing")]
        [SerializeField] private float followSmoothTime = 0.18f;
        [SerializeField] private Vector3 targetPivotOffset = new Vector3(0f, 1.6f, 0f);

        [Header("Collision")]
        [SerializeField] private LayerMask obstructionMask = ~0;
        [SerializeField] private float collisionSphereRadius = 0.22f;
        [SerializeField] private float obstructionPadding = 0.18f;
        [SerializeField] private float minCameraDistance = 0.85f;

        private float _pitch;
        private float _yaw;
        private Vector3 _smoothVelocity;
        private Vector3 _smoothedPivot;

        private void Awake()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    target = player.transform;
            }

            if (target != null)
            {
                _smoothedPivot = target.position + targetPivotOffset;
            }
        }

        private void Start()
        {
            if (target != null)
            {
                Vector3 pivot = target.position + targetPivotOffset;
                _smoothedPivot = pivot;
                Vector3 fromCamToPivot = pivot - transform.position;
                if (fromCamToPivot.sqrMagnitude > 0.001f)
                {
                    Quaternion look = Quaternion.LookRotation(fromCamToPivot, Vector3.up);
                    Vector3 e = look.eulerAngles;
                    _pitch = e.x;
                    if (_pitch > 180f)
                        _pitch -= 360f;
                    _yaw = e.y;
                }
                else
                {
                    Vector3 euler = transform.eulerAngles;
                    _pitch = euler.x;
                    if (_pitch > 180f)
                        _pitch -= 360f;
                    _yaw = euler.y;
                }
            }
            else
            {
                Vector3 euler = transform.eulerAngles;
                _pitch = euler.x;
                if (_pitch > 180f)
                    _pitch -= 360f;
                _yaw = euler.y;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                var playerGo = GameObject.FindGameObjectWithTag("Player");
                if (playerGo == null)
                    return;

                target = playerGo.transform;
                _smoothedPivot = target.position + targetPivotOffset;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            Vector3 pivotWorld = target.position + targetPivotOffset;
            _smoothedPivot = Vector3.SmoothDamp(_smoothedPivot, pivotWorld, ref _smoothVelocity, followSmoothTime);

            Vector2 look = BitInput.GetMouseLook(mouseSensitivity);
            _yaw += look.x;
            _pitch -= look.y;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            Quaternion orbitRot = Quaternion.Euler(_pitch, _yaw, 0f);
            Vector3 desiredBackward = orbitRot * Vector3.back;
            Vector3 desiredCamPos = _smoothedPivot + desiredBackward * cameraDistance;

            float distance = cameraDistance;
            Vector3 toCam = desiredCamPos - _smoothedPivot;
            float rayLen = toCam.magnitude;
            Vector3 rayDir = rayLen > 0.0001f ? toCam / rayLen : desiredBackward;

            if (Physics.SphereCast(_smoothedPivot, collisionSphereRadius, rayDir, out RaycastHit hit, rayLen,
                    obstructionMask, QueryTriggerInteraction.Ignore))
                distance = Mathf.Max(hit.distance - obstructionPadding, minCameraDistance);

            Vector3 finalPos = _smoothedPivot + rayDir * distance;
            transform.position = finalPos;
            transform.rotation = Quaternion.LookRotation(_smoothedPivot - transform.position, Vector3.up);
        }

        private void OnValidate()
        {
            followSmoothTime = Mathf.Max(0.01f, followSmoothTime);
            cameraDistance = Mathf.Max(minCameraDistance, cameraDistance);
            minCameraDistance = Mathf.Max(0.1f, minCameraDistance);
        }
    }
}
