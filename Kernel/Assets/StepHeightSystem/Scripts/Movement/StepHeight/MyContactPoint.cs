using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// MyContactPoint is a simple data structure that holds information about a contact point in the world.
	/// It includes the position of the contact point and the collider involved in the contact.
	/// </summary>
	public class MyContactPoint
	{
		// Position of the contact point
		public Vector3 Point { get; set; }

		// Collider involved in the contact
		public Collider OtherCollider { get; set; }
	}
}