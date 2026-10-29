using System;

namespace NoWayOut.Gameplay.World.Weather
{
    [Serializable]
    public sealed class WeatherPreset
    {
        public string name = "Calm";
        public float rain;
        public float storm;
        public float ash;
        public float wind = 0.1f;
        public float fog;
        public float weight = 1f;
        public float durationMin = 60f;
        public float durationMax = 120f;
    }
}
