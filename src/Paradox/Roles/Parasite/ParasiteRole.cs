using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
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

        ApplySyncedInfection(
            source.PlayerId,
            target.PlayerId,
            now,
            ParadoxRoleSettings.ParasiteInfectionDurationSeconds);

        CooldownEndsAt[source.PlayerId] = now + ParadoxRoleSettings.ParasiteCooldownSeconds;

        var sender = PlayerControl.LocalPlayer;
        if (sender != null)
        {
            Rpc<ParasiteInfectionRpc>.Instance.Send(
                sender,
                new ParasiteInfectionRpc.Data(
                    source.PlayerId,
                    target.PlayerId,
                    ParadoxRoleSettings.ParasiteInfectionDurationSeconds),
                immediately: true);
        }

        return true;
    }

    public static void ApplySyncedInfection(
        byte sourcePlayerId,
        byte targetPlayerId,
        float startedAt,
        float durationSeconds)
    {
        Infections[targetPlayerId] = new ParasiteInfection(
            sourcePlayerId,
            targetPlayerId,
            startedAt,
            durationSeconds);
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
