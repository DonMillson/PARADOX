namespace Paradox.Core;

public sealed record DeathEvidence(
    byte VictimPlayerId,
    byte KillerPlayerId,
    float TimeOfDeath);
