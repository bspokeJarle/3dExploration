namespace Domain
{
    public interface IGameEvent
    {
        GameEventType Type { get; }
        I3dObject? Source { get; }
        string? ObjectName { get; }
        bool HasPowerUp { get; }
    }
}
