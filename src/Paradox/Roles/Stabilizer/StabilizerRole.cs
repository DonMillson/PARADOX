using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Observer;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Stabilizer;

public static class StabilizerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();

    public static bool IsStabilizer(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Stabilizer;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var cooldownEnd))
            return 0f;

        return Math.Max(0f, cooldownEnd - now);
    }

    public static bool TryStabilize(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsStabilizer(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            ParadoxGame.State.Meter <= 0f)
            return false;

        var now = Time.time;
        if (CooldownEndsAt.TryGetValue(source.PlayerId, out var cooldownEnd) &&
            now < cooldownEnd)
            return false;

        var reduced = ParadoxGame.ReduceMeter(
            ParadoxRoleSettings.StabilizerReductionAmount);

        if (reduced <= 0f)
            return false;

        ApplySyncedCooldown(
            source.PlayerId,
            ParadoxRoleSettings.StabilizerCooldownSeconds);

        ObserverNetwork.BroadcastTrace(source);
        ShowFeedback(source.PlayerId, reduced);
        BroadcastAccepted(source.PlayerId, reduced);
        return true;
    }

    public static void ApplySyncedCooldown(byte playerId, float cooldownSeconds)
    {
        CooldownEndsAt[playerId] =
            Time.time + Math.Max(0f, cooldownSeconds);
    }

    public static void ShowFeedback(byte sourcePlayerId, float amount)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer.Get("role.Stabilizer.feedback")
                .Replace("{amount}", amount.ToString("0.#"));

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Stabilizer feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(byte sourcePlayerId, float reduced)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<StabilizerPulseRpc>.Instance.Send(
            sender,
            new StabilizerPulseRpc.Data(
                sourcePlayerId,
                ParadoxRoleSettings.StabilizerCooldownSeconds,
                reduced,
                1),
            immediately: true);
    }

    public static void ResetRuntime() => CooldownEndsAt.Clear();
}
