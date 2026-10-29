using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using VContainer;

namespace NoWayOut.Gameplay.World.Weather
{
    public sealed class WeatherView : MonoBehaviour
    {
        private const float FULL_TURN_DEGREES = 360f;
        private const float GUST_SEED = 17.3f;
        private const float YAW_SEED = 41.7f;
        private const float PULSE_SHARPNESS = 3f;
        private const float NOISE_CENTER = 0.5f;
        private const float NOISE_SCALE = 2f;
        private const float GUST_BASE = 0.5f;
        private const float FLASH_FADE = 0.5f;
        private const float FLASH_INTERVAL_JITTER_MIN = 0.5f;
        private const float FLASH_INTERVAL_JITTER_MAX = 1.5f;
        private const float RIPPLE_LIFT = 0.01f;

        [SerializeField] private ParticleSystem rain;
        [SerializeField] private ParticleSystem ash;
        [SerializeField] private ParticleSystem puddleRipples;
        [SerializeField] private ParticleSystem[] windDriven;
        [SerializeField] private Light lightning;
        [SerializeField] private DecalProjector wetSurface;
        [SerializeField] private Volume denseFogVolume;
        [SerializeField] private Volume clearFogVolume;

        private readonly List<int> nearbyPuddles = new List<int>();
        private readonly List<Vector3> fogScrolls = new List<Vector3>();
        private IWeather weather;
        private WeatherConfig config;
        private PuddleField puddles;
        private FogField fogs;
        private float nextFlashTime;
        private float flashStartTime = float.NegativeInfinity;
        private int flashPulses;
        private float nextPuddleUpdateTime;
        private float rippleBudget;


        [Inject]
        private void Construct(IWeather weather, WeatherConfig config, PuddleField puddles, FogField fogs)
        {
            this.weather = weather;
            this.config = config;
            this.puddles = puddles;
            this.fogs = fogs;
        }


        private void OnEnable()
        {
            lightning.enabled = false;
        }


        private void LateUpdate()
        {
            if (weather == null)
            {
                return;
            }

            WeatherSample sample = weather.Current;
            float time = Time.time;
            float gust = Mathf.PerlinNoise(time * config.gustFrequency, GUST_SEED);
            Vector3 wind = EvaluateWind(sample.Wind, gust, time);

            ApplyRain(sample.Rain, wind);
            ApplyAsh(sample.Ash, wind);
            ApplyWindDriven(wind);
            ApplyFog(sample, gust);
            AnimateFogLayers(wind, time);
            ApplyWetness(weather.Wetness, time);
            EmitPuddleRipples(sample.Rain);
            UpdateLightning(sample.Storm, time);
        }


        private void ApplyWetness(float wetness, float time)
        {
            wetSurface.fadeFactor = wetness;
            if (time < nextPuddleUpdateTime)
            {
                return;
            }

            nextPuddleUpdateTime = time + config.puddleUpdateInterval;
            float opacity = Mathf.Lerp(config.puddleDryOpacity, 1f, wetness);
            float scale = Mathf.Lerp(config.puddleDrySizeScale, 1f, wetness);

            for (int i = 0; i < puddles.Count; i++)
            {
                Puddle puddle = puddles.Get(i);
                if (puddle.Projector == null)
                {
                    continue;
                }

                puddle.Projector.fadeFactor = opacity;
                puddle.Projector.size = new Vector3(puddle.BaseSize.x * scale, puddle.BaseSize.y * scale, puddle.BaseSize.z);
            }
        }


        private void EmitPuddleRipples(float intensity)
        {
            CollectNearbyPuddles();
            if (intensity <= 0f || nearbyPuddles.Count == 0)
            {
                rippleBudget = 0f;
                return;
            }

            rippleBudget += intensity * config.puddleRippleRate * nearbyPuddles.Count * Time.deltaTime;
            int emitted = 0;
            while (rippleBudget >= 1f && emitted < config.puddleRipplesPerFrameMax)
            {
                EmitRipple(puddles.Get(nearbyPuddles[Random.Range(0, nearbyPuddles.Count)]));
                rippleBudget -= 1f;
                emitted++;
            }
        }


        private void CollectNearbyPuddles()
        {
            nearbyPuddles.Clear();
            Vector3 center = transform.position;
            float radiusSquared = config.puddleRippleRadius * config.puddleRippleRadius;

            for (int i = 0; i < puddles.Count; i++)
            {
                DecalProjector projector = puddles.Get(i).Projector;
                if (projector != null && (projector.transform.position - center).sqrMagnitude <= radiusSquared)
                {
                    nearbyPuddles.Add(i);
                }
            }
        }


        private void EmitRipple(Puddle puddle)
        {
            Vector3 size = puddle.Projector.size;
            Vector2 point = Random.insideUnitCircle * config.puddleRippleCoreFraction;
            Vector3 local = new Vector3(point.x * size.x, point.y * size.y, puddle.SurfaceDepth);
            Vector3 position = puddle.Projector.transform.TransformPoint(local) + Vector3.up * RIPPLE_LIFT;

            ParticleSystem.EmitParams emit = default;
            emit.position = position;
            emit.startSize = Random.Range(config.puddleRippleSizeMin, config.puddleRippleSizeMax);
            emit.applyShapeToPosition = false;
            puddleRipples.Emit(emit, 1);
        }


