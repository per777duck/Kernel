using UnityEngine;
using UnityEngine.InputSystem;
using LB.Player.Movement.StepHeight;
using LB.Player.Input;

namespace LB.Player.Movement
{
	/// <summary>
	/// This is a demo movement controller that handles basic player movement, jumping, and rotation. 
	/// It also integrates with a StepHeightController for stepping over obstacles.
	/// Users can replace the movement logic with their custom movement controller, while still utilizing 
	/// the StepHeightController for handling step mechanics if desired.
	/// </summary>
	
	[RequireComponent(typeof(Rigidbody))]
	public class MovementController : MonoBehaviour
	{
		#region Variables

		[HideInInspector]
		public StepHeightController stepHeightController; // Reference to StepHeightController for step logic

		[HideInInspector] public new Rigidbody rigidbody; // Rigidbody component for player physics
		private SpringJoint magnetJoint; // SpringJoint for handling swinging mechanics

		private bool isSwinging = false; // Tracks whether the player is currently swinging
		private Vector3 previousInputInfluence = Vector3.zero; // Stores the previous movement input influence

		[Header("Movement")] [SerializeField] private float walkingSpeed = 4.5f; // Walking speed of the player
		[SerializeField] private float runningSpeed = 6.725f; // Running speed of the player

		[SerializeField] [Range(1f, 10)]
		private float swingInfluenceFactor = 5; // Factor influencing the player's swing movement

		private bool onStair; // Indicates whether the player is on a stair
		private bool hasInput; // Tracks if there is movement input from the player
		[HideInInspector] public bool blockMovement; // If true, blocks player movement

		[Header("Rotation")] [SerializeField]
		private float lookSpeed = 2.0f; // Speed at which the player can look around

		[SerializeField] private float lookXLimit = 90; // Maximum angle for vertical look rotation
		private float rotationX = 0; // Tracks the current vertical rotation angle
		[SerializeField] private Transform xRotationTransform; // Transform for handling X-axis rotation (vertical look)

		[SerializeField]
		private Transform yRotationTransform; // Transform for handling Y-axis rotation (horizontal look)

		private float rotationModifier; // Modifier applied to rotation speed

		[Header("Jumping")] [SerializeField] private float jumpForce = 10f; // Force applied to the player when jumping
		[SerializeField] private MeshCollider feet; // Collider representing the player's feet for ground checks
		private float distanceToGround; // Distance from the feet to the ground

		private Vector2 movementInput = Vector2.zero; // Stores the player's movement input (WASD or analog stick)
		private Vector2 mouseDelta = Vector2.zero; // Stores the player's mouse movement input for looking around
		private bool sprinting; // Tracks whether the player is sprinting
		private Vector3 lastInputVector; // Last movement vector based on player input

		[Header("Gravity")] [SerializeField]
		private float gravityStrength; // Strength of the gravity applied to the player

		[SerializeField] private Vector3 gravityDirection; // Direction of the gravity applied to the player

		#endregion

		#region (De)initialization

		/// <summary>
		/// Subscribes to input events when the script is enabled.
		/// </summary>
		private void OnEnable() 
		{
			InputManager.inputMap.Player.MouseDelta.performed += ctx => mouseDelta = ctx.ReadValue<Vector2>();
			InputManager.inputMap.Player.MouseDelta.Enable();
			InputManager.inputMap.Player.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
			InputManager.inputMap.Player.Movement.Enable();
			InputManager.inputMap.Player.Jump.performed += OnJumpPerformed;
			InputManager.inputMap.Player.Jump.Enable();
			InputManager.inputMap.Player.Sprint.performed += ctx => sprinting = ctx.ReadValueAsButton();
			InputManager.inputMap.Player.Sprint.Enable();
			InputManager.inputMap.Player.Enable();
		}

		/// <summary>
		/// Unsubscribes from input events when the script is disabled.
		/// </summary>
		private void OnDisable()
		{
			InputManager.inputMap.Player.Jump.performed -= OnJumpPerformed;
			InputManager.inputMap.Player.MouseDelta.Disable();
			InputManager.inputMap.Player.Movement.Disable();
			InputManager.inputMap.Player.Jump.Disable();
			InputManager.inputMap.Player.Sprint.Disable();
			InputManager.inputMap.Player.Disable();
		}

