using System.Collections.Generic;
using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    [CreateAssetMenu(menuName = "No Way Out/World/Module Catalog", fileName = "WorldModuleCatalog")]
    public sealed class WorldModuleCatalog : ScriptableObject
    {
        public List<WorldModuleDefinition> modules = new List<WorldModuleDefinition>();
    }
}
