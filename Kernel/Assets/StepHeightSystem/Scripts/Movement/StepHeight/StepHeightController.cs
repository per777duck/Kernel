using System.Collections;
using System.Linq;
using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// StepHeightController is responsible for managing the player's ability to step over obstacles.
	/// It works by detecting potential steps the player can take and smoothly moving the player
	/// over the obstacle if conditions are met. The stepping logic is highly configurable and
	/// includes features such as debugging visualizations and collision checks.
	/// </summary>
	public class StepHeightController : MonoBehaviour 
	{
		// Reference to the Collider Manager, handles player collider interactions
		private IColliderManager colliderManager;

		// Reference to the Movement Input Manager, processes movement input
		private IMovementInputManager movementInputManager;

		// Wrapper around Rigidbody to manipulate the player's physics movement
		private IRigidbodyWrapper rigidbodyWrapper;

		[Tooltip("The maximum height that the player can step up.")] [SerializeField]
		private float stepHeight = 0.5f; // Maximum height the player can step up

		[Tooltip("The smooth factor for how quickly the player will move up the step.")] [SerializeField]
		private float stepUpSmoothFactor = 4.5f; // Controls the smoothness of stepping motion

		[Tooltip("The maximum angle between the player's movement direction and the step direction.")] [SerializeField]
		private float stepUpAngleThreshold = 65f; // Maximum angle allowed for stepping direction

		[Tooltip("Layers to ignore when performing physics checks.")] [SerializeField]
		private LayerMask layersToIgnore = 0; // Layers that should be ignored in physics checks

		[Tooltip("If true, step height functionality is disabled.")] [SerializeField]
		private bool disableStepHeight = false; // Disables step height functionality when true

		#region Debug

		[Space(20)] [Header("Debug")] [Tooltip("Enable or disable debug logging.")] [SerializeField]
		private bool debugLog = false; // Enables general debug logging

		[Tooltip("Enable or disable warning logging.")] [SerializeField]
		private bool debugLogWarning = false; // Enables warning logs

		[Tooltip("Enable or disable error logging.")] [SerializeField]
		private bool debugLogError = true; // Enables error logs

		[Tooltip("Enable or disable debug visualizations in the scene view.")] [SerializeField]
		private bool debugVisualization = false; // Enables debug visualizations in the scene view

		#endregion Debug

		#region State

		// Tracks if the player is currently stepping over an obstacle
		private bool isStepping = false;

		// Tracks if the player currently has movement input
		private bool hasMovementInput;

		// The collision point where the player contacts the ground
		private Vector3 groundCollisionPosition;

		#endregion State

		#region Unity Lifecycle Methods

		/// <summary>
		/// Called when the script instance is being loaded. Initializes the required components and caches collider information.
		/// </summary>
		private void Awake()
		{
			rigidbodyWrapper = new RigidbodyWrapper(GetComponent<Rigidbody>());
			colliderManager = new ColliderManager(GetComponentsInChildren<Collider>(), transform);
			movementInputManager = new MovementMovementInputManager();

			UpdateCachedPlayerColliderInfo();
		}

		/// <summary>
		/// Called when the object becomes enabled and active. Subscribes to input events.
		/// </summary>
		private void OnEnable()
		{
			movementInputManager.OnMovementPerformed += StartMovement;
			movementInputManager.OnMovementCanceled += StopMovement;
			movementInputManager.EnableMovementInput();
		}

		/// <summary>
		/// Called when the object becomes disabled or inactive. Unsubscribes from input events.
		/// </summary>
		private void OnDisable()
		{
			movementInputManager.DisableMovementInput();
		}

		#endregion Unity Lifecycle Methods

		#region Public Methods

		/// <summary>
		/// Updates the cached player collider information for accurate collision detection.
		/// </summary>
		public void UpdateCachedPlayerColliderInfo()
		{
			colliderManager.CachePlayerColliderInfo();
		}

		/// <summary>
		/// Checks if the player can step onto a nearby obstacle and handles the step if possible.
		/// </summary>
		public void CheckForStep()
		{
			if (disableStepHeight || isStepping) return;

			groundCollisionPosition = GetGroundCollisionPosition();
			Vector3 movementDirection = rigidbodyWrapper.Velocity.normalized;

			if (TryGetStep(out Vector3 stepUpPosition, out Vector3 alignedStepUpPosition,
				    out MyContactPoint bestContactPoint, movementDirection))
			{
				if (IsCollidingWithStep(bestContactPoint, stepUpPosition))
				{
					HandleStep(alignedStepUpPosition, bestContactPoint);
				}
				else if (debugLog)
				{
					Debug.Log("Player is too far away from collider, step will not be performed!");
				}
			}
		}

		#endregion Public Methods

		#region Private Methods

		/// <summary>
		/// Handles the start of movement input.
		/// </summary>
		/// <param name="input">The movement input vector.</param>
		private void StartMovement(Vector2 input)
		{
			hasMovementInput = true;
		}

		/// <summary>
		/// Handles the stop of movement input.
		/// </summary>
		private void StopMovement()
		{
			hasMovementInput = false;
		}

		/// <summary>
		/// Attempts to find a valid step for the player to move onto.
		/// </summary>
		/// <param name="stepUpPosition">Outputs the position of the step if found.</param>
		/// <param name="alignedStepUpPosition">Outputs the aligned position to the players movement direction of the step if found</param>
		/// <param name="bestContactPoint">Outputs the best contact point for stepping.</param>
		/// <param name="movementDirection">The direction of the player's movement.</param>
		/// <returns>Returns true if a valid step is found, otherwise false.</returns>
		private bool TryGetStep(out Vector3 stepUpPosition, out Vector3 alignedStepUpPosition,
			out MyContactPoint bestContactPoint,
			Vector3 movementDirection)
		{
			stepUpPosition = Vector3.zero;
			alignedStepUpPosition = Vector3.zero;
			bestContactPoint = default;
			const float minimumStepHeight = 0f;

			colliderManager.CollectContactPointsUsingOverlapSphere();

			if (colliderManager.ContactPoints.Count <= 0)
			{
				return false;
			}

			Vector3 origin = groundCollisionPosition;
			origin.y = colliderManager.ContactPoints.Min(cp => cp.Point.y) + 0.05f;

			float bestDotProduct = -1f;
			bool foundValidStep = false;

			// Loop through all contact points to find the best step candidate.
			foreach (MyContactPoint contact in colliderManager.ContactPoints)
			{
				Vector3 toContact = (contact.Point - origin).normalized;
				if (movementDirection.sqrMagnitude > 0.0001f && toContact.sqrMagnitude > 0.0001f)
				{
					float dotProduct = Vector3.Dot(movementDirection, toContact);
					float angleToContact = Vector3.Angle(movementDirection, toContact);

					if (angleToContact <= stepUpAngleThreshold)
					{
						foundValidStep = true;

						if (dotProduct > bestDotProduct)
						{
							bestDotProduct = dotProduct;
							bestContactPoint = contact;
						}
					}
				}
				else
				{
					if (debugLogWarning)
					{
						Debug.LogWarning(
							"Invalid vectors for step-up calculation. Movement direction or toContact vector might be zero.");
					}
				}
			}

			if (!foundValidStep || bestContactPoint == null)
			{
				return false;
			}

			Vector3 stepOffset = movementDirection.normalized * 0.1f;
			Vector3 topCheckOrigin = bestContactPoint.Point + Vector3.up * (stepHeight * 1.5f) + stepOffset;
			float raycastDistance = stepHeight * 1.5f;

			if (debugVisualization)
			{
				Debug.DrawLine(topCheckOrigin, topCheckOrigin + Vector3.down * raycastDistance, Color.yellow, 1.0f);
				DrawSphere(topCheckOrigin, 0.025f, Color.yellow);
			}

			if (colliderManager.IsInsideCollider(topCheckOrigin, 0.01f))
			{
				return false;
			}

			if (!Physics.Raycast(topCheckOrigin, Vector3.down, out RaycastHit topHit, raycastDistance, ~0,
				    QueryTriggerInteraction.Ignore))
			{
				return false;
			}

			float heightDifference = topHit.point.y - origin.y;
			if (debugLog && heightDifference > minimumStepHeight)
			{
				Debug.Log($"Best contact Data - " +
				          $"Contact Point: {bestContactPoint.Point}, " +
				          $"Other Collider: {bestContactPoint.OtherCollider.name}, " +
				          $"Top Hit Point: {topHit.point}, " +
				          $"Height Difference: {heightDifference}, " +
				          $"Angle to Contact: {Vector3.Angle(movementDirection, (bestContactPoint.Point - origin).normalized):F2}°, " +
				          $"Dot Product: {Vector3.Dot(movementDirection, (bestContactPoint.Point - origin).normalized):F4}, " +
				          $"Step Offset: {stepOffset}");
			}

			if (heightDifference < minimumStepHeight || heightDifference > stepHeight)
			{
				return false;
			}

			stepUpPosition = new Vector3(rigidbodyWrapper.Position.x, topHit.point.y, rigidbodyWrapper.Position.z);
			Vector3 horizontalDisplacement = Vector3.ProjectOnPlane(movementDirection, Vector3.up);
			alignedStepUpPosition = stepUpPosition + horizontalDisplacement;

			if (colliderManager.HasCeilingCollision(stepUpPosition, colliderManager.GetCachedPlayerColliderRadius(),
				    colliderManager.GetCachedPlayerColliderHeight()))
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Checks if the player is close enough to the step to collide with it.
		/// </summary>
		/// <param name="bestContactPoint">The contact point on the step object.</param>
		/// <param name="stepUpPosition">The position of the potential step.</param>
		/// <returns>Returns true if the player is colliding with the step, otherwise false.</returns>
		private bool IsCollidingWithStep(MyContactPoint bestContactPoint, Vector3 stepUpPosition)
		{
			Vector3 horizontalDistance = new Vector3(bestContactPoint.Point.x - stepUpPosition.x, 0,
				bestContactPoint.Point.z - stepUpPosition.z);

			return horizontalDistance.magnitude < colliderManager.GetCachedPlayerColliderRadius() + 0.1f;
		}

		/// <summary>
		/// Handles the player stepping up onto the detected step position.
		/// </summary>
		/// <param name="stepUpPosition">The position to step up to.</param>
		/// <param name="bestContactPoint">The best contact point for the step.</param>
		private void HandleStep(Vector3 stepUpPosition, MyContactPoint bestContactPoint)
		{
			if (!isStepping)
			{
				isStepping = true;

				Vector3 startPosition = rigidbodyWrapper.Position;

				if (debugVisualization)
				{
					Debug.DrawLine(startPosition, startPosition + Vector3.up, Color.blue, 3.0f);
					Debug.DrawLine(stepUpPosition, stepUpPosition + Vector3.up, Color.green, 3.0f);
					Debug.DrawLine(startPosition + Vector3.up, stepUpPosition + Vector3.up, Color.red, 3.0f);
				}

				StartCoroutine(CompleteStepMovement(stepUpPosition, stepUpSmoothFactor, bestContactPoint));
			}
			else
			{
				if (debugLogError)
				{
					Debug.LogError(
						$"Step-up cannot be performed. Player is still performing a step! isStepping: {isStepping}");
				}
			}
		}

		/// <summary>
		/// Calculates the ground collision position beneath the player.
		/// </summary>
		/// <returns>The position of the ground beneath the player.</returns>
		private Vector3 GetGroundCollisionPosition()
		{
			Vector3 groundPosition = rigidbodyWrapper.Position;

			Vector3 colliderBottom = rigidbodyWrapper.Position - Vector3.up *
				(colliderManager.GetCachedPlayerColliderHeight() / 2 -
				 colliderManager.GetCachedPlayerColliderRadius());
			Vector3 colliderTop = rigidbodyWrapper.Position + Vector3.up *
				(colliderManager.GetCachedPlayerColliderHeight() / 2 -
				 colliderManager.GetCachedPlayerColliderRadius());

			if (Physics.CapsuleCast(colliderTop, colliderBottom, colliderManager.GetCachedPlayerColliderRadius(),
				    Vector3.down, out RaycastHit hit, colliderManager.GetCachedPlayerColliderHeight(), ~0,
				    QueryTriggerInteraction.Ignore))
			{
				groundPosition = hit.point;
				if (debugVisualization)
				{
					Debug.DrawLine(rigidbodyWrapper.Position, hit.point, Color.green, 0.1f);
				}
			}

			groundPosition = colliderManager.GetHighestGroundPoint(groundPosition,
				colliderManager.GetCachedPlayerColliderHeight(), colliderManager.GetCachedPlayerColliderRadius());

			groundPosition.x = rigidbodyWrapper.Position.x;
			groundPosition.z = rigidbodyWrapper.Position.z;

			return groundPosition;
		}

		#endregion Private Methods

		#region Coroutines

		/// <summary>
		/// Smoothly moves the player to the step-up position over time.
		/// </summary>
		/// <param name="targetPosition">The target position for the step-up.</param>
		/// <param name="smoothFactor">The factor determining the speed of the movement.</param>
		/// <param name="targetContactPoint">The specific contact point on the target collider.</param>
		/// <returns>IEnumerator for coroutine handling.</returns>
		private IEnumerator CompleteStepMovement(Vector3 targetPosition, float smoothFactor,
			MyContactPoint targetContactPoint)
		{
			float timeElapsed = 0f;
			Vector3 startPosition = rigidbodyWrapper.Position;

			while (timeElapsed < 1f / smoothFactor)
			{
				if (!hasMovementInput)
				{
					break;
				}

				timeElapsed += Time.deltaTime;

				Vector3 smoothStepPosition = Vector3.Lerp(startPosition, targetPosition,
					Mathf.SmoothStep(0f, 1f, timeElapsed * smoothFactor));
				rigidbodyWrapper.MovePosition(smoothStepPosition);

				if (IsOnTargetCollider(targetContactPoint))
				{
					break;
				}

				yield return null;
			}

			isStepping = false;
		}

		/// <summary>
		/// Checks if the player is on the target collider after the step-up movement.
		/// </summary>
		/// <param name="targetContactPoint">The specific contact point on the target collider.</param>
		/// <returns>Returns true if the player is on the target collider, otherwise false.</returns>
		private bool IsOnTargetCollider(MyContactPoint targetContactPoint)
		{
			Vector3 rayOrigin = rigidbodyWrapper.Position;
			rayOrigin.y += colliderManager.GetCachedPlayerColliderHeight();

			RaycastHit[] hits = new RaycastHit[10];
			float checkDistance = colliderManager.GetCachedPlayerColliderHeight() + 0.5f;

			int hitCount = Physics.RaycastNonAlloc(rayOrigin, Vector3.down, hits, checkDistance, ~layersToIgnore,
				QueryTriggerInteraction.Ignore);

			for (int i = 0; i < hitCount; i++)
			{
				if (hits[i].collider == targetContactPoint.OtherCollider)
				{
					return true;
				}
			}

			return false;
		}

		#endregion Coroutines

		#region Debug Visualization

		/// <summary>
		/// Draws a sphere in the scene for debugging purposes.
		/// </summary>
		/// <param name="center">The center of the sphere.</param>
		/// <param name="radius">The radius of the sphere.</param>
		/// <param name="color">The color of the sphere.</param>
		private void DrawSphere(Vector3 center, float radius, Color color)
		{
			float angleStep = 10f;
			for (float theta = 0; theta < 360f; theta += angleStep)
			{
				float radians = Mathf.Deg2Rad * theta;
				Vector3 offset = new Vector3(Mathf.Cos(radians), Mathf.Sin(radians), 0) * radius;
				Vector3 point1 = center + offset;
				Vector3 point2 = center + Quaternion.Euler(0, angleStep, 0) * offset;
				Debug.DrawLine(point1, point2, color, 1f);

				Vector3 offsetY = new Vector3(0, Mathf.Cos(radians), Mathf.Sin(radians)) * radius;
				Vector3 pointY1 = center + offsetY;
				Vector3 pointY2 = center + Quaternion.Euler(angleStep, 0, 0) * offsetY;
				Debug.DrawLine(pointY1, pointY2, color, 1f);
			}
		}

		#endregion Debug Visualization
	}
}