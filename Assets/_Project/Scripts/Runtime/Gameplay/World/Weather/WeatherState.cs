using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace NoWayOut.Gameplay.World.Weather
{
    public sealed class WeatherState : NetworkBehaviour
    {
        private const int NO_DEBUG_PRESET = -1;

        private readonly NetworkVariable<int> presetIndex = new NetworkVariable<int>();
        private IWeather weather;
        private WeatherConfig config;
        private float nextChangeTime;


        [Inject]
        private void Construct(IWeather weather, WeatherConfig config)
        {
            this.weather = weather;
            this.config = config;
        }


        public override void OnNetworkSpawn()
        {
            if (IsServer)
            {
                StartSchedule();
            }

            weather.SnapTo(presetIndex.Value);
            presetIndex.OnValueChanged += HandlePresetChanged;
        }


        public override void OnNetworkDespawn()
        {
            presetIndex.OnValueChanged -= HandlePresetChanged;
        }


        private void Update()
        {
            if (!IsSpawned || !IsServer || config.debugPresetIndex != NO_DEBUG_PRESET)
            {
                return;
            }

            if (Time.time >= nextChangeTime)
            {
                SetPreset(PickNextPreset());
            }
        }


        private void StartSchedule()
        {
            if (config.debugPresetIndex != NO_DEBUG_PRESET)
            {
                presetIndex.Value = config.debugPresetIndex;
                return;
            }

            SetPreset(config.startPresetIndex);
        }


        private void SetPreset(int index)
        {
            WeatherPreset preset = config.presets[index];
            presetIndex.Value = index;
            nextChangeTime = Time.time + Random.Range(preset.durationMin, preset.durationMax);
        }


        private int PickNextPreset()
        {
            float totalWeight = 0f;
            for (int i = 0; i < config.presets.Count; i++)
            {
                if (i != presetIndex.Value)
                {
                    totalWeight += config.presets[i].weight;
                }
            }

            float roll = Random.value * totalWeight;
            int last = presetIndex.Value;
            for (int i = 0; i < config.presets.Count; i++)
            {
                if (i == presetIndex.Value)
                {
                    continue;
                }

                last = i;
                roll -= config.presets[i].weight;
                if (roll <= 0f)
                {
                    return i;
                }
            }

            return last;
        }


        private void HandlePresetChanged(int previous, int current)
        {
            weather.SetTarget(current);
        }
    }
}
