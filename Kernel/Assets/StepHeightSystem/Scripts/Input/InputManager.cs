using UnityEngine;

namespace LB.Player.Input
{
	public class InputManager : MonoBehaviour
	{
		public static InputMap inputMap;

		private void Awake()
		{
			inputMap = new InputMap();
		}
	}
}