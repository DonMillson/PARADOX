namespace Paradox.Roles.Parasite;

public sealed class ParasiteInfection
{
    public byte SourcePlayerId { get; }
    public byte TargetPlayerId { get; }
    public float StartedAt { get; }
    public float CompletesAt { get; }

    public ParasiteInfection(byte sourcePlayerId, byte targetPlayerId, float startedAt, float durationSeconds)
    {
        SourcePlayerId = sourcePlayerId;
        TargetPlayerId = targetPlayerId;
        StartedAt = startedAt;
        CompletesAt = startedAt + durationSeconds;
    }

    public bool IsComplete(float now) => now >= CompletesAt;
}
