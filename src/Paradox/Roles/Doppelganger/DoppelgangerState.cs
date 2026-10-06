namespace Paradox.Roles.Doppelganger;

public sealed class DoppelgangerState
{
    public byte PlayerId { get; }
    public byte? TargetPlayerId { get; private set; }
    public float DisguiseEndsAt { get; private set; }
    public float CooldownEndsAt { get; private set; }

    public bool IsDisguised(float now) =>
        TargetPlayerId.HasValue && now < DisguiseEndsAt;

    public bool IsReady(float now) => now >= CooldownEndsAt;

    public DoppelgangerState(byte playerId)
    {
        PlayerId = playerId;
    }

    public bool TryStart(byte targetPlayerId, float now, float durationSeconds, float cooldownSeconds)
    {
        if (!IsReady(now) || targetPlayerId == PlayerId)
            return false;

        TargetPlayerId = targetPlayerId;
        DisguiseEndsAt = now + durationSeconds;
        CooldownEndsAt = now + cooldownSeconds;
        return true;
    }

    public void ClearDisguise()
    {
        TargetPlayerId = null;
        DisguiseEndsAt = 0f;
    }
}
