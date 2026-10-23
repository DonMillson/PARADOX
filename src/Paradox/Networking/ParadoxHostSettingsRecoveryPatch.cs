using HarmonyLib;
using Paradox.UI;
using UnityEngine;

namespace Paradox.Networking;

/// <summary>
/// Recovery when GameSettingMenu.Start ran before the player became host.
/// The standard lobby Start/Update is known to exist in pinned 2026 GameLibs.
/// F10 works while host settings are open, without changing original UI state.
/// </summary>
[HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
public static class ParadoxHostSettingsRecoveryPatch
{
    private static float _nextScan;

    [HarmonyPostfix]
    public static void GameStartUpdatePostfix()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost ||
            Time.realtimeSinceStartup < _nextScan)
            return;

        _nextScan = Time.realtimeSinceStartup + 0.75f;
        try
        {
            foreach (var menu in UnityEngine.Object.FindObjectsOfType<GameSettingMenu>())
            {
                if (menu == null || menu.gameObject == null ||
                    !menu.gameObject.activeInHierarchy)
                    continue;

                // A reliable second chance when role/lobby settings prefab
                // has been kept alive from a previous non-host session.
                if (Input.GetKey(KeyCode.F10))
                    ParadoxLobbySettingsPatch.TryOpenViaShortcut(menu);
                else
                    ParadoxLobbySettingsPatch.TryCreateTab(menu);
            }
        }
        catch (Exception e)
        {
            _nextScan = Time.realtimeSinceStartup + 5f;
            ParadoxPlugin.Instance?.Log.LogWarning(
                $"PARADOX host lobby recovery failed: {e.Message}");
        }
    }
}