		/// <summary>
		/// Initializes the necessary components when the script is loaded.
		/// </summary>
		private void Awake()
		{
			stepHeightController = GetComponent<StepHeightController>();
			rigidbody = GetComponent<Rigidbody>();
			distanceToGround = feet.bounds.extents.y;

			rigidbody.isKinematic = true; // Initially set the Rigidbody to kinematic
			Cursor.visible = false; // Hide the cursor
			Cursor.lockState = CursorLockMode.Locked; // Lock the cursor to the center of the screen

			SetRotationModifier(1f); // Set default rotation modifier
		}

		/// <summary>
		/// Initializes the gravity direction and sets the Rigidbody to non-kinematic.
		/// </summary>
		private void Start()
		{
			gravityDirection = Vector3.down; // Set gravity to point downwards
			rigidbody.isKinematic = false; // Enable Rigidbody physics
		}

		#endregion

		/// <summary>
		/// Called every frame to update movement, rotation, and handle player input.
		/// </summary>
		private void Update()
		{
			InputManager.inputMap.Player.Enable();
			bool grounded = IsGrounded(); // Check if the player is grounded

			if (blockMovement) // If movement is blocked, apply modified velocity
			{
				Vector3 currentMovement = GetModifiedVelocity(false);
				if (grounded)
				{
					currentMovement.x *= .5f;
					currentMovement.z *= .5f;
				}

				rigidbody.linearVelocity = currentMovement;
				if (rigidbody.linearVelocity.magnitude < .3f && grounded && onStair && !magnetJoint)
				{
					rigidbody.isKinematic = true; // Set Rigidbody to kinematic if almost stationary on stairs
				}

				return;
			}

			DoRotation(); // Handle player rotation
			DoMovement(grounded); // Handle player movement
		}

		/// <summary>
		/// Handles gravity force application in FixedUpdate for consistent physics behavior.
		/// </summary>
		private void FixedUpdate()
		{
			rigidbody.AddForce(rigidbody.mass * rigidbody.mass * gravityStrength * gravityDirection.normalized);
		}

		/// <summary>
		/// Sets a modifier for controlling the player's rotation speed.
		/// </summary>
		/// <param name="_value">The new rotation modifier value.</param>
		public void SetRotationModifier(float _value)
		{
			rotationModifier = _value;
		}

		/// <summary>
		/// Handles player movement based on input, grounded status, and other factors.
		/// </summary>
		/// <param name="grounded">Is the player grounded?</param>
		private void DoMovement(bool grounded)
		{
			if (blockMovement) return;

			float speed = sprinting ? runningSpeed : walkingSpeed; // Determine movement speed based on sprint status

			Vector2 curSpeed = new(movementInput.y, movementInput.x);
			hasInput = curSpeed.x != 0 || curSpeed.y != 0; // Check if there is any input
			if (hasInput && rigidbody.isKinematic) rigidbody.isKinematic = false;

			if (curSpeed.magnitude > 1)
			{
				curSpeed = curSpeed.normalized; // Normalize the speed vector if greater than 1
			}

			curSpeed *= speed; // Apply speed to input vector

			Vector3 currentMovement = GetModifiedVelocity(hasInput);
			Vector3 newMovement = (yRotationTransform.forward * curSpeed.x) + (yRotationTransform.right * curSpeed.y);
			lastInputVector = newMovement;

			if (grounded)
			{
				if (!isSwinging)
				{
					currentMovement.x *= 0.5f;
					currentMovement.z *= 0.5f; // Slow down movement if grounded and not swinging
				}

				if (!hasInput)
				{
					previousInputInfluence = Vector3.zero; // Reset previous input influence if no input
				}

				if (hasInput)
				{
					stepHeightController.CheckForStep(); // Check for step if player is moving
				}
			}
			else
			{
				if (hasInput)
				{
					if (Vector3.Angle(lastInputVector, newMovement) < 20)
					{
						float angle = Vector3.Angle(currentMovement, newMovement);
						bool inFrontOfWall =
							Physics.SphereCast(yRotationTransform.position - .5f * yRotationTransform.forward, .5f,
								yRotationTransform.forward, out RaycastHit _, 1.5f, ~LayerMask.GetMask("Player"),
								QueryTriggerInteraction.Ignore); // Check if the player is in front of a wall

						if (inFrontOfWall)
						{
							if (angle > 20)
							{
								currentMovement =
									new Vector3(0, currentMovement.y, 0); // Stop forward movement if large angle
							}
							else
							{
								currentMovement *= (1 - (angle / 20)); // Reduce speed based on angle
							}
						}
					}

					float factor = swingInfluenceFactor * Time.deltaTime;
					newMovement =
						previousInputInfluence * (1 - factor) + newMovement * factor; // Blend previous and new input
				}
			}

			if (hasInput)
			{
				previousInputInfluence = newMovement;
			}

			rigidbody.linearVelocity = currentMovement + newMovement;

			// Clamp player velocity to running speed
			float clampedX = Mathf.Clamp(rigidbody.linearVelocity.x, -runningSpeed, runningSpeed);
			float clampedZ = Mathf.Clamp(rigidbody.linearVelocity.z, -runningSpeed, runningSpeed);
			rigidbody.linearVelocity = new Vector3(clampedX, rigidbody.linearVelocity.y, clampedZ);

			if (!hasInput && rigidbody.linearVelocity.magnitude < .3f && grounded && onStair && !magnetJoint)
			{
				rigidbody.isKinematic = true;
			}
		}

