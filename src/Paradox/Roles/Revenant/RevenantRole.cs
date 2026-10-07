using Paradox.Networking;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Revenant;

public static class RevenantRole
{
    public const int CustomWinReason = 125;
    public const byte NoWinner = byte.MaxValue;

    private static readonly Dictionary<byte, ReturnState> PendingReturns = new();
    private static readonly Dictionary<byte, float> SurvivalWinAt = new();
    private static readonly HashSet<byte> Returned = new();

    public static byte WinnerPlayerId { get; private set; } = NoWinner;
    private static bool _endSent;

    public static bool IsRevenant(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Revenant;

    public static bool HasReturned(byte playerId) =>
        Returned.Contains(playerId);

    public static void OnKilled(PlayerControl target)
    {
        if (target == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            !IsRevenant(target.PlayerId) ||
            Returned.Contains(target.PlayerId) ||
            PendingReturns.ContainsKey(target.PlayerId))
            return;

        var position = FindBodyPosition(
            target.PlayerId,
            target.GetTruePosition());

        PendingReturns[target.PlayerId] = new ReturnState(
            Time.time + ParadoxRoleSettings.RevenantReviveDelaySeconds,
            position);

        ApplyPendingState(
            target.PlayerId,
            position.x,
            position.y,
            ParadoxRoleSettings.RevenantReviveDelaySeconds);

        BroadcastState(
            target.PlayerId,
            0,
            position,
            ParadoxRoleSettings.RevenantReviveDelaySeconds);
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            _endSent)
            return;

        var now = Time.time;

        if (MeetingHud.Instance == null)
        {
            foreach (var pair in PendingReturns.ToArray())
            {
                if (now < pair.Value.ReturnAt)
                    continue;

                var player = FindPlayer(pair.Key);
                if (player == null ||
                    player.Data == null ||
                    player.Data.Disconnected)
                {
                    PendingReturns.Remove(pair.Key);
                    continue;
                }

                RevivePlayer(
                    player,
                    pair.Value.Position);

                PendingReturns.Remove(pair.Key);
                Returned.Add(pair.Key);

                SurvivalWinAt[pair.Key] =
                    now + ParadoxRoleSettings.RevenantSurvivalSeconds;

                BroadcastState(
                    pair.Key,
                    1,
                    pair.Value.Position,
                    ParadoxRoleSettings.RevenantSurvivalSeconds);
            }
        }

        foreach (var pair in SurvivalWinAt.ToArray())
        {
            if (now < pair.Value)
                continue;

            var player = FindPlayer(pair.Key);
            if (player == null ||
                player.Data == null ||
                player.Data.IsDead ||
                player.Data.Disconnected)
            {
                SurvivalWinAt.Remove(pair.Key);
                continue;
            }

            WinnerPlayerId = pair.Key;
            _endSent = true;

            GameManager.Instance.RpcEndGame(
                (GameOverReason)CustomWinReason,
                false);

            return;
        }
    }

    public static void ApplyPendingState(
        byte playerId,
        float x,
        float y,
        float delaySeconds)
    {
        PendingReturns[playerId] = new ReturnState(
            Time.time + Math.Max(0f, delaySeconds),
            new Vector2(x, y));

        ShowReturningFeedback(
            playerId,
            delaySeconds);
    }

    public static void ApplyReturnedState(
        byte playerId,
        float x,
        float y,
        float survivalSeconds)
    {
        var player = FindPlayer(playerId);
        if (player != null)
        {
            RevivePlayer(
                player,
                new Vector2(x, y));
        }

        PendingReturns.Remove(playerId);
        Returned.Add(playerId);

        SurvivalWinAt[playerId] =
            Time.time + Math.Max(0f, survivalSeconds);

        ShowReturnedFeedback(
            playerId,
            survivalSeconds);
    }

    private static void RevivePlayer(
        PlayerControl player,
        Vector2 position)
    {
        if (player == null)
            return;

        player.Revive();

        if (player.NetTransform != null)
            player.NetTransform.SnapTo(position);

        RemoveBody(player.PlayerId);
    }

    private static Vector2 FindBodyPosition(
        byte playerId,
        Vector2 fallback)
    {
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body != null &&
                body.ParentId == playerId)
                return body.TruePosition;
        }

        return fallback;
    }

    private static void RemoveBody(byte playerId)
    {
        foreach (var body in UnityEngine.Object.FindObjectsOfType<DeadBody>())
        {
            if (body == null ||
                body.ParentId != playerId)
                continue;

            body.gameObject.SetActive(false);
            UnityEngine.Object.Destroy(body.gameObject);
        }
    }

    private static void BroadcastState(
        byte playerId,
        byte action,
        Vector2 position,
        float seconds)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<RevenantReturnRpc>.Instance.Send(
            sender,
            new RevenantReturnRpc.Data(
                playerId,
                action,
                position.x,
                position.y,
                seconds,
                1),
            immediately: true);
    }

    private static void ShowReturningFeedback(
        byte playerId,
        float delaySeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                local.PlayerId != playerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer
                .Get("role.Revenant.returning")
                .Replace(
                    "{seconds}",
                    Math.Ceiling(delaySeconds).ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Revenant returning feedback failed: {e.Message}");
        }
    }

    private static void ShowReturnedFeedback(
        byte playerId,
        float survivalSeconds)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;

            if (local == null ||
                local.PlayerId != playerId ||
                hud == null ||
                hud.Notifier == null)
                return;

            var text = ParadoxPlugin.Localizer
                .Get("role.Revenant.returned")
                .Replace(
                    "{seconds}",
                    Math.Ceiling(survivalSeconds).ToString());

            hud.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Revenant return feedback failed: {e.Message}");
        }
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player != null &&
                player.PlayerId == playerId)
                return player;
        }

        return null;
    }

    public static void ResetRuntime()
    {
        PendingReturns.Clear();
        SurvivalWinAt.Clear();
        Returned.Clear();
        WinnerPlayerId = NoWinner;
        _endSent = false;
    }

    private sealed record ReturnState(
        float ReturnAt,
        Vector2 Position);
}
