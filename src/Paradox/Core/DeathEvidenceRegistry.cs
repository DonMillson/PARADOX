namespace Paradox.Core;

public static class DeathEvidenceRegistry
{
    private static readonly Dictionary<byte, DeathEvidence> Records = new();

    public static IReadOnlyCollection<DeathEvidence> All => Records.Values;

    // False means this death was already recorded (e.g. repeated network callbacks).
    public static bool Record(byte victimPlayerId, byte killerPlayerId, float timeOfDeath)
    {
        if (Records.ContainsKey(victimPlayerId))
            return false;

        Records[victimPlayerId] = new DeathEvidence(
            victimPlayerId,
            killerPlayerId,
            timeOfDeath);
        return true;
    }

    // A revived Revenant can die again; its previous death is no longer current evidence.
    public static bool Forget(byte playerId) => Records.Remove(playerId);

    public static bool TryGet(byte victimPlayerId, out DeathEvidence evidence) =>
        Records.TryGetValue(victimPlayerId, out evidence!);

    public static void Reset() => Records.Clear();
}
