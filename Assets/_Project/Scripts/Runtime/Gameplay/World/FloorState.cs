using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace NoWayOut.Gameplay.World
{
    public sealed class FloorState : NetworkBehaviour
    {
        private const int RANDOM_SEED_MIN = 1;

        private readonly NetworkVariable<int> seed = new NetworkVariable<int>();
        private IFloor floor;
        private FloorConfig config;


        [Inject]
        private void Construct(IFloor floor, FloorConfig config)
        {
            this.floor = floor;
            this.config = config;
        }


        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                seed.Value = PickSeed();
            }

            floor.Build(seed.Value, transform);
        }


        private int PickSeed()
        {
            if (config.seed != 0)
            {
                return config.seed;
            }

            return Random.Range(RANDOM_SEED_MIN, int.MaxValue);
        }
    }
}
