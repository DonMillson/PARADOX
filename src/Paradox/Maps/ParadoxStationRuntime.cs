using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Maps;

/// <summary>
/// Opt-in host controlled experimental walkable scene (F7).
/// Normal public matches are unchanged until the host explicitly opts in.
/// This is not yet a registered Among Us ShipStatus map.
/// </summary>
[HarmonyPatch]
public static class ParadoxStationRuntime
{
    private static readonly Dictionary<byte, Vector2> ReturnPositions = new();
    private static readonly byte[] Steps = new byte[12];
    private static readonly float[] LastInteractedAt = new float[12];

    public static bool Active { get; private set; }
    public static int CompleteTasks => Steps.Count(x => x >= 3);

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix()
    {
        if (AmongUsClient.Instance == null ||
            PlayerControl.LocalPlayer == null)
            return;

        if (Input.GetKeyDown(KeyCode.F7) &&
            AmongUsClient.Instance.AmHost && MeetingHud.Instance == null)
            TryToggleHost();

        if (Active && Input.GetKeyDown(KeyCode.F8) && MeetingHud.Instance == null)
            TryInteract();
    }

    public static void TryToggleHost()
    {
        if (AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            PlayerControl.LocalPlayer == null)
            return;

        if (!Active && ShipStatus.Instance == null)
        {
            Feedback("PARADOX STATION: start a match before entering the test scene.");
            return;
        }

        if (!Active && !AllClientsCompatible())
        {
            Feedback("PARADOX STATION: all players must have the same PARADOX build.");
            return;
        }

        try
        {
            var enable = !Active;
            ApplyMode(enable, isHost: true);
            Rpc<StationModeRpc>.Instance.Send(
                PlayerControl.LocalPlayer,
                new StationModeRpc.Data(enable),
                immediately: true);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogError(
                $"PARADOX STATION transition failed: {e}");
            RestoreHostPlayers();
            Reset();
        }
    }

    public static void ApplyMode(bool enable, bool isHost)
    {
        if (enable == Active)
            return;

        if (enable)
        {
            ParadoxStationScene.Build();
            Active = true;
            Array.Clear(Steps, 0, Steps.Length);
            Array.Clear(LastInteractedAt, 0, LastInteractedAt.Length);

            if (isHost)
            {
                ReturnPositions.Clear();
                var i = 0;
                foreach (var player in PlayerControl.AllPlayerControls)
                {
                    if (player == null || player.Data == null ||
                        player.Data.Disconnected || player.NetTransform == null)
                        continue;

                    ReturnPositions[player.PlayerId] = player.GetTruePosition();
                    var offset = new Vector2((i % 4) * 0.46f, (i / 4) * 0.48f);
                    player.NetTransform.SnapTo(ParadoxStationScene.Spawn + offset);
                    i++;
                }
            }

            Feedback("PARADOX STATION: walkable prototype. F8: consoles; host F7: exit.");
        }
        else
        {
            if (isHost)
                RestoreHostPlayers();

            Reset();
            Feedback("PARADOX STATION: prototype closed.");
        }
    }

    private static void RestoreHostPlayers()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.NetTransform == null ||
                !ReturnPositions.TryGetValue(player.PlayerId, out var position))
                continue;
            player.NetTransform.SnapTo(position);
        }
        ReturnPositions.Clear();
    }

    private static bool AllClientsCompatible()
    {
        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Disconnected)
                continue;
            if (player.OwnerId == AmongUsClient.Instance.HostId)
                continue;
            if (ParadoxPlugin.Clients.GetCompatibility(player.OwnerId) !=
                ClientCompatibility.Compatible)
                return false;
        }

        return true;
    }

    private static void TryInteract()
    {
        var player = PlayerControl.LocalPlayer;
        if (player == null || player.Data == null || player.Data.IsDead ||
            !player.CanMove)
            return;

        var position = player.GetTruePosition();
        var nearest = -1;
        var best = 1.6f;

        for (var i = 0; i < ParadoxStation.Rooms.Count; i++)
        {
            if (Steps[i] >= 3)
                continue;
            var distance = Vector2.Distance(
                position, ParadoxStationScene.ConsolePosition(i));
            if (distance < best)
            {
                nearest = i;
                best = distance;
            }
        }

        if (nearest < 0)
        {
            Feedback("PARADOX STATION: stand near a yellow console and press F8.");
            return;
        }

        if (AmongUsClient.Instance.AmHost)
            AcceptTaskHost(player, nearest);
        else
            Rpc<StationTaskRequestRpc>.Instance.Send(
                player,
                new StationTaskRequestRpc.Data((byte)nearest),
                immediately: true);
    }

    public static void AcceptTaskHost(PlayerControl actor, int roomIndex)
    {
        if (!Active || AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            actor == null || actor.Data == null ||
            actor.Data.IsDead || actor.Data.Disconnected ||
            MeetingHud.Instance != null ||
            roomIndex < 0 || roomIndex >= Steps.Length ||
            Steps[roomIndex] >= 3)
            return;

        if (Vector2.Distance(
                actor.GetTruePosition(),
                ParadoxStationScene.ConsolePosition(roomIndex)) > 1.65f)
            return;

        var now = Time.time;
        if (now - LastInteractedAt[roomIndex] < 0.75f)
            return;

        LastInteractedAt[roomIndex] = now;
        var stage = (byte)(Steps[roomIndex] + 1);
        SetTaskStep(roomIndex, stage);

        var sender = PlayerControl.LocalPlayer;
        if (sender != null)
            Rpc<StationTaskStateRpc>.Instance.Send(
                sender,
                new StationTaskStateRpc.Data((byte)roomIndex, stage),
                immediately: true);
    }

    public static void SetTaskStep(int index, byte stage)
    {
        if (!Active || index < 0 || index >= Steps.Length || stage > 3)
            return;

        // Do not regress task state on a reordered or duplicated network packet.
        if (stage <= Steps[index])
            return;

        Steps[index] = stage;
        Feedback($"PARADOX STATION: console {index + 1}/12: {stage}/3; " +
                 $"completed {CompleteTasks}/12.");
        if (CompleteTasks == Steps.Length)
            Feedback("PARADOX STATION: all experimental console objectives completed!");
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void MeetingStartPostfix()
    {
        // Do not strand players off-map during a vanilla meeting transition.
        if (Active && AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
            TryForceExitHost();
    }

    public static void TryForceExitHost()
    {
        if (!Active || AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return;

        var sender = PlayerControl.LocalPlayer;
        ApplyMode(false, isHost: true);
        if (sender != null)
            Rpc<StationModeRpc>.Instance.Send(
                sender,
                new StationModeRpc.Data(false),
                immediately: true);
    }

    public static void Reset()
    {
        Active = false;
        ReturnPositions.Clear();
        Array.Clear(Steps, 0, Steps.Length);
        Array.Clear(LastInteractedAt, 0, LastInteractedAt.Length);
        ParadoxStationScene.Reset();
    }

    private static void Feedback(string message)
    {
        try
        {
            if (HudManager.Instance != null && HudManager.Instance.Notifier != null)
                HudManager.Instance.Notifier.AddDisconnectMessage(message);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Station status notification failed: {e.Message}");
        }
    }
}
