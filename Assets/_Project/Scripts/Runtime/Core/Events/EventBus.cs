using System;
using System.Collections.Generic;

namespace NoWayOut.Core.Events
{
    public sealed class EventBus : IEventBus
    {
        private readonly Dictionary<Type, object> channels = new Dictionary<Type, object>();


        public void Publish<TEvent>(TEvent payload) where TEvent : struct
        {
            if (channels.TryGetValue(typeof(TEvent), out object channel))
            {
                ((EventChannel<TEvent>)channel).Publish(payload);
            }
        }


        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct
        {
            EventChannel<TEvent> channel = GetOrCreateChannel<TEvent>();
            return channel.Subscribe(handler);
        }


        private EventChannel<TEvent> GetOrCreateChannel<TEvent>() where TEvent : struct
        {
            if (channels.TryGetValue(typeof(TEvent), out object existingChannel))
            {
                return (EventChannel<TEvent>)existingChannel;
            }

            EventChannel<TEvent> channel = new EventChannel<TEvent>();
            channels.Add(typeof(TEvent), channel);
            return channel;
        }
    }
}
