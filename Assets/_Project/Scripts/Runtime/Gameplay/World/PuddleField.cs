using System.Collections.Generic;

namespace NoWayOut.Gameplay.World
{
    internal sealed class PuddleField
    {
        private readonly List<Puddle> puddles = new List<Puddle>();

        public int Count => puddles.Count;


        public void Add(Puddle puddle)
        {
            puddles.Add(puddle);
        }


        public void Clear()
        {
            puddles.Clear();
        }


        public Puddle Get(int index)
        {
            return puddles[index];
        }
    }
}
