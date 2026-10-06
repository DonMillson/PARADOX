using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Parasite;

public static class ParasiteRole
{
    private static readonly Dictionary<byte, ParasiteInfection> Infections = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static IReadOnlyCollection<ParasiteInfection> ActiveInfections => Infections.Values;

    public static bool IsParasite(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Parasite;

    public static bool TryInfect(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsParasite(source.PlayerId) || source.PlayerId == target.PlayerId)
            return false;

        var now = Time.time;
        if (CooldownEndsAt.TryGetValue(source.PlayerId, out var cooldownEnd) && now < cooldownEnd)
            return false;

        if (Infections.ContainsKey(target.PlayerId))
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Parasite))
            return false;

        Infections[target.PlayerId] = new ParasiteInfection(
            source.PlayerId,
            target.PlayerId,
            now,
            ParadoxRoleSettings.ParasiteInfectionDurationSeconds);

        CooldownEndsAt[source.PlayerId] = now + ParadoxRoleSettings.ParasiteCooldownSeconds;
        return true;
    }

    public static bool TryTakeCompleted(byte targetPlayerId, float now, out ParasiteInfection infection)
    {
        if (Infections.TryGetValue(targetPlayerId, out var found) && found.IsComplete(now))
        {
            infection = found;
            Infections.Remove(targetPlayerId);
            return true;
        }

        infection = null!;
        return false;
    }

    public static void Reset()
    {
        Infections.Clear();
        CooldownEndsAt.Clear();
    }
}
