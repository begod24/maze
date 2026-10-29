using System;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class FloorGenerator
    {
        private const int ATTEMPT_SEED_STEP = 7919;

        private readonly FloorConfig config;
        private readonly LinearFloorTemplate linearTemplate;


        public FloorGenerator(FloorConfig config, WorldModuleCatalog catalog)
        {
            this.config = config;
            ModulePicker picker = new ModulePicker(catalog);
            ModulePlacer placer = new ModulePlacer(config);
            linearTemplate = new LinearFloorTemplate(config, picker, placer);
        }


        public FloorLayout Generate(int seed)
        {
            for (int attempt = 0; attempt < config.maxAttempts; attempt++)
            {
                int attemptSeed = unchecked(seed + attempt * ATTEMPT_SEED_STEP);
                DeterministicRandom random = new DeterministicRandom(attemptSeed);
                FloorLayout layout = linearTemplate.TryBuild(seed, random);

                if (layout != null && FloorLayoutValidator.IsValid(layout))
                {
                    return layout;
                }
            }

            throw new InvalidOperationException("No valid floor after " + config.maxAttempts + " attempts for seed " + seed);
        }
    }
}
