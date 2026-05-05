using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// RigidbodyWrapper implements the IRigidbodyWrapper interface, wrapping a Unity Rigidbody component.
	/// It provides access to Rigidbody properties and methods in a controlled way.
	/// </summary>
	public class RigidbodyWrapper : IRigidbodyWrapper
	{
		private readonly Rigidbody rigidbody;

		public RigidbodyWrapper(Rigidbody rigidbody)
		{
			this.rigidbody = rigidbody;
		}

		// Current velocity of the Rigidbody
		public Vector3 Velocity => rigidbody.linearVelocity;

		// Current position of the Rigidbody
		public Vector3 Position => rigidbody.position;

		// Moves the Rigidbody to a specified position
		public void MovePosition(Vector3 position)
		{
			rigidbody.MovePosition(position);
		}
	}
}