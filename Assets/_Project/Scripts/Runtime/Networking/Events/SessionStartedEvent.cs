namespace NoWayOut.Networking.Events
{
    public readonly struct SessionStartedEvent
    {
        public bool IsHost { get; }


        public SessionStartedEvent(bool isHost)
        {
            IsHost = isHost;
        }
    }
}
