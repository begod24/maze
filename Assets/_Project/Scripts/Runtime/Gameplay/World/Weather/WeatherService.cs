using UnityEngine;
using VContainer.Unity;

namespace NoWayOut.Gameplay.World.Weather
{
    internal sealed class WeatherService : IWeather, ITickable
    {
        private readonly WeatherConfig config;
        private WeatherSample target;

        public WeatherSample Current { get; private set; }
        public float Wetness { get; private set; }


        public WeatherService(WeatherConfig config)
        {
            this.config = config;
        }


        public void SetTarget(int presetIndex)
        {
            target = WeatherSample.From(config.presets[presetIndex]);
        }


        public void SnapTo(int presetIndex)
        {
            SetTarget(presetIndex);
            Current = target;
            Wetness = target.Rain;
        }


        public void Tick()
        {
            Advance(Time.deltaTime);
        }


        public void Advance(float deltaTime)
        {
            Current = WeatherSample.MoveTowards(Current, target, deltaTime / config.transitionSeconds);

            float seconds = Current.Rain > Wetness ? config.wetRiseSeconds : config.wetDrySeconds;
            Wetness = Mathf.MoveTowards(Wetness, Current.Rain, deltaTime / seconds);
        }
    }
}
