using UnityEngine;

namespace Paradox.Roles.Witness;

public static class WitnessRole
{
    private static readonly Dictionary<byte, WitnessClue> LastClues = new();

    public static bool IsWitness(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Witness;

    public static bool TryCreateClue(PlayerControl witness, DeadBody body, out WitnessClue clue)
    {
        clue = null!;

        if (witness == null || body == null || !IsWitness(witness.PlayerId))
            return false;

        // Keep clues intentionally incomplete: Witness should help discussion,
        // not identify the killer directly.
        if (UnityEngine.Random.Range(0, 2) == 0)
        {
            clue = new WitnessClue(
                WitnessClueType.BodyAge,
                "role.Witness.clue.bodyAge",
                "recent");
        }
        else
        {
            clue = new WitnessClue(
                WitnessClueType.NearbyActivity,
                "role.Witness.clue.activity",
                "detected");
        }

        LastClues[witness.PlayerId] = clue;
        return true;
    }

    public static bool TryGetLastClue(byte playerId, out WitnessClue clue) =>
        LastClues.TryGetValue(playerId, out clue!);

    public static void Reset() => LastClues.Clear();
}
