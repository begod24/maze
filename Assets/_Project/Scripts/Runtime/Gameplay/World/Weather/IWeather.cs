namespace NoWayOut.Gameplay.World.Weather
{
    internal interface IWeather
    {
        WeatherSample Current { get; }

        float Wetness { get; }

        void SetTarget(int presetIndex);

        void SnapTo(int presetIndex);
    }
}
