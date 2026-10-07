using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Roles.Saboteur;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Technician;

public static class TechnicianRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    private static float _shieldEndsAt;
    private static byte _shieldOwner = byte.MaxValue;

    private static float _lastSabotageAt;
    private static byte _lastSabotageSource = byte.MaxValue;
    private static byte _lastSabotageAmount = byte.MaxValue;

    public static bool IsTechnician(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Technician;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static float ShieldRemaining(float now) =>
        Math.Max(0f, _shieldEndsAt - now);

    public static bool IsShieldActive =>
        ShieldRemaining(Time.time) > 0f;

    public static bool TryActivateShield(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsTechnician(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            !source.CanMove ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId))
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f ||
            IsShieldActive)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Technician))
            return false;

        ApplySyncedShield(
            source.PlayerId,
            ParadoxRoleSettings.TechnicianShieldDurationSeconds,
            ParadoxRoleSettings.TechnicianCooldownSeconds);

        BroadcastState(
            source.PlayerId,
            ParadoxRoleSettings.TechnicianShieldDurationSeconds,
            ParadoxRoleSettings.TechnicianCooldownSeconds,
            1,
            1);

        return true;
    }

    public static void ApplySyncedShield(
        byte sourcePlayerId,
        float durationSeconds,
        float cooldownSeconds)
    {
        _shieldOwner = sourcePlayerId;
        _shieldEndsAt =
            Time.time + Math.Max(0f, durationSeconds);

        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        ShowActivationFeedback(sourcePlayerId, durationSeconds);
    }

    public static void ApplySyncedSabotageBlocked(byte shieldOwner)
    {
        ShowBlockedFeedback(shieldOwner);
    }

    public static void HandleSabotageMeter(
        PlayerControl? source,
        byte amount)
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        var now = Time.time;
        var sourceId = source?.PlayerId ?? byte.MaxValue;

        if (sourceId == _lastSabotageSource &&
            amount == _lastSabotageAmount &&
            now - _lastSabotageAt < 1f)
            return;

        _lastSabotageAt = now;
        _lastSabotageSource = sourceId;
        _lastSabotageAmount = amount;

        var overloadBonus =
            SaboteurRole.TryConsumeOverloadForSabotage(sourceId);

        if (IsShieldActive)
        {
            ParadoxPlugin.Instance.Log.LogInfo(
                $"Technician shield absorbed sabotage instability (source {sourceId}, amount {amount}, overload {overloadBonus:0.#}).");

            BroadcastState(
                _shieldOwner,
                ShieldRemaining(now),
                0f,
                2,
                1);

            ShowBlockedFeedback(_shieldOwner);
            return;
        }

        ParadoxGame.AddFrom(ParadoxMeterSource.Sabotage);

        if (overloadBonus > 0f)
            ParadoxGame.AddMeter(overloadBonus);
    }

    private static void BroadcastState(
        byte sourcePlayerId,
        float durationSeconds,
        float cooldownSeconds,
        byte action,
        byte accepted)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<TechnicianShieldRpc>.Instance.Send(
            sender,
            new TechnicianShieldRpc.Data(
                sourcePlayerId,
                durationSeconds,
                cooldownSeconds,
                action,
                accepted),
            immediately: true);
    }

    private static void ShowActivationFeedback(
        byte sourcePlayerId,
        float durationSeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                local.PlayerId != sourcePlayerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer
                .Get("role.Technician.feedback")
                .Replace(
                    "{seconds}",
                    Math.Ceiling(durationSeconds).ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Technician activation feedback failed: {e.Message}");
        }
    }

    private static void ShowBlockedFeedback(byte shieldOwner)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                local.PlayerId != shieldOwner ||
                hud == null ||
                hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    "role.Technician.blockedSabotage"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Technician sabotage feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        _shieldEndsAt = 0f;
        _shieldOwner = byte.MaxValue;
        _lastSabotageAt = 0f;
        _lastSabotageSource = byte.MaxValue;
        _lastSabotageAmount = byte.MaxValue;
    }
}
