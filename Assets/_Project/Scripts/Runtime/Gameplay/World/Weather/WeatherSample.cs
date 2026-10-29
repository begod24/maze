using UnityEngine;

namespace NoWayOut.Gameplay.World.Weather
{
    internal readonly struct WeatherSample
    {
        public float Rain { get; }
        public float Storm { get; }
        public float Ash { get; }
        public float Wind { get; }
        public float Fog { get; }


        public WeatherSample(float rain, float storm, float ash, float wind, float fog)
        {
            Rain = rain;
            Storm = storm;
            Ash = ash;
            Wind = wind;
            Fog = fog;
        }


        public static WeatherSample From(WeatherPreset preset)
        {
            return new WeatherSample(preset.rain, preset.storm, preset.ash, preset.wind, preset.fog);
        }


        public static WeatherSample MoveTowards(WeatherSample from, WeatherSample to, float maxDelta)
        {
            return new WeatherSample(
                Mathf.MoveTowards(from.Rain, to.Rain, maxDelta),
                Mathf.MoveTowards(from.Storm, to.Storm, maxDelta),
                Mathf.MoveTowards(from.Ash, to.Ash, maxDelta),
                Mathf.MoveTowards(from.Wind, to.Wind, maxDelta),
                Mathf.MoveTowards(from.Fog, to.Fog, maxDelta));
        }
    }
}
