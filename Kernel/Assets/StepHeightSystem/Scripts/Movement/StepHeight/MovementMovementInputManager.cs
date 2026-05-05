using LB.Player.Input;
using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// MovementInputManager implements the IMovementInputManager interface and handles player input.
	/// It subscribes to Unity's input system and triggers events when input is detected.
	/// </summary>
	public class MovementMovementInputManager : IMovementInputManager
	{
		public event System.Action<Vector2> OnMovementPerformed;
		public event System.Action OnMovementCanceled;

		// Enables movement input and subscribes to input events
		public void EnableMovementInput()
		{
			Debug.Log($"InputManager.inputMap: {InputManager.inputMap}");
			Debug.Log($"InputManager.inputMap.Player: {InputManager.inputMap.Player}");
			Debug.Log($"InputManager.inputMap.Player.Movement: {InputManager.inputMap.Player.Movement}");
			InputManager.inputMap.Player.Movement.performed +=
				ctx => OnMovementPerformed?.Invoke(ctx.ReadValue<Vector2>());
			InputManager.inputMap.Player.Movement.canceled += ctx => OnMovementCanceled?.Invoke();
			InputManager.inputMap.Player.Movement.Enable();
		}

		// Disables movement input
		public void DisableMovementInput()
		{
			InputManager.inputMap.Player.Movement.Disable();
		}
	}
}