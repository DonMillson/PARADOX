namespace Paradox.Roles.ParadoxTeam;

/// <summary>Testable host rule state: countdown and one-time sidekick succession.</summary>
public sealed class ParadoxTeamRules
{
    public const float WinHoldSeconds = 15f;
    public const float RevealSeconds = 30f;
    public const float DirectionCooldownSeconds = 60f;
    public const float DirectionVisibleSeconds = 3f;
    public const float SuccessionMeterPenalty = 20f;
    private float _armedAt = -1f;
    private bool _ended;
    private bool _promoted;

    public bool PromotionUsed => _promoted;
    public float Remaining(float now) => _armedAt < 0f
        ? WinHoldSeconds : Math.Max(0f, WinHoldSeconds - (now - _armedAt));

    public bool TryPromote(bool leaderAlive, bool sidekickAlive)
    {
        if (_promoted || leaderAlive || !sidekickAlive)
            return false;
        _promoted = true;
        _armedAt = -1f;
        return true;
    }

    public bool Tick(float now, float meter, bool leaderAlive)
    {
        if (_ended)
            return false;
        if (!leaderAlive || meter < 100f)
        {
            _armedAt = -1f;
            return false;
        }
        if (_armedAt < 0f)
            _armedAt = now;
        if (now - _armedAt < WinHoldSeconds)
            return false;
        _ended = true;
        return true;
    }

    public void Reset()
    {
        _armedAt = -1f;
        _ended = false;
        _promoted = false;
    }
}
