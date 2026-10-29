using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace NoWayOut.Gameplay.World.Weather
{
    [CreateAssetMenu(menuName = "No Way Out/Configs/Weather Config", fileName = "WeatherConfig")]
    public sealed class WeatherConfig : ScriptableObject
    {
        public List<WeatherPreset> presets = new List<WeatherPreset> { new WeatherPreset() };
        public int startPresetIndex = 0;
        public int debugPresetIndex = -1;
        public float transitionSeconds = 25f;
        public float wetRiseSeconds = 40f;
        public float wetDrySeconds = 120f;
        public float puddleDryOpacity = 0.35f;
        public float puddleDrySizeScale = 0.7f;
        public float puddleUpdateInterval = 0.5f;
        public float puddleRippleRate = 6f;
        public float puddleRippleRadius = 18f;
        public float puddleRippleCoreFraction = 0.3f;
        public int puddleRipplesPerFrameMax = 30;
        public float puddleRippleSizeMin = 0.15f;
        public float puddleRippleSizeMax = 0.35f;
        public float rainRateMax = 1800f;
        public float rainWindInfluence = 0.6f;
        public float ashRateMax = 140f;
        public float ashWindInfluence = 1f;
        public float ashDepthBoostPerMeter = 0.04f;
        public float particleWindForce = 0.15f;
        public float windSpeedMax = 6f;
        public float windYawDegrees = 35f;
        public float windYawWander = 60f;
        public float windWanderFrequency = 0.01f;
        public float gustFrequency = 0.12f;
        public float gustThreshold = 0.6f;
        public float rainFogWeight = 0.9f;
        public float ashFogWeight = 0.5f;
        public float gustClearWeight = 0.8f;
        public float fogWindInfluence = 0.5f;
        public float lightningIntervalMin = 4f;
        public float lightningIntervalMax = 25f;
        [FormerlySerializedAs("lightningIntensityLux")] public float lightningIntensity = 8f;
        public float lightningDuration = 0.45f;
        public int lightningPulsesMax = 3;
        public float lightningPitchMin = 45f;
        public float lightningPitchMax = 75f;
    }
}
