using NoWayOut.Infrastructure.Input;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.Rendering;
using VContainer;

namespace NoWayOut.Gameplay.Player
{
    public sealed class PlayerController : NetworkBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private NetworkTransform networkTransform;
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private GameObject ownerCamera;
        [SerializeField] private Renderer bodyRenderer;

        private IInputService inputService;
        private PlayerConfig config;
        private PlayerMotor motor;
        private PlayerLook look;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;


        [Inject]
        private void Construct(IInputService inputService, PlayerConfig config)
        {
            this.inputService = inputService;
            this.config = config;
        }


        public override void OnNetworkSpawn()
        {
            ownerCamera.SetActive(IsOwner);

            if (!IsOwner)
            {
                return;
            }

            bodyRenderer.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
            motor = new PlayerMotor(characterController, config);
            look = new PlayerLook(transform, cameraPivot, config);
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }


        public override void OnNetworkDespawn()
        {
            ownerCamera.SetActive(false);
        }


        private void Update()
        {
            if (!IsSpawned || !IsOwner)
            {
                return;
            }

            look.Tick(inputService.Look);
            motor.Tick(inputService.Move, inputService.IsSprintHeld, inputService.WasJumpPressedThisFrame, Time.deltaTime);

            if (transform.position.y < config.fallRespawnHeight)
            {
                Respawn();
            }
        }


        private void Respawn()
        {
            characterController.enabled = false;
            networkTransform.Teleport(spawnPosition, spawnRotation, transform.localScale);
            characterController.enabled = true;
            motor.ResetVerticalSpeed();
        }
    }
}
