using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Sheriff;

/// <summary>
/// Classic Crewmate deduction role. The host resolves the target's actual
/// faction and issues the native kill, not the requesting client.
/// </summary>
public static class SheriffRole
{
    private static readonly Dictionary<byte, float> Cooldowns = new();

    public static bool IsSheriff(byte id) =>
        PlayerRoleRegistry.TryGet(id, out var role) && role == RoleId.Sheriff;

    public static float Remaining(byte id, float now) =>
        Cooldowns.TryGetValue(id, out var until) ? Math.Max(0f, until - now) : 0f;

    public static PlayerControl? ClosestTarget(PlayerControl source)
    {
        if (source == null || source.Data == null ||
            source.Data.IsDead || source.Data.Disconnected)
            return null;

        var bestRange = source.MaxReportDistance;
        PlayerControl? closest = null;
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (!ValidTarget(source, candidate))
                continue;
            var distance = Vector2.Distance(source.GetTruePosition(), candidate.GetTruePosition());
            if (distance > bestRange)
                continue;
            bestRange = distance;
            closest = candidate;
        }
        return closest;
    }

    private static bool ValidTarget(PlayerControl shooter, PlayerControl? target) =>
        target != null && target.PlayerId != shooter.PlayerId &&
        target.Data != null && !target.Data.IsDead && !target.Data.Disconnected;

    public static bool TryShoot(PlayerControl shooter, PlayerControl target)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
            shooter == null || !IsSheriff(shooter.PlayerId) ||
            shooter.Data == null || shooter.Data.IsDead || shooter.Data.Disconnected ||
            !shooter.CanMove || !ValidTarget(shooter, target) ||
            MeetingHud.Instance != null ||
            Remaining(shooter.PlayerId, Time.time) > 0f)
            return false;

        if (Vector2.Distance(shooter.GetTruePosition(), target.GetTruePosition()) >
            shooter.MaxReportDistance)
            return false;

        // Validated by the common ability service: Corruptor, jam and assigned
        // role checks remain authoritative on the host.
        if (!RoleAbilityService.Use(shooter, RoleId.Sheriff))
            return false;

        var correct = target.Data.Role != null && target.Data.Role.IsImpostor;
        var victim = correct ? target : shooter;
        Cooldowns[shooter.PlayerId] =
            Time.time + ParadoxRoleSettings.SheriffCooldownSeconds;
        shooter.MurderPlayer(victim, MurderResultFlags.Succeeded);

        // Do not assume the murder succeeded: Guardian/Phantom can block it.
        ApplySyncedResult(shooter.PlayerId,
            ParadoxRoleSettings.SheriffCooldownSeconds, correct);
        if (PlayerControl.LocalPlayer != null)
            Rpc<SheriffShotRpc>.Instance.Send(PlayerControl.LocalPlayer,
                new SheriffShotRpc.Data(shooter.PlayerId, target.PlayerId,
                    ParadoxRoleSettings.SheriffCooldownSeconds,
                    correct ? (byte)1 : (byte)0, 1), immediately: true);
        return true;
    }

    public static void ApplySyncedResult(byte shooterId, float cooldown, bool correct)
    {
        Cooldowns[shooterId] = Time.time + Math.Max(0f, cooldown);
        try
        {
            if (PlayerControl.LocalPlayer?.PlayerId == shooterId &&
                HudManager.Instance?.Notifier != null)
                HudManager.Instance.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get(
                        correct ? "role.Sheriff.correct" : "role.Sheriff.wrong"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Sheriff result notification failed: {e.Message}");
        }
    }

    public static void ResetRuntime() => Cooldowns.Clear();
}
