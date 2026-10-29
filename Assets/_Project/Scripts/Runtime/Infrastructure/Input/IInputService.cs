using UnityEngine;

namespace NoWayOut.Infrastructure.Input
{
    public interface IInputService
    {
        InputMode Mode { get; }

        Vector2 Move { get; }

        Vector2 Look { get; }

        bool IsSprintHeld { get; }

        bool WasJumpPressedThisFrame { get; }

        void SetMode(InputMode mode);
    }
}
