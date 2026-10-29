using System.Collections.Generic;
using UnityEngine;

namespace NoWayOut.Gameplay.World
{
    [CreateAssetMenu(menuName = "No Way Out/World/Module Definition", fileName = "Module")]
    public sealed class WorldModuleDefinition : ScriptableObject
    {
        public GameObject prefab;
        public WorldModuleRole role;
        public float weight = 1f;
        public List<Vector3Int> cells = new List<Vector3Int> { Vector3Int.zero };
        public List<ModulePassage> passages = new List<ModulePassage>();
    }
}
