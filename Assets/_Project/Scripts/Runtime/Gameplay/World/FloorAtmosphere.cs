using NoWayOut.Gameplay.World.Generation;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NoWayOut.Gameplay.World
{
    internal sealed class FloorAtmosphere
    {
        private const string ROOT_NAME = "Atmosphere";
        private const float HALF = 0.5f;
        private const float FULL_TURN_DEGREES = 360f;
        private const float FULL_TURN_RADIANS = Mathf.PI * 2f;
        private const float SQUARE_METERS_PER_UNIT = 1000f;
        private const float FOG_BREATH_SEED_STEP = 37.1f;

        private readonly FloorAtmosphereConfig config;
        private readonly FloorConfig floorConfig;
        private readonly PuddleField puddles;
        private readonly FogField fogs;


        public FloorAtmosphere(FloorAtmosphereConfig config, FloorConfig floorConfig, PuddleField puddles, FogField fogs)
        {
            this.config = config;
            this.floorConfig = floorConfig;
            this.puddles = puddles;
            this.fogs = fogs;
        }


        public void Build(FloorLayout layout, Transform parent)
        {
            puddles.Clear();
            fogs.Clear();
            Bounds bounds = MeasureBounds(layout);
            Transform root = new GameObject(ROOT_NAME).transform;
            root.SetParent(parent, false);

            BuildMist(bounds, root);
            BuildFogDrift(bounds, root);
            BuildBlocks(layout, bounds, root);
            BuildPuddles(layout, root);
        }


        private Bounds MeasureBounds(FloorLayout layout)
        {
            float cellSize = floorConfig.cellSize;
            Vector3 cellExtent = Vector3.one * cellSize;
            Bounds bounds = new Bounds(GetCellCenter(layout.Modules[0].GetCell(0)), cellExtent);

            for (int i = 0; i < layout.Modules.Count; i++)
            {
                PlacedModule module = layout.Modules[i];
                for (int cell = 0; cell < module.CellCount; cell++)
                {
                    bounds.Encapsulate(new Bounds(GetCellCenter(module.GetCell(cell)), cellExtent));
                }
            }

            return bounds;
        }


        private void BuildMist(Bounds bounds, Transform root)
        {
            float top = bounds.min.y - config.mistTopOffset;
            Vector3 center = new Vector3(bounds.center.x, top - config.mistDepth * HALF, bounds.center.z);
            Vector3 size = new Vector3(bounds.size.x + config.horizontalMargin * 2f, config.mistDepth, bounds.size.z + config.horizontalMargin * 2f);
            SpawnFogVolume(config.mistPrefab, root, center, size, config.mistPatchSize, config.mistBreathAmplitude, config.mistBreathPeriod);
        }


        private void BuildFogDrift(Bounds bounds, Transform root)
        {
            float bottom = bounds.min.y - config.mistTopOffset;
            float top = bounds.max.y + config.driftFogHeadroom;
            Vector3 center = new Vector3(bounds.center.x, (bottom + top) * HALF, bounds.center.z);
            Vector3 size = new Vector3(bounds.size.x + config.horizontalMargin * 2f, top - bottom, bounds.size.z + config.horizontalMargin * 2f);
            SpawnFogVolume(config.driftFogPrefab, root, center, size, config.driftFogPatchSize, config.driftFogBreathAmplitude, config.driftFogBreathPeriod);
            SpawnFogVolume(config.wispFogPrefab, root, center, size, config.wispFogPatchSize, config.wispFogBreathAmplitude, config.wispFogBreathPeriod);
        }


        private void SpawnFogVolume(LocalVolumetricFog prefab, Transform root, Vector3 center, Vector3 size, float patchSize, float breathAmplitude, float breathPeriod)
        {
            LocalVolumetricFog fog = Object.Instantiate(prefab, root);
            fog.transform.localPosition = center;

            LocalVolumetricFogArtistParameters parameters = fog.parameters;
            parameters.size = size;
            parameters.textureTiling = size / patchSize;
            fog.parameters = parameters;

            float breathSeed = (fogs.Count + 1) * FOG_BREATH_SEED_STEP;
            fogs.Add(new FogLayer(fog, patchSize, breathAmplitude, breathPeriod, breathSeed));
        }


        private void BuildBlocks(FloorLayout layout, Bounds bounds, Transform root)
        {
            DeterministicRandom random = new DeterministicRandom(layout.Seed + config.blockSeedOffset);
            Vector3 shell = new Vector3(config.blockShellMargin, config.blockVerticalMargin, config.blockShellMargin);
            Vector3 min = bounds.min - shell;
            Vector3 max = bounds.max + shell;
            float footprint = (max.x - min.x) * (max.z - min.z);
            int count = Mathf.Min(config.blockCountMax, Mathf.RoundToInt(footprint * config.blocksPerThousandSquareMeters / SQUARE_METERS_PER_UNIT));
            int attempts = count * config.blockAttemptsPerBlock;
            int placed = 0;

            for (int i = 0; i < attempts && placed < count; i++)
            {
                Vector3 size = PickBlockSize(random);
                Vector3 position = new Vector3(Lerp(random, min.x, max.x), Lerp(random, min.y, max.y), Lerp(random, min.z, max.z));
                float reach = size.magnitude * HALF + config.blockClearance;

                if (IsNearFloor(layout, position, reach))
                {
                    continue;
                }

                SpawnBlock(random, root, position, size);
                placed++;
            }
        }


        private Vector3 PickBlockSize(DeterministicRandom random)
        {
            float width = Lerp(random, config.blockSizeMin, config.blockSizeMax);
            float depth = Lerp(random, config.blockSizeMin, config.blockSizeMax);
            float height = Lerp(random, config.blockSizeMin, config.blockSizeMax);

            if (random.Chance(config.blockSlabChance))
            {
                height = Mathf.Max(config.blockSizeMin, Mathf.Max(width, depth) * config.blockSlabThickness);
            }

            return new Vector3(width, height, depth);
        }


        private void SpawnBlock(DeterministicRandom random, Transform root, Vector3 position, Vector3 size)
        {
            VoidDrift block = Object.Instantiate(config.blockPrefab, root);
            Transform blockTransform = block.transform;
            float tiltX = Lerp(random, -config.blockTiltMax, config.blockTiltMax);
            float tiltZ = Lerp(random, -config.blockTiltMax, config.blockTiltMax);
            float yaw = random.NextFloat() * FULL_TURN_DEGREES;

            blockTransform.localPosition = position;
            blockTransform.localRotation = Quaternion.Euler(tiltX, yaw, tiltZ);
            blockTransform.localScale = size;

            float amplitude = Lerp(random, config.driftAmplitudeMin, config.driftAmplitudeMax);
            float period = Lerp(random, config.driftPeriodMin, config.driftPeriodMax);
            float phase = random.NextFloat() * FULL_TURN_RADIANS;
            Vector3 spinAxis = new Vector3(Lerp(random, -1f, 1f), 1f, Lerp(random, -1f, 1f)).normalized;
            float spinSpeed = Lerp(random, -config.spinSpeedMax, config.spinSpeedMax);
            block.Begin(amplitude, period, phase, spinAxis, spinSpeed);
        }


        private void BuildPuddles(FloorLayout layout, Transform root)
        {
            DeterministicRandom random = new DeterministicRandom(layout.Seed + config.puddleSeedOffset);

            for (int i = 0; i < layout.Modules.Count; i++)
            {
                PlacedModule module = layout.Modules[i];
                WorldModuleRole role = module.Definition.role;
                if (role == WorldModuleRole.Stair)
                {
                    continue;
                }

                float chance = role == WorldModuleRole.Bridge ? config.puddleChanceOnBridge : config.puddleChancePerCell;
                for (int cell = 0; cell < module.CellCount; cell++)
                {
                    if (random.Chance(chance))
                    {
                        SpawnPuddle(random, root, module.GetCell(cell));
                    }
                }
            }
        }


        private void SpawnPuddle(DeterministicRandom random, Transform root, Vector3Int cell)
        {
            float cellSize = floorConfig.cellSize;
            float offsetX = Lerp(random, -config.puddleOffsetMax, config.puddleOffsetMax);
            float offsetZ = Lerp(random, -config.puddleOffsetMax, config.puddleOffsetMax);
            float width = Lerp(random, config.puddleSizeMin, config.puddleSizeMax);
            float length = width * Lerp(random, config.puddleAspectMin, 1f);
            float yaw = random.NextFloat() * FULL_TURN_DEGREES;

            DecalProjector puddle = Object.Instantiate(config.puddlePrefab, root);
            puddle.transform.localPosition = new Vector3(cell.x * cellSize + offsetX, cell.y * cellSize + config.puddleHeight, cell.z * cellSize + offsetZ);
            puddle.transform.localRotation = Quaternion.Euler(90f, yaw, 0f);
            puddle.size = new Vector3(width, length, config.puddleDepth);
            puddle.pivot = new Vector3(0f, 0f, config.puddleDepth * HALF);

            float cellScale = 1f / config.puddleAtlasCells;
            int column = random.Range(0, config.puddleAtlasCells);
            int row = random.Range(0, config.puddleAtlasCells);
            puddle.uvScale = new Vector2(cellScale, cellScale);
            puddle.uvBias = new Vector2(column * cellScale, row * cellScale);
            puddles.Add(new Puddle(puddle, config.puddleHeight));
        }


        private bool IsNearFloor(FloorLayout layout, Vector3 position, float reach)
        {
            float cellSize = floorConfig.cellSize;
            int minX = Mathf.RoundToInt((position.x - reach) / cellSize);
            int maxX = Mathf.RoundToInt((position.x + reach) / cellSize);
            int minY = Mathf.FloorToInt((position.y - reach) / cellSize);
            int maxY = Mathf.FloorToInt((position.y + reach) / cellSize);
            int minZ = Mathf.RoundToInt((position.z - reach) / cellSize);
            int maxZ = Mathf.RoundToInt((position.z + reach) / cellSize);

            for (int x = minX; x <= maxX; x++)
            {
                for (int y = minY; y <= maxY; y++)
                {
                    for (int z = minZ; z <= maxZ; z++)
                    {
                        if (layout.TryGetModuleAt(new Vector3Int(x, y, z), out _))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }


        private Vector3 GetCellCenter(Vector3Int cell)
        {
            float cellSize = floorConfig.cellSize;
            return new Vector3(cell.x * cellSize, (cell.y + HALF) * cellSize, cell.z * cellSize);
        }


        private static float Lerp(DeterministicRandom random, float min, float max)
        {
            return Mathf.Lerp(min, max, random.NextFloat());
        }
    }
}
