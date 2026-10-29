using System.Collections.Generic;

namespace NoWayOut.Gameplay.World
{
    internal sealed class FogField
    {
        private readonly List<FogLayer> layers = new List<FogLayer>();

        public int Count => layers.Count;


        public void Add(FogLayer layer)
        {
            layers.Add(layer);
        }


        public void Clear()
        {
            layers.Clear();
        }


        public FogLayer Get(int index)
        {
            return layers[index];
        }
    }
}
