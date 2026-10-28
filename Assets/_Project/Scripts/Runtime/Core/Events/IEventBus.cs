using System;

namespace NoWayOut.Core.Events
{
    public interface IEventBus
    {
        void Publish<TEvent>(TEvent payload) where TEvent : struct;

        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct;
    }
}
