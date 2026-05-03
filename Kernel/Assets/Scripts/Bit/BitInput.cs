using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Bit.Robot
{
    /// <summary>
    /// Читает WASD / мышь и через старый Input Manager, и через новый Input System
    /// (когда в проекте только Input System, GetAxis часто всегда 0).
    /// </summary>
    internal static class BitInput
    {
        private const float MousePixelScale = 0.02f;

        public static Vector2 GetMoveAxesSmoothed()
        {
            float h = Input.GetAxis("Horizontal");
            float v = Input.GetAxis("Vertical");

#if ENABLE_INPUT_SYSTEM
            if (Mathf.Abs(h) < 0.001f && Mathf.Abs(v) < 0.001f)
                ReadWasdRaw(out h, out v);
#endif

            Vector2 m = new Vector2(h, v);
            if (m.sqrMagnitude > 1f)
                m.Normalize();
            return m;
        }

        public static Vector2 GetMouseLook(float sensitivity)
        {
            float mx = Input.GetAxis("Mouse X") * sensitivity;
            float my = Input.GetAxis("Mouse Y") * sensitivity;

#if ENABLE_INPUT_SYSTEM
            if ((Mathf.Abs(mx) < 1e-5f && Mathf.Abs(my) < 1e-5f) && Mouse.current != null)
            {
                Vector2 d = Mouse.current.delta.ReadValue();
                mx = d.x * MousePixelScale * sensitivity;
                my = d.y * MousePixelScale * sensitivity;
            }
#endif

            return new Vector2(mx, my);
        }

        public static bool GetJumpDown()
        {
            if (Input.GetButtonDown("Jump"))
                return true;

            if (Input.GetKeyDown(KeyCode.Space))
                return true;

#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
                return true;

            if (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame)
                return true;
#endif

            return false;
        }

        public static bool GetSprintHeld()
        {
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                return true;

            try
            {
                if (Input.GetButton("Fire3"))
                    return true;
            }
            catch
            {
                // Ось Fire3 не задана в Input Manager — игнорируем.
            }

#if ENABLE_INPUT_SYSTEM
            Keyboard kb = Keyboard.current;
            if (kb != null && (kb.leftShiftKey.isPressed || kb.rightShiftKey.isPressed))
                return true;

            if (Gamepad.current != null && Gamepad.current.leftStickButton.isPressed)
                return true;
#endif

            return false;
        }

#if ENABLE_INPUT_SYSTEM
        private static void ReadWasdRaw(out float h, out float v)
        {
            h = 0f;
            v = 0f;
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)
                v += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)
                v -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed)
                h += 1f;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)
                h -= 1f;

            if (Mathf.Abs(h) > 1f || Mathf.Abs(v) > 1f)
            {
                Vector2 x = new Vector2(h, v);
                x.Normalize();
                h = x.x;
                v = x.y;
            }
        }
#endif
    }
}
