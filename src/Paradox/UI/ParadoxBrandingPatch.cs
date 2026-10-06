using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Paradox.UI;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxBrandingPatch
{
    private const string ObjectName = "PARADOX_Branding";

    [HarmonyPostfix]
    public static void MainMenuStartPostfix()
    {
        if (GameObject.Find(ObjectName) != null)
            return;

        var gameObject = new GameObject(ObjectName);
        UnityEngine.Object.DontDestroyOnLoad(gameObject);

        var text = gameObject.AddComponent<TextMeshPro>();
        text.text = $"PARADOX v{ParadoxInfo.Version} — by DonMillson";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 2.2f;

        gameObject.transform.position = new Vector3(0f, -2.75f, -10f);
    }
}
