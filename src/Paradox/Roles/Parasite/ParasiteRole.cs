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

        if (!IsParasite(source.PlayerId) ||
            source.PlayerId == target.PlayerId ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            target.Data == null || target.Data.IsDead || target.Data.Disconnected)
            return false;

        var now = Time.time;
        if (CooldownEndsAt.TryGetValue(source.PlayerId, out var cooldownEnd) && now < cooldownEnd)
            return false;

        if (Infections.ContainsKey(target.PlayerId))
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), target.GetTruePosition());
        if (distance > source.MaxReportDistance)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Parasite))
            return false;

        ApplySyncedInfection(
            source.PlayerId,
            target.PlayerId,
            now,
            ParadoxRoleSettings.ParasiteInfectionDurationSeconds,
            ParadoxRoleSettings.ParasiteCooldownSeconds);

        var sender = PlayerControl.LocalPlayer;
        if (sender != null)
        {
            Rpc<ParasiteInfectionRpc>.Instance.Send(
                sender,
                new ParasiteInfectionRpc.Data(
                    source.PlayerId,
                    target.PlayerId,
                    ParadoxRoleSettings.ParasiteInfectionDurationSeconds,
                    ParadoxRoleSettings.ParasiteCooldownSeconds),
                immediately: true);
        }

        return true;
    }

    public static void ApplySyncedInfection(
        byte sourcePlayerId,
        byte targetPlayerId,
        float startedAt,
        float durationSeconds,
        float cooldownSeconds)
    {
        Infections[targetPlayerId] = new ParasiteInfection(
            sourcePlayerId,
            targetPlayerId,
            startedAt,
            durationSeconds);

        CooldownEndsAt[sourcePlayerId] = startedAt + Math.Max(0f, cooldownSeconds);
        TryShowInfectionFeedback(sourcePlayerId, targetPlayerId, durationSeconds);
    }

    public static bool IsInfected(byte playerId) =>
        Infections.ContainsKey(playerId);

    public static bool RemoveSyncedInfection(byte playerId) =>
        Infections.Remove(playerId);

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var cooldownEnd))
            return 0f;

        return Math.Max(0f, cooldownEnd - now);
    }

    private static void TryShowInfectionFeedback(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || hud == null || hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                var text = ParadoxPlugin.Localizer.Get("role.Parasite.feedback.source")
                    .Replace("{seconds}", Math.Ceiling(durationSeconds).ToString());
                hud.Notifier.AddDisconnectMessage(text);
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Parasite.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Parasite infection feedback failed: {e.Message}");
        }
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
