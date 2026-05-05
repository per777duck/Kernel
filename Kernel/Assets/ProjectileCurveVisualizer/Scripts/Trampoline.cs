using UnityEngine;
using StarterAssets;

namespace ProjectileCurveVisualizerSystem
{
    public class Trampoline : MonoBehaviour
    {
        [Range(0.0f, 1.0f)]
        public float bounciness = 0.99f;
        [Header("Character Controller Boost")]
        public bool applyCharacterControllerBoost = true;
        public float characterControllerBounceVelocity = 12.5f;
        [Range(0.0f, 1.0f)]
        public float tiltInfluence = 0.7f;
        [Min(0.0f)]
        public float lateralBounceMultiplier = 1.0f;
        public float minUpBoost = 10.5f;
        public float bounceCooldown = 0.2f;
        [Header("Dynamic Bounce From Fall")]
        public bool useDynamicBounceFromFall = true;
        [Min(0.0f)]
        public float bounceHeightMultiplier = 1.25f;
        [Min(0.0f)]
        public float extraBounceHeight = 0.0f;
        [Min(0.01f)]
        public float controllerGravityMagnitude = 15.0f;

        private Transform trampolineTransform;

        private bool touched = false;
        private Transform objectTransform;
        private Rigidbody objectRigidbody;
        private float currentClosestDistance = 999999.0f;
        private float closestDistance = 999999.0f;
        private Vector3 objectVelocity;

        private float incidenceVectorLength;
        private Vector3 incidenceVector;
        private Vector3 reflectionVector;

        // Output variables of method VisualizeProjectileCurve
        private Vector3 updatedProjectileStartPosition;
        private RaycastHit hit;

        public float projectileCurveStartPositionYOffset = 0.1f;
        public ProjectileCurveVisualizer projectileCurveVisualizer;

        private Trampoline nextTrampoline;
        private float lastCharacterControllerBounceTime = -999.0f;

        void Start()
        {
            trampolineTransform = this.transform;
        }

        void Update()
        {
            if (touched && objectRigidbody != null && objectRigidbody.linearVelocity.y < 0.0f)
            {
                currentClosestDistance = objectTransform.position.y - trampolineTransform.position.y;
                if (currentClosestDistance < closestDistance)
                {
                    closestDistance = currentClosestDistance;
                    objectVelocity = objectRigidbody.linearVelocity;
                }
            }
        }

        public void VisualizeOutgoingProjectileCurve(Vector3 hitPosition, Vector3 incidenceVelocity, float projectileRadius, float distanceOffsetAboveHitPosition, bool debugMode)
        {
            if (projectileCurveVisualizer)
            {
                projectileCurveVisualizer.VisualizeProjectileCurve(hitPosition + Vector3.up * projectileCurveStartPositionYOffset, 0.0f, CalculateReflectionVector(incidenceVelocity) * bounciness, projectileRadius, distanceOffsetAboveHitPosition, debugMode, out updatedProjectileStartPosition, out hit);

                if (projectileCurveVisualizer.hitObjectTransform)
                {
                    // Check if the hit object is a trampoline
                    if (projectileCurveVisualizer.hitObjectTransform.name == "Trampoline")
                    {
                        nextTrampoline = projectileCurveVisualizer.hitObjectTransform.GetComponent<Trampoline>();
                        nextTrampoline.VisualizeOutgoingProjectileCurve(projectileCurveVisualizer.hitPosition, projectileCurveVisualizer.projectileVelocityWhenHit, projectileRadius, 0.1f, true);
                    }
                }
            }
        }

        public void HideProjectileCurve()
        {
            if (projectileCurveVisualizer)
            {
                projectileCurveVisualizer.HideProjectileCurve();

                if (nextTrampoline)
                {
                    nextTrampoline.HideProjectileCurve();
                    nextTrampoline = null;
                }
            }
        }

        Vector3 CalculateReflectionVector(Vector3 incidenceVector)
        {
            incidenceVectorLength = incidenceVector.magnitude;
            incidenceVector = incidenceVector.normalized;

            return Vector3.Normalize(incidenceVector - 2f * Vector3.Dot(incidenceVector, trampolineTransform.forward) * trampolineTransform.forward) * incidenceVectorLength;
        }

        void OnTriggerEnter(Collider collider)
        {
            if (applyCharacterControllerBoost)
            {
                ThirdPersonController thirdPersonController = collider.GetComponentInParent<ThirdPersonController>();
                if (thirdPersonController != null && Time.time - lastCharacterControllerBounceTime >= bounceCooldown)
                {
                    Vector3 surfaceUp = trampolineTransform.up.normalized;
                    Vector3 launchVelocity = Vector3.up * characterControllerBounceVelocity;

                    if (useDynamicBounceFromFall)
                    {
                        float downwardSpeed = Mathf.Max(0.0f, -thirdPersonController.VerticalVelocity);
                        float fallHeight = (downwardSpeed * downwardSpeed) / (2.0f * controllerGravityMagnitude);
                        float targetHeight = fallHeight * bounceHeightMultiplier + extraBounceHeight;
                        float requiredUpSpeed = Mathf.Sqrt(2.0f * controllerGravityMagnitude * targetHeight);
                        launchVelocity.y = Mathf.Max(requiredUpSpeed, minUpBoost);
                    }
                    else if (launchVelocity.y < minUpBoost)
                    {
                        launchVelocity.y = minUpBoost;
                    }

                    // Keep jump height logic unchanged (Y), but add horizontal push from trampoline tilt.
                    Vector3 surfaceLateral = Vector3.ProjectOnPlane(surfaceUp, Vector3.up);
                    float slopeAmount = Mathf.Clamp01(surfaceLateral.magnitude);
                    if (slopeAmount > 0.0001f && tiltInfluence > 0.0f && lateralBounceMultiplier > 0.0f)
                    {
                        Vector3 lateralDirection = surfaceLateral.normalized;
                        float lateralSpeed = launchVelocity.y * slopeAmount * tiltInfluence * lateralBounceMultiplier;
                        launchVelocity += lateralDirection * lateralSpeed;
                    }

                    thirdPersonController.ExternalLaunch(launchVelocity);
                    lastCharacterControllerBounceTime = Time.time;
                }
            }

            if (!touched)
            {
                touched = true;
                objectTransform = collider.transform;
                objectRigidbody = objectTransform.GetComponent<Rigidbody>();

                if (objectRigidbody)
                {
                    touched = true;

                    closestDistance = objectTransform.position.y - trampolineTransform.position.y;
                    objectVelocity = objectRigidbody.linearVelocity;
                }
            }
        }

        void OnTriggerExit(Collider collider)
        {
            touched = false;

            objectRigidbody = null;
            closestDistance = 999999.0f;
            objectVelocity = Vector3.zero;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (objectRigidbody == null)
                return;

            closestDistance = 999999.0f;

            // Calculate reflection vector
            reflectionVector = CalculateReflectionVector(objectVelocity);
            objectRigidbody.linearVelocity = reflectionVector * bounciness;
        }

    }
}