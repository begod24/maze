using System;
using System.Collections.Generic;
using NoWayOut.Core.Events;
using NUnit.Framework;

namespace NoWayOut.Tests.EditMode.Core
{
    public sealed class EventBusTests
    {
        private EventBus eventBus;


        [SetUp]
        public void SetUp()
        {
            eventBus = new EventBus();
        }


        [Test]
        public void Publish_WithSubscriber_DeliversPayload()
        {
            List<int> received = new List<int>();
            eventBus.Subscribe<TestEvent>(payload => received.Add(payload.Value));

            eventBus.Publish(new TestEvent(7));

            CollectionAssert.AreEqual(new[] { 7 }, received);
        }


        [Test]
        public void Publish_AfterSubscriptionDisposed_DoesNotDeliver()
        {
            int calls = 0;
            IDisposable subscription = eventBus.Subscribe<TestEvent>(_ => calls++);
            subscription.Dispose();

            eventBus.Publish(new TestEvent(1));

            Assert.AreEqual(0, calls);
        }


        [Test]
        public void Publish_HandlerUnsubscribesDuringPublish_DoesNotThrow()
        {
            IDisposable subscription = null;
            subscription = eventBus.Subscribe<TestEvent>(_ => subscription.Dispose());
            eventBus.Subscribe<TestEvent>(_ => { });

            Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent(1)));
        }


        [Test]
        public void Publish_WithoutSubscribers_DoesNotThrow()
        {
            Assert.DoesNotThrow(() => eventBus.Publish(new TestEvent(1)));
        }


        private readonly struct TestEvent
        {
            public int Value { get; }


            public TestEvent(int value)
            {
                Value = value;
            }
        }
    }
}