		/// <summary>
		/// Handles player rotation based on mouse input.
		/// </summary>
		private void DoRotation()
		{
			if (blockMovement) return;
			float deltaTime = Mathf.Min(Time.deltaTime, .05f); // Limit the time step to prevent large jumps

			rotationX += -mouseDelta.y * lookSpeed * deltaTime * 10 * rotationModifier;
			rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
			xRotationTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);

			yRotationTransform.rotation *=
				Quaternion.Euler(0, mouseDelta.x * lookSpeed * deltaTime * 10 * rotationModifier, 0);
		}

		/// <summary>
		/// Completely removes the previous input influence on the player's velocity if new input is provided.
		/// </summary>
		/// <param name="_hasInput">Is new input provided?</param>
		/// <returns>The velocity of the player without input influences (if input is provided, else the unmodified velocity).</returns>
		private Vector3 GetModifiedVelocity(bool _hasInput)
		{
			Vector3 rawVelocity = rigidbody.linearVelocity;

			if (_hasInput)
			{
				Vector3 proj = Vector3.Project(rawVelocity, previousInputInfluence);
				if (proj.sqrMagnitude > previousInputInfluence.sqrMagnitude)
				{
					proj = proj.normalized * previousInputInfluence.magnitude;
				}

				return rawVelocity - proj; // Subtract the influence of previous input from the velocity
			}

			return rawVelocity; // Return the raw velocity if no input
		}

		/// <summary>
		/// Checks if the player is grounded by casting rays from the feet to the ground.
		/// </summary>
		/// <returns>True if the player is grounded, otherwise false.</returns>
		public bool IsGrounded()
		{
			Vector3 fPos = feet.transform.position;
			Vector3[] positions = new Vector3[4]; // Array for the four raycast positions around the feet
			Vector3 fwd = 0.4f * feet.bounds.extents.z * Vector3.forward;
			Vector3 right = 0.4f * feet.bounds.extents.x * Vector3.right;

			// Set up the four corner positions for the raycasts
			positions[0] = fPos + fwd + right;
			positions[1] = fPos + fwd - right;
			positions[2] = fPos - fwd + right;
			positions[3] = fPos - fwd - right;

			// Cast rays to detect the ground
			foreach (Vector3 v in positions)
			{
				if (Physics.Raycast(v, -transform.up, out RaycastHit ray, distanceToGround + .5f,
					    ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
				{
					onStair = ray.collider.CompareTag("Stairs");
					if (!magnetJoint)
					{
						isSwinging = false; // Stop swinging if not attached to the magnet joint
					}

					return true; // Player is grounded
				}
			}

			onStair = false; // Player is not on stairs
			return false; // Player is not grounded
		}

		/// <summary>
		/// Handles the player's jump input.
		/// </summary>
		/// <param name="ctx">The callback context for the input action.</param>
		private void OnJumpPerformed(InputAction.CallbackContext ctx)
		{
			if (!IsGrounded()) return;
			if (blockMovement) return;

			rigidbody.isKinematic = false;
			Vector3 vel = transform.InverseTransformDirection(rigidbody.linearVelocity); // Get velocity in local space
			vel.y = jumpForce;
			rigidbody.linearVelocity = transform.TransformDirection(vel); // Apply the jump force
		}
	}
}