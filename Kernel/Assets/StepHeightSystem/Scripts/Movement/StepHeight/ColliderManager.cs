using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LB.Player.Movement.StepHeight
{
	/// <summary>
	/// ColliderManager handles all the collider-related calculations and caching.
	/// It calculates the player's collider size, detects contact points, and checks for potential collisions.
	/// </summary>
	public class ColliderManager : IColliderManager
	{
		// Array of player's colliders
		private readonly Collider[] playerColliders;

		// Player's transform for positional calculations
		private readonly Transform playerTransform;

		// Array of player's colliders
		private float cachedPlayerColliderRadius;

		// Cached player collider height
		private float cachedPlayerColliderHeight;

		public float GetCachedPlayerColliderRadius()
		{
			return cachedPlayerColliderRadius;
		}

		public float GetCachedPlayerColliderHeight()
		{
			return cachedPlayerColliderHeight;
		}

		// List of contact points detected
		public List<MyContactPoint> ContactPoints { get; private set; }

		public ColliderManager(Collider[] playerColliders, Transform playerTransform)
		{
			this.playerColliders = playerColliders;
			this.playerTransform = playerTransform;
			ContactPoints = new List<MyContactPoint>();
			CachePlayerColliderInfo();
		}

		/// <summary>
		/// Caches the player's collider radius and height by analyzing the attached colliders.
		/// </summary>
		public void CachePlayerColliderInfo()
		{
			cachedPlayerColliderRadius = CalculatePlayerHorizontalColliderRadius();
			cachedPlayerColliderHeight = CalculatePlayerColliderHeight();
		}

		/// <summary>
		/// Collects contact points around the player using an overlap sphere method.
		/// </summary>
		public void CollectContactPointsUsingOverlapSphere()
		{
			const float sphereCastRadius = 0.6f;
			Collider[] colliders = new Collider[10];

			int hitCount = Physics.OverlapSphereNonAlloc(playerTransform.position, sphereCastRadius, colliders, ~0,
				QueryTriggerInteraction.Ignore);

			ContactPoints.Clear();

			for (int i = 0; i < hitCount; i++)
			{
				Collider otherCollider = colliders[i];
				if (otherCollider == null || playerColliders.Contains(otherCollider) || otherCollider.isTrigger)
					continue;

				Vector3 closestPoint = otherCollider.ClosestPoint(playerTransform.position);
				closestPoint.y = playerTransform.position.y + 0.01f; // Slightly above ground to avoid precision issues

				ContactPoints.Add(new MyContactPoint
				{
					Point = closestPoint,
					OtherCollider = otherCollider
				});
			}
		}

		/// <summary>
		/// Calculates the player's horizontal collider radius by combining all player colliders.
		/// </summary>
		/// <returns>Returns the maximum horizontal radius of the player colliders.</returns>
		private float CalculatePlayerHorizontalColliderRadius()
		{
			if (playerColliders.Length == 0)
			{
				Debug.LogWarning("No colliders found on the player object. Using default radius of 0.5f.");
				return 0.5f;
			}

			Bounds combinedBounds = new Bounds(playerTransform.position, Vector3.zero);
			foreach (Collider col in playerColliders)
			{
				if (!col.enabled) continue;
				combinedBounds.Encapsulate(col.bounds);
			}

			float radius = Mathf.Max(combinedBounds.extents.x, combinedBounds.extents.z);
			return radius;
		}

		/// <summary>
		/// Calculates the player's collider height by combining all player colliders.
		/// </summary>
		/// <returns>Returns the total height of the player colliders.</returns>
		private float CalculatePlayerColliderHeight()
		{
			if (playerColliders.Length == 0)
			{
				Debug.LogWarning("No colliders found on the player object. Using default height of 2.0f.");
				return 2.0f;
			}

			float minHeight = float.MaxValue;
			float maxHeight = float.MinValue;

			foreach (Collider col in playerColliders)
			{
				if (!col.enabled) continue;

				Bounds colBounds = col.bounds;
				minHeight = Mathf.Min(minHeight, colBounds.min.y);
				maxHeight = Mathf.Max(maxHeight, colBounds.max.y);
			}

			float height = maxHeight - minHeight;
			return height;
		}

		/// <summary>
		/// Checks if a point is inside any collider other than the player's own colliders.
		/// </summary>
		/// <param name="position">The position to check.</param>
		/// <param name="radius">The radius around the position to check for overlap.</param>
		/// <returns>True if the point is inside any collider, otherwise false.</returns>
		public bool IsInsideCollider(Vector3 position, float radius)
		{
			Collider[] overlappingColliders = new Collider[10];
			int numOverlaps = Physics.OverlapSphereNonAlloc(position, radius, overlappingColliders, ~0,
				QueryTriggerInteraction.Ignore);
			for (int i = 0; i < numOverlaps; i++)
			{
				Collider otherCollider = overlappingColliders[i];
				if (!otherCollider || playerColliders.Contains(otherCollider) || otherCollider.isTrigger)
					continue;
				return true;
			}

			return false;
		}

		/// <summary>
		/// Finds the highest ground point beneath the player using a grid of raycasts.
		/// </summary>
		/// <param name="groundPosition">The initial ground position.</param>
		/// <param name="height">The maximum height to search within.</param>
		/// <param name="radius">The radius around the player to check.</param>
		/// <returns>The highest detected ground point.</returns>
		public Vector3 GetHighestGroundPoint(Vector3 groundPosition, float height, float radius)
		{
			Vector3[] raycastOffsets =
			{
				Vector3.zero,
				new Vector3(radius, 0, 0),
				new Vector3(-radius, 0, 0),
				new Vector3(0, 0, radius),
				new Vector3(0, 0, -radius),
				new Vector3(radius / 2, 0, radius / 2),
				new Vector3(-radius / 2, 0, radius / 2),
				new Vector3(radius / 2, 0, -radius / 2),
				new Vector3(-radius / 2, 0, -radius / 2)
			};

			foreach (Vector3 offset in raycastOffsets)
			{
				Vector3 rayOrigin = playerTransform.position + offset + Vector3.up * 0.1f;
				if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, height, ~0, QueryTriggerInteraction.Ignore))
				{
					if (hit.point.y > groundPosition.y)
					{
						groundPosition = hit.point;
					}
				}
			}

			return groundPosition;
		}

		/// <summary>
		/// Checks for any ceiling collisions above a potential step-up position.
		/// </summary>
		/// <param name="stepUpPosition">The position to check above.</param>
		/// <param name="radius">The radius of the sphere to cast upwards.</param>
		/// <param name="height">The height to check above.</param>
		/// <returns>True if a ceiling collision is detected, otherwise false.</returns>
		public bool HasCeilingCollision(Vector3 stepUpPosition, float radius, float height)
		{
			RaycastHit[] ceilingHits = new RaycastHit[10];
			Vector3 startingPos = stepUpPosition + Vector3.up * height / 2f;
			int ceilingHitCount = Physics.SphereCastNonAlloc(startingPos, radius, Vector3.up, ceilingHits, radius * 2,
				~0, QueryTriggerInteraction.Ignore);
			for (int i = 0; i < ceilingHitCount; i++)
			{
				Collider hitCollider = ceilingHits[i].collider;
				if (hitCollider == null || playerColliders.Contains(hitCollider) || hitCollider.isTrigger)
				{
					continue;
				}

				return true;
			}

			return false;
		}
	}
}