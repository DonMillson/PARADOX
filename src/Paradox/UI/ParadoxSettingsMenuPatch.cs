using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// The old PARADOX main-menu settings launcher cloned the vanilla Quit button
/// and could leave an inherited "QUIT" label on top of Settings in the 2026 UI.
/// Settings are now configured from the native PARADOX lobby tab instead.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxSettingsMenuPatch
{
    [HarmonyPostfix]
    public static void MainMenuStartPostfix()
    {
        // Remove any launcher/root left behind after returning to the menu.
        var names = new[]
        {
            "PARADOX_SettingsButton",
            "PARADOX_SettingsRoot",
            "PARADOX_SettingsContent"
        };

        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.Destroy(go);
        }
    }
}
