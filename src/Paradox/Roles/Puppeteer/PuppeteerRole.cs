using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Puppeteer;

public static class PuppeteerRole
{
    private static readonly Dictionary<byte, ControlState> Controls = new();
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static bool _localFrozenByPuppeteer;

    public static bool IsPuppeteer(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Puppeteer;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool HasActiveControl(byte sourcePlayerId, float now) =>
        Controls.TryGetValue(sourcePlayerId, out var state) &&
        now < state.EndsAt;

    public static bool IsControlledTarget(byte targetPlayerId, float now)
    {
        foreach (var state in Controls.Values)
        {
            if (state.TargetPlayerId == targetPlayerId &&
                now < state.EndsAt)
                return true;
        }

        return false;
    }

    public static PlayerControl? FindClosestTarget(
        PlayerControl source,
        float maxDistance)
    {
        if (source == null || source.Data == null || source.Data.IsDead)
            return null;

        PlayerControl? closest = null;
        var bestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();
        var now = Time.time;

        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate == null ||
                candidate.PlayerId == source.PlayerId ||
                candidate.Data == null ||
                candidate.Data.IsDead ||
                candidate.Data.Disconnected ||
                IsControlledTarget(candidate.PlayerId, now))
                continue;

            var distance = Vector2.Distance(
                sourcePosition,
                candidate.GetTruePosition());

            if (distance > bestDistance)
                continue;

            closest = candidate;
            bestDistance = distance;
        }

        return closest;
    }

    public static bool TryControl(
        PlayerControl source,
        PlayerControl target)
    {
        if (source == null ||
            target == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        var now = Time.time;

        if (!IsPuppeteer(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            target.Data == null ||
            target.Data.IsDead ||
            target.Data.Disconnected ||
            source.PlayerId == target.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId) ||
            CooldownRemaining(source.PlayerId, now) > 0f ||
            HasActiveControl(source.PlayerId, now) ||
            IsControlledTarget(target.PlayerId, now))
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            target.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        var rawOffset =
            target.GetTruePosition() - source.GetTruePosition();

        var offset = rawOffset.sqrMagnitude > 0.001f
            ? rawOffset.normalized * ParadoxRoleSettings.PuppeteerFollowDistance
            : Vector2.down * ParadoxRoleSettings.PuppeteerFollowDistance;

        if (!RoleAbilityService.Use(source, RoleId.Puppeteer))
            return false;

        ApplySyncedControl(
            source.PlayerId,
            target.PlayerId,
            offset.x,
            offset.y,
            ParadoxRoleSettings.PuppeteerDurationSeconds,
            ParadoxRoleSettings.PuppeteerCooldownSeconds,
            true);

        BroadcastState(
            source.PlayerId,
            target.PlayerId,
            offset,
            ParadoxRoleSettings.PuppeteerDurationSeconds,
            ParadoxRoleSettings.PuppeteerCooldownSeconds,
            true);

        return true;
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        var now = Time.time;

        foreach (var pair in Controls.ToArray())
        {
            var sourceId = pair.Key;
            var state = pair.Value;
            var source = FindPlayer(sourceId);
            var target = FindPlayer(state.TargetPlayerId);

            var shouldEnd =
                now >= state.EndsAt ||
                MeetingHud.Instance != null ||
                source == null ||
                source.Data == null ||
                source.Data.IsDead ||
                source.Data.Disconnected ||
                target == null ||
                target.Data == null ||
                target.Data.IsDead ||
                target.Data.Disconnected;

            if (shouldEnd)
            {
                Controls.Remove(sourceId);
                BroadcastState(
                    sourceId,
                    state.TargetPlayerId,
                    state.Offset,
                    0f,
                    CooldownRemaining(sourceId, now),
                    false);
                continue;
            }

            if (now < state.NextSnapAt)
                continue;

            var desired =
                source.GetTruePosition() + state.Offset;

            target.NetTransform.SnapTo(desired);
            state.NextSnapAt = now + 0.10f;
        }
    }

    public static void UpdateLocalControl()
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null)
            return;

        var now = Time.time;
        var frozen = IsControlledTarget(local.PlayerId, now);

        if (frozen)
        {
            local.moveable = false;
            _localFrozenByPuppeteer = true;
            return;
        }

        if (!_localFrozenByPuppeteer)
            return;

        _localFrozenByPuppeteer = false;

        if (local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected &&
            MeetingHud.Instance == null)
        {
            local.moveable = true;
        }
    }

    public static void ApplySyncedControl(
        byte sourcePlayerId,
        byte targetPlayerId,
        float offsetX,
        float offsetY,
        float durationSeconds,
        float cooldownSeconds,
        bool active)
    {
        if (active)
        {
            Controls[sourcePlayerId] = new ControlState(
                targetPlayerId,
                new Vector2(offsetX, offsetY),
                Time.time + Math.Max(0f, durationSeconds),
                0f);

            CooldownEndsAt[sourcePlayerId] =
                Time.time + Math.Max(0f, cooldownSeconds);

            ShowFeedback(
                sourcePlayerId,
                targetPlayerId,
                durationSeconds);
            return;
        }

        Controls.Remove(sourcePlayerId);
    }

    private static void BroadcastState(
        byte sourcePlayerId,
        byte targetPlayerId,
        Vector2 offset,
        float durationSeconds,
        float cooldownSeconds,
        bool active)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<PuppeteerControlRpc>.Instance.Send(
            sender,
            new PuppeteerControlRpc.Data(
                sourcePlayerId,
                targetPlayerId,
                offset.x,
                offset.y,
                durationSeconds,
                cooldownSeconds,
                active ? (byte)1 : (byte)0),
            immediately: true);
    }

    private static void ShowFeedback(
        byte sourcePlayerId,
        byte targetPlayerId,
        float durationSeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null ||
                hud == null ||
                hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                var text = ParadoxPlugin.Localizer
                    .Get("role.Puppeteer.feedback.source")
                    .Replace(
                        "{seconds}",
                        Math.Ceiling(durationSeconds).ToString());

                hud.Notifier.AddDisconnectMessage(text);
            }
            else if (local.PlayerId == targetPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get(
                        "role.Puppeteer.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Puppeteer feedback failed: {e.Message}");
        }
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null && candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }

    public static void ResetRuntime()
    {
        Controls.Clear();
        CooldownEndsAt.Clear();

        var local = PlayerControl.LocalPlayer;
        if (_localFrozenByPuppeteer &&
            local != null &&
            local.Data != null &&
            !local.Data.IsDead &&
            !local.Data.Disconnected)
        {
            local.moveable = true;
        }

        _localFrozenByPuppeteer = false;
    }

    private sealed class ControlState
    {
        public byte TargetPlayerId { get; }
        public Vector2 Offset { get; }
        public float EndsAt { get; }
        public float NextSnapAt { get; set; }

        public ControlState(
            byte targetPlayerId,
            Vector2 offset,
            float endsAt,
            float nextSnapAt)
        {
            TargetPlayerId = targetPlayerId;
            Offset = offset;
            EndsAt = endsAt;
            NextSnapAt = nextSnapAt;
        }
    }
}
