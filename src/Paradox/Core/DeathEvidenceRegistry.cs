namespace Paradox.Core;

public static class DeathEvidenceRegistry
{
    private static readonly Dictionary<byte, DeathEvidence> Records = new();

    public static IReadOnlyCollection<DeathEvidence> All => Records.Values;

    public static void Record(byte victimPlayerId, byte killerPlayerId, float timeOfDeath)
    {
        if (Records.ContainsKey(victimPlayerId))
            return;

        Records[victimPlayerId] = new DeathEvidence(
            victimPlayerId,
            killerPlayerId,
            timeOfDeath);
    }

    public static bool TryGet(byte victimPlayerId, out DeathEvidence evidence) =>
        Records.TryGetValue(victimPlayerId, out evidence!);

    public static void Reset() => Records.Clear();
}
