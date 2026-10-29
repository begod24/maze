using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace NoWayOut.Gameplay.World
{
    internal readonly struct Puddle
    {
        public DecalProjector Projector { get; }
        public Vector3 BaseSize { get; }
        public float SurfaceDepth { get; }


        public Puddle(DecalProjector projector, float surfaceDepth)
        {
            Projector = projector;
            BaseSize = projector.size;
            SurfaceDepth = surfaceDepth;
        }
    }
}
