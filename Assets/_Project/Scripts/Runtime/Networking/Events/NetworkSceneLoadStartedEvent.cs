namespace NoWayOut.Networking.Events
{
    public readonly struct NetworkSceneLoadStartedEvent
    {
        public string SceneName { get; }


        public NetworkSceneLoadStartedEvent(string sceneName)
        {
            SceneName = sceneName;
        }
    }
}
