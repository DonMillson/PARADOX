using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Doppelganger;

public static class DoppelgangerRole
{
    private static readonly Dictionary<byte, DoppelgangerState> States = new();

    public static bool IsDoppelganger(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Doppelganger;

    public static DoppelgangerState GetOrCreate(byte playerId)
    {
        if (!States.TryGetValue(playerId, out var state))
            States[playerId] = state = new DoppelgangerState(playerId);

        return state;
    }

    public static bool TryDisguise(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsDoppelganger(player.PlayerId))
            return false;

        var state = GetOrCreate(player.PlayerId);
        if (!state.TryStart(
                target.PlayerId,
                Time.time,
                ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds,
                ParadoxRoleSettings.DoppelgangerCooldownSeconds))
            return false;

        if (!RoleAbilityService.Use(player, RoleId.Doppelganger))
        {
            state.ClearDisguise();
            return false;
        }

        if (!DoppelgangerAppearance.Copy(player, target))
        {
            state.ClearDisguise();
            return false;
        }

        return true;
    }

    public static void Update(PlayerControl player)
    {
        if (player == null || !AmongUsClient.Instance.AmHost)
            return;

        if (!States.TryGetValue(player.PlayerId, out var state) ||
            !state.TargetPlayerId.HasValue ||
            state.IsDisguised(Time.time))
            return;

        DoppelgangerAppearance.Restore(player);
        state.ClearDisguise();
    }

    public static void Reset()
    {
        States.Clear();
        DoppelgangerAppearance.Reset();
    }
}
