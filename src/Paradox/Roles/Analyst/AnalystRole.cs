using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Analyst;

public static class AnalystRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsAnalyst(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Analyst;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryAnalyze(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsAnalyst(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        ObserverState.RemoveExpired(now);

        var alive = 0;
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player?.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected)
                continue;

            alive++;
        }

        var deaths = DeathEvidenceRegistry.All.Count;
        var traces = ObserverState.Active.Count;
        var meter = ParadoxGame.State.Meter;

        if (!RoleAbilityService.Use(source, RoleId.Analyst))
            return false;

        ApplySyncedResult(
            source.PlayerId,
            ParadoxRoleSettings.AnalystCooldownSeconds,
            alive,
            deaths,
            traces,
            meter);

        BroadcastAccepted(source.PlayerId, alive, deaths, traces, meter);
        return true;
    }

    public static void ApplySyncedResult(
        byte sourcePlayerId,
        float cooldownSeconds,
        int alive,
        int deaths,
        int traces,
        float meter)
    {
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(sourcePlayerId, alive, deaths, traces, meter);
    }

    private static void ShowFeedback(
        byte sourcePlayerId,
        int alive,
        int deaths,
        int traces,
        float meter)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer.Get("role.Analyst.result")
                .Replace("{alive}", alive.ToString())
                .Replace("{deaths}", deaths.ToString())
                .Replace("{traces}", traces.ToString())
                .Replace("{meter}", meter.ToString("0.#"));

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Analyst feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        int alive,
        int deaths,
        int traces,
        float meter)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<AnalystScanRpc>.Instance.Send(
            sender,
            new AnalystScanRpc.Data(
                sourcePlayerId,
                ParadoxRoleSettings.AnalystCooldownSeconds,
                alive,
                deaths,
                traces,
                meter,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
