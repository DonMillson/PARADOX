using System.Reflection;
using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Swapper;

/// <summary>
/// Temporary input until a native meeting picker is completed. /swap A B is
/// swallowed locally, never sent into public meeting chat.
/// Reflection is used here to support both Text and text-style chat fields.
/// </summary>
[HarmonyPatch(typeof(ChatController), nameof(ChatController.SendChat))]
public static class SwapperChatCommandPatch
{
    [HarmonyPrefix]
    public static bool Prefix(ChatController __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !SwapperRole.IsSwapper(local.PlayerId) ||
            MeetingHud.Instance == null)
            return true;

        var input = AccessTools.Field(typeof(ChatController), "freeChatField")
            ?.GetValue(__instance);
        if (input == null)
            return true;

        var raw = ReadText(input);
        if (raw == null || !(raw.TrimStart().StartsWith(
            "/swap", StringComparison.OrdinalIgnoreCase) ||
            raw.TrimStart().StartsWith("/sw ", StringComparison.OrdinalIgnoreCase)))
            return true;

        // Do not leak the special role/targets into the meeting chat.
        WriteText(input, string.Empty);
        var parts = raw.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 3 || !byte.TryParse(parts[1], out var a) ||
            !byte.TryParse(parts[2], out var b))
        {
            SwapperRole.Notify(local.PlayerId, "role.Swapper.invalid");
            return false;
        }

        if (AmongUsClient.Instance == null ||
            a == b || a == local.PlayerId || b == local.PlayerId ||
            a >= SwapperVoteResolver.Skip || b >= SwapperVoteResolver.Skip)
        {
            SwapperRole.Notify(local.PlayerId, "role.Swapper.invalid");
            return false;
        }

        if (AmongUsClient.Instance.AmHost)
        {
            if (!SwapperRole.TrySetSwap(local, a, b))
                SwapperRole.Notify(local.PlayerId, "role.Swapper.invalid");
        }
        else
        {
            Rpc<SwapperMeetingRpc>.Instance.Send(local,
                new SwapperMeetingRpc.Data(0, local.PlayerId, a, b),
                immediately: true);
        }
        return false;
    }

    private static string? ReadText(object input)
    {
        try
        {
            var type = input.GetType();
            foreach (var name in new[] { "Text", "text" })
            {
                var property = AccessTools.Property(type, name);
                if (property?.GetValue(input) is string fromProperty)
                    return fromProperty;
                var field = AccessTools.Field(type, name);
                if (field?.GetValue(input) is string fromField)
                    return fromField;
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Swapper chat input read failed: {e.Message}");
        }
        return null;
    }

    private static void WriteText(object input, string text)
    {
        try
        {
            var type = input.GetType();
            foreach (var name in new[] { "Text", "text" })
            {
                var property = AccessTools.Property(type, name);
                if (property != null && property.CanWrite)
                {
                    property.SetValue(input, text);
                    return;
                }
                var field = AccessTools.Field(type, name);
                if (field != null)
                {
                    field.SetValue(input, text);
                    return;
                }
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Swapper chat input clear failed: {e.Message}");
        }
    }
}
