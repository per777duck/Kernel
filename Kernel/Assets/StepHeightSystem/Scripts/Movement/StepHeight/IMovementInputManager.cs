using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// IMovementInputManager defines the interface for managing player input.
	/// It provides events for movement input and methods to enable or disable input handling.
	/// </summary>
	public interface IMovementInputManager
	{
		event System.Action<Vector2> OnMovementPerformed;
		event System.Action OnMovementCanceled;
		void EnableMovementInput();
		void DisableMovementInput();
	}
}