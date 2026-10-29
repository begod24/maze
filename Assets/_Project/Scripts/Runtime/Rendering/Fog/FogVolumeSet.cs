using System.Collections.Generic;
using UnityEngine;

namespace NoWayOut.Rendering.Fog
{
    [CreateAssetMenu(menuName = "No Way Out/Rendering/Fog Volume Set", fileName = "FogVolumeSet")]
    public sealed class FogVolumeSet : ScriptableObject
    {
        private readonly List<LocalFogVolume> volumes = new List<LocalFogVolume>();

        public int Count => volumes.Count;


        public LocalFogVolume Get(int index)
        {
            return volumes[index];
        }


        public void Add(LocalFogVolume volume)
        {
            if (!volumes.Contains(volume))
            {
                volumes.Add(volume);
            }
        }


        public void Remove(LocalFogVolume volume)
        {
            volumes.Remove(volume);
        }
    }
}
