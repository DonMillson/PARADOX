namespace Paradox.Roles.Observer;

public sealed record ObserverTrace(
    byte SourcePlayerId,
    float CreatedAt,
    float LifetimeSeconds = 12f)
{
    public bool IsExpired(float now) => now - CreatedAt >= LifetimeSeconds;
}
