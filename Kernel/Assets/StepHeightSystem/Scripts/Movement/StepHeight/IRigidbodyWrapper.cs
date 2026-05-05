using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// IRigidbodyWrapper provides an abstraction over Unity's Rigidbody component.
	/// It allows for easier testing and modification of Rigidbody-related behavior.
	/// </summary>
	public interface IRigidbodyWrapper
	{
		Vector3 Velocity { get; }
		Vector3 Position { get; }
		void MovePosition(Vector3 position);
	}
}