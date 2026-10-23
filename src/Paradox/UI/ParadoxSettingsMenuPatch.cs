using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Give an independent, visible indicator that ParadoxPlugin.Load and Harmony
/// reached the main menu. Host role settings are in the lobby, not the main menu.
/// A visible indicator is NOT proof of multiplayer/role correctness.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxSettingsMenuPatch
{
    private const string IndicatorName = "PARADOX_MainMenuStatus";

    [HarmonyPostfix]
    public static void MainMenuStartPostfix(MainMenuManager __instance)
    {
        foreach (var name in new[]
        {
            "PARADOX_SettingsButton",
            "PARADOX_SettingsRoot",
            "PARADOX_SettingsContent",
            IndicatorName
        })
        {
            var old = GameObject.Find(name);
            if (old != null)
                Object.Destroy(old);
        }

        try
        {
            if (__instance == null)
                return;

            var fontSource = __instance.GetComponentsInChildren<TextMeshPro>(true)
                .FirstOrDefault(label => label != null && label.font != null);
            if (fontSource == null)
            {
                ParadoxPlugin.Instance?.Log.LogWarning(
                    "PARADOX main-menu status: no suitable text template.");
                return;
            }

            var root = new GameObject(IndicatorName);
            root.layer = fontSource.gameObject.layer;
            var text = root.AddComponent<TextMeshPro>();
            text.font = fontSource.font;
            text.fontSharedMaterial = fontSource.fontSharedMaterial;
            text.fontSize = 2.15f;
            text.color = new Color32(65, 224, 240, 255);
            text.alignment = TextAlignmentOptions.Left;
            text.enableWordWrapping = false;
            text.text = $"PARADOX {ParadoxInfo.Version}  •  Ustawienia: HOST > Opcje pokoju";
            root.transform.SetParent(__instance.transform, false);
            root.transform.localScale = Vector3.one;

            var cam = Camera.main;
            if (cam != null)
                root.transform.position = cam.ViewportToWorldPoint(
                    new Vector3(0.035f, 0.09f, 10f));

            var renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.sortingOrder = 150;

            ParadoxPlugin.Instance?.Log.LogInfo(
                "PARADOX MAIN MENU INDICATOR READY: plugin code reached main menu.");
        }
        catch (Exception error)
        {
            ParadoxPlugin.Instance?.Log.LogWarning(
                $"PARADOX main-menu status indicator unavailable: {error}");
        }
    }
}
