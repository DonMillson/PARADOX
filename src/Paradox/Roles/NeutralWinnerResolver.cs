namespace Paradox.Roles;

/// <summary>
/// Resolve the sole neutral winner for a custom game-over reason on every client.
/// The host knows its actual winner ID; clients can recover it from the synchronized
/// unique neutral role assignment, rather than showing an empty winning team.
/// If more than one player of this role exists, fail closed instead of awarding
/// the custom win to an arbitrary player.
/// </summary>
public static class NeutralWinnerResolver
{
    public static byte Resolve(RoleId role, byte hostWinnerId)
    {
        if (hostWinnerId != byte.MaxValue)
            return hostWinnerId;

        var found = byte.MaxValue;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null ||
                player.Data == null ||
                !PlayerRoleRegistry.TryGet(player.PlayerId, out var assigned) ||
                assigned != role)
                continue;

            if (found != byte.MaxValue)
                return byte.MaxValue;

            found = player.PlayerId;
        }

        return found;
    }
}
