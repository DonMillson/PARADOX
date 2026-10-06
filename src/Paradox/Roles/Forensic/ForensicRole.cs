using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Forensic;

public static class ForensicRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsForensic(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Forensic;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static DeadBody? FindClosestBody(PlayerControl source, float maxDistance)
    {
        if (source == null || source.Data == null || source.Data.IsDead)
            return null;

        DeadBody? closest = null;
        var closestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();

        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null || body.Reported)
                continue;

            var distance = Vector2.Distance(sourcePosition, body.TruePosition);
            if (distance > closestDistance)
                continue;

            closest = body;
            closestDistance = distance;
        }

        return closest;
    }

    public static bool TryExamine(PlayerControl source, DeadBody body)
    {
        if (source == null || body == null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsForensic(source.PlayerId) ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            body.Reported ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(source.GetTruePosition(), body.TruePosition);
        if (distance > source.MaxReportDistance + 0.15f)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Forensic))
            return false;

        var known = DeathEvidenceRegistry.TryGet(body.ParentId, out var evidence);
        var ageSeconds = known
            ? Math.Max(0f, now - evidence.TimeOfDeath)
            : 0f;

        var residue = false;
        if (known)
        {
            ObserverState.RemoveExpired(now);
            residue = ObserverState.Active.Any(trace =>
                trace.SourcePlayerId == evidence.KillerPlayerId &&
                Math.Abs(trace.CreatedAt - evidence.TimeOfDeath) <= ObserverRole.TraceLifetimeSeconds);
        }

        ApplySyncedResult(
            source.PlayerId,
            ParadoxRoleSettings.ForensicCooldownSeconds,
            known,
            ageSeconds,
            residue);

        BroadcastAccepted(
            source.PlayerId,
            body.ParentId,
            known,
            ageSeconds,
            residue);

        return true;
    }

    public static void ApplySyncedResult(
        byte sourcePlayerId,
        float cooldownSeconds,
        bool known,
        float ageSeconds,
        bool residue)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(sourcePlayerId, known, ageSeconds, residue);
    }

    private static void ShowFeedback(
        byte sourcePlayerId,
        bool known,
        float ageSeconds,
        bool residue)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            if (!known)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Forensic.result.unknown"));
                return;
            }

            var residueText = ParadoxPlugin.Localizer.Get(
                residue ? "role.Forensic.residue.yes" : "role.Forensic.residue.no");

            var text = ParadoxPlugin.Localizer.Get("role.Forensic.result")
                .Replace("{seconds}", Math.Floor(ageSeconds).ToString())
                .Replace("{residue}", residueText);

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Forensic feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte bodyPlayerId,
        bool known,
        float ageSeconds,
        bool residue)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<ForensicExamineRpc>.Instance.Send(
            sender,
            new ForensicExamineRpc.Data(
                sourcePlayerId,
                bodyPlayerId,
                ParadoxRoleSettings.ForensicCooldownSeconds,
                known ? (byte)1 : (byte)0,
                ageSeconds,
                residue ? (byte)1 : (byte)0,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