        private Vector3 EvaluateWind(float strength, float gust, float time)
        {
            float wander = (Mathf.PerlinNoise(time * config.windWanderFrequency, YAW_SEED) - NOISE_CENTER) * NOISE_SCALE;
            float yaw = (config.windYawDegrees + wander * config.windYawWander) * Mathf.Deg2Rad;
            float speed = strength * config.windSpeedMax * (GUST_BASE + gust);
            return new Vector3(Mathf.Sin(yaw), 0f, Mathf.Cos(yaw)) * speed;
        }


        private void ApplyRain(float intensity, Vector3 wind)
        {
            ParticleSystem.EmissionModule emission = rain.emission;
            emission.rateOverTime = intensity * config.rainRateMax;

            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.x = wind.x * config.rainWindInfluence;
            velocity.z = wind.z * config.rainWindInfluence;
        }


        private void ApplyAsh(float intensity, Vector3 wind)
        {
            float depthBoost = 1f + Mathf.Max(0f, -transform.position.y) * config.ashDepthBoostPerMeter;
            ParticleSystem.EmissionModule emission = ash.emission;
            emission.rateOverTime = intensity * config.ashRateMax * depthBoost;

            ParticleSystem.ForceOverLifetimeModule force = ash.forceOverLifetime;
            force.x = wind.x * config.ashWindInfluence;
            force.z = wind.z * config.ashWindInfluence;
        }


        private void ApplyWindDriven(Vector3 wind)
        {
            for (int i = 0; i < windDriven.Length; i++)
            {
                ParticleSystem.ForceOverLifetimeModule force = windDriven[i].forceOverLifetime;
                force.x = wind.x * config.particleWindForce;
                force.z = wind.z * config.particleWindForce;
            }
        }


        private void ApplyFog(WeatherSample sample, float gust)
        {
            float dense = Mathf.Max(sample.Fog, Mathf.Max(sample.Rain * config.rainFogWeight, sample.Ash * config.ashFogWeight));
            float gustPeak = Mathf.Clamp01((gust - config.gustThreshold) / (1f - config.gustThreshold));
            denseFogVolume.weight = Mathf.Clamp01(dense);
            clearFogVolume.weight = sample.Wind * gustPeak * config.gustClearWeight;
        }


        private void AnimateFogLayers(Vector3 wind, float time)
        {
            while (fogScrolls.Count < fogs.Count)
            {
                fogScrolls.Add(Vector3.zero);
            }

            for (int i = 0; i < fogs.Count; i++)
            {
                FogLayer layer = fogs.Get(i);
                if (layer.Volume == null)
                {
                    continue;
                }

                Vector3 velocity = layer.BaseScroll - wind * (config.fogWindInfluence / layer.PatchSize);
                Vector3 scroll = fogScrolls[i] + velocity * Time.deltaTime;
                scroll = new Vector3(Mathf.Repeat(scroll.x, 1f), Mathf.Repeat(scroll.y, 1f), Mathf.Repeat(scroll.z, 1f));
                fogScrolls[i] = scroll;

                float breath = (Mathf.PerlinNoise(time / layer.BreathPeriod, layer.BreathSeed) - NOISE_CENTER) * NOISE_SCALE;
                layer.Volume.MeanFreePath = layer.BaseMeanFreePath / (1f + breath * layer.BreathAmplitude);
                layer.Volume.NoiseOffset = scroll;
            }
        }


        private void UpdateLightning(float storm, float time)
        {
            if (storm > 0f && time >= nextFlashTime && time > flashStartTime + config.lightningDuration)
            {
                StartFlash(time);
                float interval = Mathf.Lerp(config.lightningIntervalMax, config.lightningIntervalMin, storm);
                nextFlashTime = time + interval * Random.Range(FLASH_INTERVAL_JITTER_MIN, FLASH_INTERVAL_JITTER_MAX);
            }

            float elapsed = (time - flashStartTime) / config.lightningDuration;
            bool flashing = elapsed >= 0f && elapsed < 1f;
            lightning.enabled = flashing;
            if (!flashing)
            {
                return;
            }

            float pulsePhase = elapsed * flashPulses;
            float pulse = 1f - (pulsePhase - Mathf.Floor(pulsePhase));
            lightning.intensity = config.lightningIntensity * Mathf.Pow(pulse, PULSE_SHARPNESS) * (1f - elapsed * FLASH_FADE);
        }


        private void StartFlash(float time)
        {
            flashStartTime = time;
            flashPulses = Random.Range(1, config.lightningPulsesMax + 1);
            float pitch = Random.Range(config.lightningPitchMin, config.lightningPitchMax);
            float yaw = Random.value * FULL_TURN_DEGREES;
            lightning.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }
    }
}
