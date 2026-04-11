using Domain;

namespace CommonUtilities.Events
{
    public class GameEvent : IGameEvent
    {
        public GameEventType Type { get; init; }
        public I3dObject? Source { get; init; }
        public string? ObjectName { get; init; }
        public bool HasPowerUp { get; init; }
    }
}
