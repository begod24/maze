using System;
using UnityEngine;

namespace NoWayOut.Infrastructure.Input
{
    internal sealed class InputService : IInputService, IDisposable
    {
        private readonly GameInputActions actions = new GameInputActions();
        private InputMode mode = InputMode.Disabled;

        public InputMode Mode => mode;
        public Vector2 Move => actions.Player.Move.ReadValue<Vector2>();
        public Vector2 Look => actions.Player.Look.ReadValue<Vector2>();
        public bool IsSprintHeld => actions.Player.Sprint.IsPressed();
        public bool WasJumpPressedThisFrame => actions.Player.Jump.WasPressedThisFrame();


        public void SetMode(InputMode mode)
        {
            this.mode = mode;
            actions.Player.Disable();
            actions.UI.Disable();

            switch (mode)
            {
                case InputMode.Gameplay:
                    actions.Player.Enable();
                    SetCursorLocked(true);
                    break;
                case InputMode.UI:
                    actions.UI.Enable();
                    SetCursorLocked(false);
                    break;
                default:
                    SetCursorLocked(false);
                    break;
            }
        }


        public void Dispose()
        {
            actions.Disable();
            actions.Dispose();
        }


        private static void SetCursorLocked(bool isLocked)
        {
            if (isLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
