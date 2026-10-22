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

    public float CooldownRemaining(float now) => Math.Max(0f, CooldownEndsAt - now);

    public float DisguiseRemaining(float now) =>
        IsDisguised(now) ? Math.Max(0f, DisguiseEndsAt - now) : 0f;

    public DoppelgangerState(byte playerId)
    {
        PlayerId = playerId;
    }

    public bool TryStart(byte targetPlayerId, float now, float durationSeconds, float cooldownSeconds)
    {
        // Reject overlapping disguises even when a host chooses a cooldown
        // shorter than the disguise duration.
        if (!IsReady(now) || TargetPlayerId.HasValue ||
            targetPlayerId == PlayerId || targetPlayerId == byte.MaxValue)
            return false;

        TargetPlayerId = targetPlayerId;
        DisguiseEndsAt = now + durationSeconds;
        CooldownEndsAt = now + cooldownSeconds;
        return true;
    }

    public void ApplySyncedStart(
        byte targetPlayerId,
        float now,
        float durationSeconds,
        float cooldownSeconds)
    {
        TargetPlayerId = targetPlayerId;
        DisguiseEndsAt = now + Math.Max(0f, durationSeconds);
        CooldownEndsAt = now + Math.Max(0f, cooldownSeconds);
    }

    public void ClearDisguise()
    {
        TargetPlayerId = null;
        DisguiseEndsAt = 0f;
    }

    public void ApplySyncedStop(float now, float cooldownRemainingSeconds)
    {
        ClearDisguise();
        // The host sends remaining cooldown after an early cancellation
        // (meeting, death, disconnect). Do not leave clients on a stale clock.
        CooldownEndsAt = now + Math.Max(0f, cooldownRemainingSeconds);
    }

    public void CancelStart()
    {
        ClearDisguise();
        CooldownEndsAt = 0f;
    }
}
