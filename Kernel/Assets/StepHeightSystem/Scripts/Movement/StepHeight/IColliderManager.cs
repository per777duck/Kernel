using System.Collections.Generic;
using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// IColliderManager defines the interface for managing player colliders and related calculations.
	/// It abstracts away the details of how colliders are handled, allowing for easier modifications and testing.
	/// </summary>
	public interface IColliderManager
	{
		void CachePlayerColliderInfo();
		void CollectContactPointsUsingOverlapSphere();
		List<MyContactPoint> ContactPoints { get; }
		bool IsInsideCollider(Vector3 position, float radius);
		Vector3 GetHighestGroundPoint(Vector3 groundPosition, float height, float radius);
		bool HasCeilingCollision(Vector3 stepUpPosition, float radius, float height);
		float GetCachedPlayerColliderRadius();
		float GetCachedPlayerColliderHeight();
	}
}