using HarmonyLib;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Networking;

/// <summary>
/// Experimental host-side create-lobby capacity adapter for *modded servers*.
/// Never alters vanilla Innersloth regions. A 50-seat UI is not proof that the
/// server accepts 50 clients or that meeting/vitals HUD can render everyone.
/// </summary>
[HarmonyPatch]
public static class ParadoxLargeLobbyPatches
{
    [HarmonyPatch(typeof(CreateGameOptions), nameof(CreateGameOptions.Show))]
    [HarmonyPrefix]
    public static void CreateGameShowPrefix(CreateGameOptions __instance)
    {
        try
        {
            if (__instance == null || ParadoxPlugin.Instance == null)
                return;

            var requested = ParadoxPlugin.Instance.ExperimentalLobbyMaxPlayers;
            var enabled = ParadoxPlugin.Instance.ExperimentalLargeLobbiesEnabled;
            var privateServer = IsCustomServerSelected();
            if (!LargeLobbyPolicy.CanUnlock(enabled, privateServer))
            {
                if (enabled && !privateServer)
                    ParadoxPlugin.Instance.Log.LogWarning(
                        "PARADOX 50-player test mode blocked: a custom/modded Among Us server is required. Official server player limit remains unchanged.");
                return;
            }

            if (__instance.capacitySetting == null || __instance.capacityOption == null)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX large lobby: native capacity picker was not present; leaving vanilla capacity untouched.");
                return;
            }

            var max = LargeLobbyPolicy.EffectiveMaxPlayers(requested, enabled, privateServer);
            __instance.capacitySetting.ValidRange = new IntRange(LargeLobbyPolicy.MinimumPlayers, max);
            __instance.capacityOption.ValidRange = new FloatRange(LargeLobbyPolicy.MinimumPlayers, max);
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX EXPERIMENTAL: lobby picker permits up to {max} seats on a custom server. " +
                "No 16–50 player runtime or vote UI compatibility has been certified.");
        }
        catch (Exception error)
        {
            // Fail safely rather than preventing the game from creating a normal room.
            ParadoxPlugin.Instance?.Log.LogError(
                $"PARADOX experimental large lobby picker could not initialize: {error}");
        }
    }

    /// <summary>
    /// Fail closed for unknown or official regions: do not silently attempt
    /// to overload public matchmaking with a locally enlarged capacity.
    /// </summary>
    private static bool IsCustomServerSelected()
    {
        var server = ServerManager.Instance;
        var region = server?.CurrentRegion;
        if (region == null)
            return false;

        return region.TranslateName is not (
            StringNames.ServerAS or StringNames.ServerEU or StringNames.ServerNA);
    }
}
