using System;
using System.Collections.Generic;

namespace NoWayOut.Core.Events
{
    internal sealed class EventChannel<TEvent> where TEvent : struct
    {
        private readonly List<Action<TEvent>> handlers = new List<Action<TEvent>>();
        private Action<TEvent>[] snapshot = Array.Empty<Action<TEvent>>();
        private bool isSnapshotDirty;


        public void Publish(TEvent payload)
        {
            if (isSnapshotDirty)
            {
                snapshot = handlers.ToArray();
                isSnapshotDirty = false;
            }

            foreach (Action<TEvent> handler in snapshot)
            {
                handler(payload);
            }
        }


        public IDisposable Subscribe(Action<TEvent> handler)
        {
            handlers.Add(handler);
            isSnapshotDirty = true;
            return new Subscription(this, handler);
        }


        private void Unsubscribe(Action<TEvent> handler)
        {
            if (handlers.Remove(handler))
            {
                isSnapshotDirty = true;
            }
        }


        private sealed class Subscription : IDisposable
        {
            private readonly EventChannel<TEvent> channel;
            private Action<TEvent> handler;


            public Subscription(EventChannel<TEvent> channel, Action<TEvent> handler)
            {
                this.channel = channel;
                this.handler = handler;
            }


            public void Dispose()
            {
                if (handler == null)
                {
                    return;
                }

                channel.Unsubscribe(handler);
                handler = null;
            }
        }
    }
}
