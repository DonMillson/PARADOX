using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Main-menu branding is intentionally disabled for the 2026 menu layout.
/// Previous experimental marks used transforms that could interfere visually
/// with the vanilla menu. Keep the menu clean until a dedicated asset-based
/// implementation is ready.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxBrandingPatch
{
    [HarmonyPostfix]
    public static void MainMenuStartPostfix()
    {
        // Remove any legacy PARADOX branding objects that may still exist
        // after returning to the menu within the same game process.
        var names = new[]
        {
            "PARADOX_MainMenuIdentity",
            "PARADOX_MainMenuMark",
            "PARADOX_MainMenuEditionBadge",
            "PARADOX_MainMenuBrand",
            "PARADOX_Branding"
        };

        foreach (var name in names)
        {
            var go = GameObject.Find(name);
            if (go != null)
                Object.Destroy(go);
        }
    }
}
