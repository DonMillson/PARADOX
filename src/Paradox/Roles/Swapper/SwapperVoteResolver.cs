namespace Paradox.Roles.Swapper;

/// <summary>
/// Pure voting math; independent of Among Us/Unity so it can run in CI.
/// Swaps all votes cast FOR two candidates (not the two players' own ballots).
/// 253 is Skip, while 254/255 denote no valid candidate vote.
/// </summary>
public static class SwapperVoteResolver
{
    public const byte Skip = 253;

    public static byte MapVote(byte vote, byte a, byte b) =>
        vote == a ? b : vote == b ? a : vote;

    public static byte[] Swap(IReadOnlyList<byte> original, byte a, byte b)
    {
        if (a == b || a >= Skip || b >= Skip)
            throw new ArgumentException("Two distinct living player IDs are required.");

        var result = new byte[original.Count];
        for (var i = 0; i < original.Count; i++)
            result[i] = MapVote(original[i], a, b);
        return result;
    }

    // Null means tie, skip, or no valid exile.
    public static byte? FindExiled(IReadOnlyList<byte> votes)
    {
        var totals = new Dictionary<byte, int>();
        foreach (var vote in votes)
        {
            if (vote > Skip)
                continue;
            totals[vote] = totals.TryGetValue(vote, out var count) ? count + 1 : 1;
        }

        if (totals.Count == 0)
            return null;

        var best = -1;
        byte winner = Skip;
        var tie = false;
        foreach (var (player, count) in totals)
        {
            if (count > best)
            {
                best = count;
                winner = player;
                tie = false;
            }
            else if (count == best)
                tie = true;
        }

        return tie || winner == Skip ? null : winner;
    }

    /// <summary>
    /// A swap must actually change the person exiled. If an unrelated majority
    /// would exile the same Crewmate anyway, Swapper is not punished.
    /// </summary>
    public static bool ChangedExileTo(
        IReadOnlyList<byte> original, byte a, byte b, byte exiledId)
    {
        var before = FindExiled(original);
        var after = FindExiled(Swap(original, a, b));
        return after.HasValue && after.Value == exiledId &&
               (!before.HasValue || before.Value != exiledId);
    }
}
