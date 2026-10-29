using UnityEngine;

namespace NoWayOut.Gameplay.Player
{
    internal sealed class PlayerMotor
    {
        private readonly CharacterController characterController;
        private readonly PlayerConfig config;
        private float verticalSpeed;


        public PlayerMotor(CharacterController characterController, PlayerConfig config)
        {
            this.characterController = characterController;
            this.config = config;
        }


        public void Tick(Vector2 moveInput, bool isSprintHeld, bool isJumpPressed, float deltaTime)
        {
            UpdateVerticalSpeed(isJumpPressed, deltaTime);

            Vector3 velocity = GetHorizontalVelocity(moveInput, isSprintHeld);
            velocity.y = verticalSpeed;
            characterController.Move(velocity * deltaTime);
        }


        public void ResetVerticalSpeed()
        {
            verticalSpeed = 0f;
        }


        private void UpdateVerticalSpeed(bool isJumpPressed, float deltaTime)
        {
            if (!characterController.isGrounded)
            {
                verticalSpeed += config.gravity * deltaTime;
                return;
            }

            if (isJumpPressed)
            {
                verticalSpeed = Mathf.Sqrt(2f * config.jumpHeight * -config.gravity);
                return;
            }

            verticalSpeed = config.groundedVerticalSpeed;
        }


        private Vector3 GetHorizontalVelocity(Vector2 moveInput, bool isSprintHeld)
        {
            Transform body = characterController.transform;
            Vector3 direction = body.forward * moveInput.y + body.right * moveInput.x;
            direction = Vector3.ClampMagnitude(direction, 1f);

            float speed = isSprintHeld ? config.sprintSpeed : config.walkSpeed;
            return direction * speed;
        }
    }
}
