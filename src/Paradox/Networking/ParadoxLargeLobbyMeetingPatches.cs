using HarmonyLib;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Networking;

/// <summary>
/// A preliminary 15-card paged voting grid for experimental large lobbies.
/// It deliberately leaves the network votes / native exile resolution intact.
/// </summary>
[HarmonyPatch]
public static class ParadoxLargeLobbyMeetingPatches
{
    private static IntPtr _meeting;
    private static int _page;

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(MeetingHud __instance)
    {
        _meeting = __instance != null ? __instance.Pointer : IntPtr.Zero;
        _page = 0;
        Relayout(__instance);
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(MeetingHud __instance)
    {
        if (__instance == null || __instance.playerStates == null)
            return;

        var count = __instance.playerStates.Count;
        if (!LargeLobbyPolicy.IsLargeLobby(count))
            return;

        if (_meeting != __instance.Pointer)
        {
            _meeting = __instance.Pointer;
            _page = 0;
        }

        if (__instance.state is MeetingHud.MeetingStates.Animating or MeetingHud.MeetingStates.Proceeding)
            return;

        if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.RightArrow))
            _page = LargeLobbyPagination.CyclePage(_page, +1, count);
        else if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.LeftArrow))
            _page = LargeLobbyPagination.CyclePage(_page, -1, count);

        Relayout(__instance);
    }

    private static void Relayout(MeetingHud? meeting)
    {
        if (meeting == null || meeting.playerStates == null)
            return;

        var count = meeting.playerStates.Count;
        if (!LargeLobbyPolicy.IsLargeLobby(count))
            return;

        try
        {
            _page = LargeLobbyPagination.ClampPage(_page, count);
            var idx = 0;
            foreach (var card in meeting.playerStates)
            {
                if (card == null)
                {
                    idx++;
                    continue;
                }

                var shown = LargeLobbyPagination.IsOnPage(idx, _page, count);
                card.gameObject.SetActive(shown);
                if (shown)
                {
                    var slot = LargeLobbyPagination.PositionWithinPage(idx);
                    var loc = card.transform.localPosition;
                    var basePosition = meeting.VoteOrigin;
                    var spacing = meeting.VoteButtonOffsets;
                    card.transform.localPosition = new Vector3(
                        basePosition.x + spacing.x * slot.Column,
                        basePosition.y + spacing.y * slot.Row,
                        loc.z);
                }
                idx++;
            }

            if (meeting.TimerText != null)
            {
                var current = meeting.TimerText.text ?? string.Empty;
                var marker = " [PARADOX ";
                var markerIndex = current.IndexOf(marker, StringComparison.Ordinal);
                if (markerIndex >= 0)
                    current = current.Substring(0, markerIndex);
                meeting.TimerText.text = current +
                    $" [PARADOX {_page + 1}/{LargeLobbyPagination.PageCount(count)} PgUp/PgDn]";
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance?.Log.LogWarning(
                $"PARADOX experimental large-lobby meeting UI error: {e.Message}");
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    public static void ClosePostfix()
    {
        _meeting = IntPtr.Zero;
        _page = 0;
    }
}
