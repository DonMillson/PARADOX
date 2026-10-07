using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Collector;

public static class CollectorRole
{
    public const int CustomWinReason = 123;
    public const byte NoWinner = byte.MaxValue;
    private static readonly Dictionary<byte, HashSet<byte>> Samples = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsCollector(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Collector;

    public static int GetSampleCount(byte playerId) =>
        Samples.TryGetValue(playerId, out var samples) ? samples.Count : 0;

    public static bool HasSample(byte collectorId, byte targetId) =>
        Samples.TryGetValue(collectorId, out var samples) && samples.Contains(targetId);

    public static float CooldownRemaining(byte playerId, float now) =>
        CooldownEndsAt.TryGetValue(playerId, out var end) ? Math.Max(0f, end - now) : 0f;

    public static bool TryCollect(PlayerControl source, PlayerControl target)
    {
        if (source == null || target == null || AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost || _endSent)
            return false;
        if (!IsCollector(source.PlayerId) || source.Data == null || source.Data.IsDead ||
            source.Data.Disconnected || target.Data == null || target.Data.IsDead ||
            target.Data.Disconnected || source.PlayerId == target.PlayerId ||
            HasSample(source.PlayerId, target.PlayerId) ||
            ParadoxEventRuntime.RoleAbilitiesBlocked || CorruptorRole.IsCorrupted(source.PlayerId))
            return false;
        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;
        if (Vector2.Distance(source.GetTruePosition(), target.GetTruePosition()) > source.MaxReportDistance)
            return false;
        if (!RoleAbilityService.Use(source, RoleId.Collector))
            return false;

        var set = Samples.TryGetValue(source.PlayerId, out var existing)
            ? existing : (Samples[source.PlayerId] = new HashSet<byte>());
        if (!set.Add(target.PlayerId))
            return false;

        CooldownEndsAt[source.PlayerId] = now + ParadoxRoleSettings.CollectorCooldownSeconds;
        ApplySyncedState(source.PlayerId, target.PlayerId, set.Count, ParadoxRoleSettings.CollectorCooldownSeconds);
        BroadcastAccepted(source.PlayerId, target.PlayerId, set.Count);

        if (set.Count >= ParadoxRoleSettings.CollectorRequiredSamples)
        {
            WinnerPlayerId = source.PlayerId;
            _endSent = true;
            GameManager.Instance.RpcEndGame((GameOverReason)CustomWinReason, false);
        }
        return true;
    }

    public static void ApplySyncedState(byte collectorId, byte targetId, int count, float cooldownSeconds)
    {
        var set = Samples.TryGetValue(collectorId, out var existing)
            ? existing : (Samples[collectorId] = new HashSet<byte>());
        set.Add(targetId);
        CooldownEndsAt[collectorId] = Time.time + Math.Max(0f, cooldownSeconds);
        ShowFeedback(collectorId, count);
    }

    private static void BroadcastAccepted(byte collectorId, byte targetId, int count)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null) return;
        Rpc<CollectorSampleRpc>.Instance.Send(sender,
            new CollectorSampleRpc.Data(collectorId, targetId, count,
                ParadoxRoleSettings.CollectorCooldownSeconds, 1), immediately: true);
    }

    private static void ShowFeedback(byte collectorId, int count)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != collectorId || hud?.Notifier == null) return;
            hud.Notifier.AddDisconnectMessage(ParadoxPlugin.Localizer.Get("role.Collector.feedback")
                .Replace("{count}", count.ToString())
                .Replace("{required}", ParadoxRoleSettings.CollectorRequiredSamples.ToString()));
        }
        catch (Exception e) { ParadoxPlugin.Instance.Log.LogWarning($"Collector feedback failed: {e.Message}"); }
    }

    public static void ResetRuntime()
    {
        Samples.Clear();
        CooldownEndsAt.Clear();
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }
}
