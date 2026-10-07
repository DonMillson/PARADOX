using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Saboteur;

public static class SaboteurRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly HashSet<byte> ArmedPlayers = new();

    public static bool IsSaboteur(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Saboteur;

    public static bool IsArmed(byte playerId) =>
        ArmedPlayers.Contains(playerId);

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryArm(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsSaboteur(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            !source.CanMove ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId))
            return false;

        var now = Time.time;

        if (CooldownRemaining(source.PlayerId, now) > 0f ||
            IsArmed(source.PlayerId))
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Saboteur))
            return false;

        ApplySyncedState(
            source.PlayerId,
            true,
            ParadoxRoleSettings.SaboteurCooldownSeconds,
            false);

        BroadcastState(
            source.PlayerId,
            true,
            ParadoxRoleSettings.SaboteurCooldownSeconds,
            1,
            1);

        return true;
    }

    public static float TryConsumeOverloadForSabotage(byte sourcePlayerId)
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            !ArmedPlayers.Remove(sourcePlayerId))
            return 0f;

        ApplySyncedState(
            sourcePlayerId,
            false,
            0f,
            true);

        BroadcastState(
            sourcePlayerId,
            false,
            0f,
            2,
            1);

        return Math.Max(0f, ParadoxRoleSettings.SaboteurBonusMeter);
    }

    public static void ApplySyncedState(
        byte sourcePlayerId,
        bool armed,
        float cooldownSeconds,
        bool fired)
    {
        if (armed)
            ArmedPlayers.Add(sourcePlayerId);
        else
            ArmedPlayers.Remove(sourcePlayerId);

        if (cooldownSeconds > 0f)
        {
            CooldownEndsAt[sourcePlayerId] =
                Time.time + cooldownSeconds;
        }

        if (fired)
            ShowFiredFeedback(sourcePlayerId);
        else if (armed)
            ShowArmedFeedback(sourcePlayerId);
    }

    private static void BroadcastState(
        byte sourcePlayerId,
        bool armed,
        float cooldownSeconds,
        byte action,
        byte accepted)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<SaboteurOverloadRpc>.Instance.Send(
            sender,
            new SaboteurOverloadRpc.Data(
                sourcePlayerId,
                armed ? (byte)1 : (byte)0,
                cooldownSeconds,
                action,
                accepted),
            immediately: true);
    }

    private static void ShowArmedFeedback(byte sourcePlayerId)
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
                .Get("role.Saboteur.feedback.arm")
                .Replace(
                    "{amount}",
                    ParadoxRoleSettings.SaboteurBonusMeter.ToString("0.#"));

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Saboteur arm feedback failed: {e.Message}");
        }
    }

    private static void ShowFiredFeedback(byte sourcePlayerId)
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

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    "role.Saboteur.feedback.fire"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Saboteur fire feedback failed: {e.Message}");
        }
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        ArmedPlayers.Clear();
    }
}
