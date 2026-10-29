using System.Collections.Generic;
using NoWayOut.Gameplay.World.Weather;
using NUnit.Framework;
using UnityEngine;

namespace NoWayOut.Tests.EditMode.World
{
    public sealed class WeatherServiceTests
    {
        private const float TRANSITION_SECONDS = 10f;
        private const int CALM = 0;
        private const int DOWNPOUR = 1;
        private const float TOLERANCE = 0.0001f;

        private WeatherConfig config;
        private WeatherService service;


        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<WeatherConfig>();
            config.transitionSeconds = TRANSITION_SECONDS;
            config.presets = new List<WeatherPreset>
            {
                new WeatherPreset { name = "Calm", rain = 0f, wind = 0.1f },
                new WeatherPreset { name = "Downpour", rain = 1f, wind = 0.5f, fog = 0.6f }
            };
            service = new WeatherService(config);
        }


        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }


        [Test]
        public void SnapTo_Preset_AppliesItImmediately()
        {
            service.SnapTo(DOWNPOUR);

            Assert.AreEqual(1f, service.Current.Rain, TOLERANCE);
            Assert.AreEqual(0.6f, service.Current.Fog, TOLERANCE);
        }


        [Test]
        public void Advance_HalfTransition_MovesHalfway()
        {
            service.SnapTo(CALM);
            service.SetTarget(DOWNPOUR);

            service.Advance(TRANSITION_SECONDS * 0.5f);

            Assert.AreEqual(0.5f, service.Current.Rain, TOLERANCE);
        }


        [Test]
        public void Advance_SteadyRain_ReachesFullWetnessAfterRiseTime()
        {
            service.SnapTo(CALM);
            service.SetTarget(DOWNPOUR);
            service.Advance(TRANSITION_SECONDS);

            service.Advance(config.wetRiseSeconds);

            Assert.AreEqual(1f, service.Wetness, TOLERANCE);
        }


        [Test]
        public void Advance_AfterRain_DriesSlowerThanItWets()
        {
            service.SnapTo(DOWNPOUR);
            service.SetTarget(CALM);
            service.Advance(TRANSITION_SECONDS);

            service.Advance(config.wetRiseSeconds);

            Assert.Greater(service.Wetness, 0f);
        }


        [Test]
        public void Advance_PastTransition_StopsAtTarget()
        {
            service.SnapTo(CALM);
            service.SetTarget(DOWNPOUR);

            service.Advance(TRANSITION_SECONDS * 3f);

            Assert.AreEqual(1f, service.Current.Rain, TOLERANCE);
            Assert.AreEqual(0.5f, service.Current.Wind, TOLERANCE);
        }
    }
}
