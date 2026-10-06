using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Keeps the original AMONG US logo intact and adds a separate PARADOX identity
/// underneath it. Uses the game's own menu text as a font/template when available.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxBrandingPatch
{
    private const string RootName = "PARADOX_MainMenuBrand";
    private static GameObject? _root;

    [HarmonyPostfix]
    public static void MainMenuStartPostfix(MainMenuManager __instance)
    {
        try
        {
            if (_root != null)
                Object.Destroy(_root);

            var existing = GameObject.Find(RootName);
            if (existing != null)
                Object.Destroy(existing);

            Transform? logo = __instance.transform.Find("MainUI/AspectScaler/LeftPanel/Sizer/LOGO-AU");
            Transform? parent = logo?.parent ?? __instance.transform;

            _root = new GameObject(RootName);
            _root.transform.SetParent(parent, false);
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;

            if (logo != null)
            {
                // Preserve the original AMONG US logo and place PARADOX as its
                // own mod identity just beneath it.
                _root.transform.localPosition =
                    logo.localPosition + new Vector3(0f, -1.22f, -8f);
            }
            else
            {
                _root.transform.localPosition = new Vector3(-3.8f, 1.6f, -8f);
            }

            var text = CreateBrandText(__instance, _root.transform);
            if (text == null)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX main-menu branding could not find a usable TMP template.");
                return;
            }

            text.text =
                "<size=132%><b>" +
                "<color=#55D9D2>PARA</color>" +
                "<color=#F4F7FA>DO</color>" +
                "<color=#FF5A67>X</color>" +
                "</b></size>\n" +
                "<size=48%><color=#8393A3>REALITY FRACTURE MOD</color></size>\n" +
                $"<size=42%><color=#B6C0C9>by DonMillson  |  v{ParadoxInfo.Version}</color></size>";

            text.alignment = TextAlignmentOptions.Center;
            text.richText = true;
            text.enableWordWrapping = false;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = 2.4f;
            text.characterSpacing = 1.5f;
            text.lineSpacing = -11f;
            text.color = Color.white;
            text.outlineColor = new Color32(2, 12, 18, 230);
            text.outlineWidth = 0.16f;

            var rt = text.rectTransform;
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(6.2f, 2.4f);
            }

            text.gameObject.SetActive(true);

            ParadoxPlugin.Instance.Log.LogInfo(
                "PARADOX original main-menu branding created under the AMONG US logo.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX main-menu branding failed: {e}");
        }
    }

    private static TextMeshPro? CreateBrandText(MainMenuManager menu, Transform parent)
    {
        try
        {
            var templateTransform = menu.transform.Find(
                "MainUI/AspectScaler/LeftPanel/Main Buttons/PlayButton/FontPlacer/Text_TMP");

            if (templateTransform != null)
            {
                var clone = Object.Instantiate(templateTransform.gameObject, parent);
                clone.name = "PARADOX_BrandText";

                var translator = clone.GetComponent<TextTranslatorTMP>();
                if (translator != null)
                    Object.DestroyImmediate(translator);

                var aspect = clone.GetComponent<AspectPosition>();
                if (aspect != null)
                    Object.DestroyImmediate(aspect);

                var text = clone.GetComponent<TextMeshPro>();
                if (text != null)
                {
                    clone.transform.localPosition = Vector3.zero;
                    clone.transform.localRotation = Quaternion.identity;
                    clone.transform.localScale = Vector3.one;
                    return text;
                }

                Object.Destroy(clone);
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX menu text template clone failed: {e.Message}");
        }

        try
        {
            var fallback = new GameObject("PARADOX_BrandText");
            fallback.transform.SetParent(parent, false);
            fallback.transform.localPosition = Vector3.zero;
            return fallback.AddComponent<TextMeshPro>();
        }
        catch
        {
            return null;
        }
    }
}
