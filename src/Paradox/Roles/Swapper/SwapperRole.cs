using Paradox.Networking;
using Paradox.Roles;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Swapper;

public static class SwapperRole
{
    private static byte? _swapper;
    private static byte _first;
    private static byte _second;
    private static bool _applied;
    private static byte[]? _votesBeforeSwap;
    private static byte? _pendingPunishment;

    public static bool IsSwapper(byte id) =>
        PlayerRoleRegistry.TryGet(id, out var role) && role == RoleId.Swapper;

    public static void OnMeetingStart()
    {
        _swapper = null;
        _votesBeforeSwap = null;
        _applied = false;
    }

    public static bool TrySetSwap(PlayerControl player, byte a, byte b)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
            MeetingHud.Instance == null || _applied || _swapper.HasValue ||
            player == null || !IsSwapper(player.PlayerId) ||
            player.Data == null || player.Data.IsDead || player.Data.Disconnected ||
            a == b || a == player.PlayerId || b == player.PlayerId ||
            a >= SwapperVoteResolver.Skip || b >= SwapperVoteResolver.Skip)
            return false;

        var first = FindPlayer(a);
        var second = FindPlayer(b);
        if (first == null || second == null ||
            first.Data == null || first.Data.IsDead || first.Data.Disconnected ||
            second.Data == null || second.Data.IsDead || second.Data.Disconnected)
            return false;

        _swapper = player.PlayerId;
        _first = a;
        _second = b;
        NotifyOwnSwapper(player.PlayerId, a, b);
        // Ack only comes from the host and does not transmit authority back
        // to clients; actual ballots are always changed on the host.
        if (PlayerControl.LocalPlayer != null)
            Rpc<SwapperMeetingRpc>.Instance.Send(
                PlayerControl.LocalPlayer,
                new SwapperMeetingRpc.Data(1, player.PlayerId, a, b),
                immediately: true);
        return true;
    }

    public static void ApplySwapBeforeCount(MeetingHud meeting)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
            !_swapper.HasValue || _applied || meeting == null)
            return;

        var owner = FindPlayer(_swapper.Value);
        if (owner == null || owner.Data == null ||
            owner.Data.IsDead || owner.Data.Disconnected ||
            !IsSwapper(owner.PlayerId) ||
            FindLiving(_first) == null || FindLiving(_second) == null)
            return;

        // Never change a partial vote count. Vanilla CheckForEndVoting can be
        // called repeatedly as ballots arrive, so only map after everybody
        // alive has voted, exactly once per meeting.
        var before = new List<byte>();
        foreach (var area in meeting.playerStates)
        {
            if (area == null)
                continue;
            if (!area.AmDead && !area.DidVote)
                return;
            before.Add(!area.AmDead && area.DidVote ? area.VotedForId : byte.MaxValue);
        }

        _votesBeforeSwap = before.ToArray();
        foreach (var area in meeting.playerStates)
        {
            if (area != null && !area.AmDead && area.DidVote)
                area.VotedForId = SwapperVoteResolver.MapVote(
                    area.VotedForId, _first, _second);
        }
        _applied = true;
        ParadoxPlugin.Instance.Log.LogInfo(
            $"Swapper {_swapper.Value} exchanged ballots for {_first}/{_second}.");
    }

    public static void OnVotingComplete(NetworkedPlayerInfo? exiled)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
            !_applied || !_swapper.HasValue || _votesBeforeSwap == null ||
            exiled == null ||
            !SwapperVoteResolver.ChangedExileTo(
                _votesBeforeSwap, _first, _second, exiled.PlayerId))
            return;

        // Only a real crewmate (not an impostor or a custom neutral)
        // activates the user's punishment rule.
        if (exiled.Role == null || exiled.Role.IsImpostor ||
            PlayerRoleRegistry.TryGet(exiled.PlayerId, out var assigned) &&
            RoleRegistry.GetFaction(assigned) != RoleFaction.Crewmate)
            return;

        _pendingPunishment = _swapper.Value;
    }

    public static void OnMeetingClose()
    {
        _swapper = null;
        _votesBeforeSwap = null;
        _applied = false;
    }

    public static void ProcessPendingPunishment()
    {
        if (!_pendingPunishment.HasValue || MeetingHud.Instance != null ||
            AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return;

        var id = _pendingPunishment.Value;
        _pendingPunishment = null; // exactly once
        var swapper = FindLiving(id);
        if (swapper == null)
            return;

        // A custom host RPC applies the actual kill to every modded client.
        // This avoids showing a local-only ghost or relying on a client
        // to decide whether the ejected player was a Crewmate.
        if (PlayerControl.LocalPlayer != null)
            Rpc<SwapperMeetingRpc>.Instance.Send(
                PlayerControl.LocalPlayer,
                new SwapperMeetingRpc.Data(2, id, 0, 0),
                immediately: true);

        ApplyPunishment(id);
    }

    public static void ApplyPunishment(byte id)
    {
        var victim = FindLiving(id);
        if (victim == null)
            return;

        victim.MurderPlayer(victim, MurderResultFlags.Succeeded);
        Notify(id, "role.Swapper.penalty");
    }

    public static void NotifyOwnSwapper(byte id, byte a, byte b)
    {
        var text = ParadoxPlugin.Localizer.Get("role.Swapper.selected")
            .Replace("{a}", a.ToString())
            .Replace("{b}", b.ToString());
        NotifyText(id, text);
    }

    public static void Notify(byte id, string key) =>
        NotifyText(id, ParadoxPlugin.Localizer.Get(key));

    private static void NotifyText(byte id, string text)
    {
        try
        {
            if (PlayerControl.LocalPlayer?.PlayerId == id &&
                HudManager.Instance?.Notifier != null)
                HudManager.Instance.Notifier.AddDisconnectMessage(text);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning($"Swapper notification: {e.Message}");
        }
    }

    private static PlayerControl? FindLiving(byte id)
    {
        var player = FindPlayer(id);
        return player?.Data != null && !player.Data.IsDead &&
               !player.Data.Disconnected ? player : null;
    }

    public static PlayerControl? FindPlayer(byte id)
    {
        foreach (var player in PlayerControl.AllPlayerControls)
            if (player != null && player.PlayerId == id)
                return player;
        return null;
    }

    public static void ResetRuntime()
    {
        _swapper = null;
        _first = 0;
        _second = 0;
        _applied = false;
        _votesBeforeSwap = null;
        _pendingPunishment = null;
    }
}
