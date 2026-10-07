using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Forgotten;

public static class ForgottenRole
{
    public const int CustomWinReason = 124;
    public const byte NoWinner = byte.MaxValue;

    private static readonly Dictionary<byte, HashSet<byte>> Memories = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsForgotten(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Forgotten;

    public static int GetMemoryCount(byte playerId) =>
        Memories.TryGetValue(playerId, out var memories)
            ? memories.Count
            : 0;

    public static bool HasMemory(byte playerId, byte bodyPlayerId) =>
        Memories.TryGetValue(playerId, out var memories) &&
        memories.Contains(bodyPlayerId);

    public static float CooldownRemaining(byte playerId, float now) =>
        CooldownEndsAt.TryGetValue(playerId, out var end)
            ? Math.Max(0f, end - now)
            : 0f;

    public static DeadBody? FindClosestBody(
        PlayerControl source,
        float maxDistance)
    {
        if (source == null ||
            source.Data == null ||
            source.Data.IsDead)
            return null;

        DeadBody? closest = null;
        var bestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();

        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null ||
                body.Reported ||
                HasMemory(source.PlayerId, body.ParentId))
                continue;

            var distance = Vector2.Distance(
                sourcePosition,
                body.TruePosition);

            if (distance > bestDistance)
                continue;

            closest = body;
            bestDistance = distance;
        }

        return closest;
    }

    public static bool TryRemember(
        PlayerControl source,
        DeadBody body)
    {
        if (source == null ||
            body == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return false;

        if (!IsForgotten(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            body.Reported ||
            HasMemory(source.PlayerId, body.ParentId) ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId))
            return false;

        var now = Time.time;

        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            body.TruePosition);

        if (distance > source.MaxReportDistance + 0.15f)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Forgotten))
            return false;

        var set = Memories.TryGetValue(source.PlayerId, out var existing)
            ? existing
            : (Memories[source.PlayerId] = new HashSet<byte>());

        if (!set.Add(body.ParentId))
            return false;

        CooldownEndsAt[source.PlayerId] =
            now + ParadoxRoleSettings.ForgottenCooldownSeconds;

        ApplySyncedState(
            source.PlayerId,
            body.ParentId,
            set.Count,
            ParadoxRoleSettings.ForgottenCooldownSeconds);

        BroadcastAccepted(
            source.PlayerId,
            body.ParentId,
            set.Count);

        if (set.Count >= ParadoxRoleSettings.ForgottenRequiredMemories)
        {
            WinnerPlayerId = source.PlayerId;
            _endSent = true;

            GameManager.Instance.RpcEndGame(
                (GameOverReason)CustomWinReason,
                false);
        }

        return true;
    }

    public static void ApplySyncedState(
        byte forgottenPlayerId,
        byte bodyPlayerId,
        int memoryCount,
        float cooldownSeconds)
    {
        var set = Memories.TryGetValue(
            forgottenPlayerId,
            out var existing)
            ? existing
            : (Memories[forgottenPlayerId] = new HashSet<byte>());

        set.Add(bodyPlayerId);

        CooldownEndsAt[forgottenPlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowFeedback(
            forgottenPlayerId,
            memoryCount);
    }

    private static void BroadcastAccepted(
        byte forgottenPlayerId,
        byte bodyPlayerId,
        int memoryCount)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<ForgottenMemoryRpc>.Instance.Send(
            sender,
            new ForgottenMemoryRpc.Data(
                forgottenPlayerId,
                bodyPlayerId,
                memoryCount,
                ParadoxRoleSettings.ForgottenCooldownSeconds,
                1),
            immediately: true);
    }

    private static void ShowFeedback(
        byte forgottenPlayerId,
        int memoryCount)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                local.PlayerId != forgottenPlayerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer
                .Get("role.Forgotten.feedback")
                .Replace("{count}", memoryCount.ToString())
                .Replace(
                    "{required}",
                    ParadoxRoleSettings.ForgottenRequiredMemories.ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Forgotten feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime()
    {
        Memories.Clear();
        CooldownEndsAt.Clear();
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }
}
