using UnityEngine;

namespace NoWayOut.Gameplay.Player
{
    internal sealed class PlayerLook
    {
        private readonly Transform body;
        private readonly Transform cameraPivot;
        private readonly PlayerConfig config;
        private float pitch;


        public PlayerLook(Transform body, Transform cameraPivot, PlayerConfig config)
        {
            this.body = body;
            this.cameraPivot = cameraPivot;
            this.config = config;
        }


        public void Tick(Vector2 lookInput)
        {
            body.Rotate(0f, lookInput.x * config.lookSensitivity, 0f);

            pitch -= lookInput.y * config.lookSensitivity;
            pitch = Mathf.Clamp(pitch, config.minPitch, config.maxPitch);
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
