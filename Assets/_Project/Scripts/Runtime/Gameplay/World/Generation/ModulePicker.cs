using System;

namespace NoWayOut.Gameplay.World.Generation
{
    internal sealed class ModulePicker
    {
        private readonly WorldModuleCatalog catalog;


        public ModulePicker(WorldModuleCatalog catalog)
        {
            this.catalog = catalog;
        }


        public WorldModuleDefinition GetSingle(WorldModuleRole role)
        {
            foreach (WorldModuleDefinition definition in catalog.modules)
            {
                if (definition.role == role)
                {
                    return definition;
                }
            }

            throw new InvalidOperationException("WorldModuleCatalog has no module with role " + role);
        }


        public WorldModuleDefinition PickWeighted(DeterministicRandom random, params WorldModuleRole[] roles)
        {
            float totalWeight = 0f;
            foreach (WorldModuleDefinition definition in catalog.modules)
            {
                if (HasRole(definition, roles))
                {
                    totalWeight += definition.weight;
                }
            }

            float roll = random.NextFloat() * totalWeight;
            WorldModuleDefinition last = null;
            foreach (WorldModuleDefinition definition in catalog.modules)
            {
                if (!HasRole(definition, roles))
                {
                    continue;
                }

                last = definition;
                roll -= definition.weight;
                if (roll <= 0f)
                {
                    return definition;
                }
            }

            if (last == null)
            {
                throw new InvalidOperationException("WorldModuleCatalog has no module for the requested roles");
            }

            return last;
        }


        private static bool HasRole(WorldModuleDefinition definition, WorldModuleRole[] roles)
        {
            foreach (WorldModuleRole role in roles)
            {
                if (definition.role == role)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
