using Paradox.Core;
using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Riftmaker;

public static class RiftmakerRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, Vector2> Anchors = new();

    public static bool IsRiftmaker(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) && role == RoleId.Riftmaker;

    public static bool HasAnchor(byte playerId) => Anchors.ContainsKey(playerId);

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool TryUse(PlayerControl source)
    {
        if (source == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsRiftmaker(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            ParadoxEventRuntime.RoleAbilitiesBlocked)
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f)
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Riftmaker))
            return false;

        if (!Anchors.TryGetValue(source.PlayerId, out var anchor))
        {
            anchor = source.GetTruePosition();
            ApplySyncedAction(
                source.PlayerId,
                0,
                anchor.x,
                anchor.y,
                ParadoxRoleSettings.RiftmakerAnchorDelaySeconds);

            BroadcastAccepted(
                source.PlayerId,
                0,
                anchor,
                ParadoxRoleSettings.RiftmakerAnchorDelaySeconds);
            return true;
        }

        ApplySyncedAction(
            source.PlayerId,
            1,
            anchor.x,
            anchor.y,
            ParadoxRoleSettings.RiftmakerCooldownSeconds);

        BroadcastAccepted(
            source.PlayerId,
            1,
            anchor,
            ParadoxRoleSettings.RiftmakerCooldownSeconds);
        return true;
    }

    public static void ApplySyncedAction(
        byte sourcePlayerId,
        byte action,
        float x,
        float y,
        float cooldownSeconds)
    {
        var position = new Vector2(x, y);
        CooldownEndsAt[sourcePlayerId] =
            Time.time + Math.Max(0f, cooldownSeconds);

        if (action == 0)
        {
            Anchors[sourcePlayerId] = position;
            ShowFeedback(sourcePlayerId, false);
            return;
        }

        Anchors.Remove(sourcePlayerId);

        var player = FindPlayer(sourcePlayerId);
        if (player != null && player.NetTransform != null)
            player.NetTransform.SnapTo(position);

        ShowFeedback(sourcePlayerId, true);
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

    private static void ShowFeedback(byte sourcePlayerId, bool warped)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || local.PlayerId != sourcePlayerId ||
                hud == null || hud.Notifier == null)
                return;

            hud.Notifier.AddDisconnectMessage(
                ParadoxPlugin.Localizer.Get(
                    warped
                        ? "role.Riftmaker.feedback.warp"
                        : "role.Riftmaker.feedback.anchor"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Riftmaker feedback failed: {e.Message}");
        }
    }

    private static void BroadcastAccepted(
        byte sourcePlayerId,
        byte action,
        Vector2 position,
        float cooldownSeconds)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<RiftmakerWarpRpc>.Instance.Send(
            sender,
            new RiftmakerWarpRpc.Data(
                sourcePlayerId,
                action,
                position.x,
                position.y,
                cooldownSeconds,
                1),
            immediately: true);
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        Anchors.Clear();
    }
}
