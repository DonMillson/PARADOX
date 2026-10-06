using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Adds a compact PARADOX edition badge to the original Among Us logo area.
/// The vanilla AMONG US logo stays untouched.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxBrandingPatch
{
    private const string RootName = "PARADOX_MainMenuEditionBadge";
    private static GameObject? _root;
    private static Sprite? _solidSprite;

    [HarmonyPostfix]
    public static void MainMenuStartPostfix(MainMenuManager __instance)
    {
        try
        {
            if (_root != null)
                Object.Destroy(_root);

            var previous = GameObject.Find(RootName);
            if (previous != null)
                Object.Destroy(previous);

            var logo = __instance.transform.Find(
                "MainUI/AspectScaler/LeftPanel/Sizer/LOGO-AU");

            var parent = logo?.parent ?? __instance.transform;

            _root = new GameObject(RootName);
            _root.transform.SetParent(parent, false);
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;

            // Treat PARADOX like an "edition badge" attached to the lower-right
            // edge of the original logo instead of placing a second title over
            // the menu buttons.
            _root.transform.localPosition = logo != null
                ? logo.localPosition + new Vector3(1.30f, -0.50f, -8f)
                : new Vector3(-2.35f, 1.45f, -8f);

            var logoRenderer = logo?.GetComponent<SpriteRenderer>();
            var baseOrder = logoRenderer != null ? logoRenderer.sortingOrder : 0;

            CreateBadgePlate(_root.transform, baseOrder);

            var text = CreateBrandText(__instance, _root.transform);
            if (text == null)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX main-menu badge could not find a usable TMP template.");
                return;
            }

            text.text =
                "<size=118%><b>" +
                "<color=#55D9D2>PARA</color>" +
                "<color=#F3F6F8>DO</color>" +
                "<color=#FF5665>X</color>" +
                "</b></size>\n" +
                "<size=45%><color=#8FA0AC>REALITY FRACTURE</color></size>\n" +
                $"<size=38%><color=#62717D>DONMILLSON // v{ParadoxInfo.Version}</color></size>";

            text.alignment = TextAlignmentOptions.Center;
            text.richText = true;
            text.enableWordWrapping = false;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = 1.18f;
            text.characterSpacing = 1.2f;
            text.lineSpacing = -10f;
            text.color = Color.white;
            text.outlineColor = new Color32(0, 7, 11, 235);
            text.outlineWidth = 0.10f;
            text.renderer.sortingOrder = baseOrder + 5;
            text.transform.localPosition = new Vector3(0f, 0.03f, -0.08f);

            var rt = text.rectTransform;
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(2.55f, 1.04f);
            }

            text.gameObject.SetActive(true);

            ParadoxPlugin.Instance.Log.LogInfo(
                "PARADOX edition badge attached to original Among Us logo.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX main-menu branding failed: {e}");
        }
    }

    private static void CreateBadgePlate(Transform parent, int baseOrder)
    {
        var sprite = GetSolidSprite();
        if (sprite == null)
            return;

        var shadowGo = new GameObject("PARADOX_BadgeShadow");
        shadowGo.transform.SetParent(parent, false);
        shadowGo.transform.localPosition = new Vector3(0.04f, -0.04f, 0.12f);
        shadowGo.transform.localScale = new Vector3(2.58f, 1.06f, 1f);
        var shadow = shadowGo.AddComponent<SpriteRenderer>();
        shadow.sprite = sprite;
        shadow.color = new Color32(0, 0, 0, 105);
        shadow.sortingOrder = baseOrder + 1;

        var plateGo = new GameObject("PARADOX_BadgePlate");
        plateGo.transform.SetParent(parent, false);
        plateGo.transform.localPosition = new Vector3(0f, 0f, 0.10f);
        plateGo.transform.localScale = new Vector3(2.52f, 1.00f, 1f);
        var plate = plateGo.AddComponent<SpriteRenderer>();
        plate.sprite = sprite;
        plate.color = new Color32(6, 15, 20, 220);
        plate.sortingOrder = baseOrder + 2;

        var topLineGo = new GameObject("PARADOX_BadgeTopLine");
        topLineGo.transform.SetParent(parent, false);
        topLineGo.transform.localPosition = new Vector3(0f, 0.49f, 0.08f);
        topLineGo.transform.localScale = new Vector3(2.52f, 0.035f, 1f);
        var topLine = topLineGo.AddComponent<SpriteRenderer>();
        topLine.sprite = sprite;
        topLine.color = new Color32(85, 217, 210, 230);
        topLine.sortingOrder = baseOrder + 3;

        var redMarkGo = new GameObject("PARADOX_BadgeRedMark");
        redMarkGo.transform.SetParent(parent, false);
        redMarkGo.transform.localPosition = new Vector3(1.20f, 0.40f, 0.06f);
        redMarkGo.transform.localScale = new Vector3(0.08f, 0.14f, 1f);
        var redMark = redMarkGo.AddComponent<SpriteRenderer>();
        redMark.sprite = sprite;
        redMark.color = new Color32(255, 86, 101, 245);
        redMark.sortingOrder = baseOrder + 4;
    }

    private static Sprite? GetSolidSprite()
    {
        if (_solidSprite != null)
            return _solidSprite;

        try
        {
            var texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            _solidSprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                1f);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX menu UI sprite creation failed: {e.Message}");
        }

        return _solidSprite;
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
                clone.name = "PARADOX_BadgeText";

                var translator = clone.GetComponent<TextTranslatorTMP>();
                if (translator != null)
                    Object.DestroyImmediate(translator);

                var aspect = clone.GetComponent<AspectPosition>();
                if (aspect != null)
                    Object.DestroyImmediate(aspect);

                var passive = clone.GetComponent<PassiveButton>();
                if (passive != null)
                    Object.DestroyImmediate(passive);

                var collider = clone.GetComponent<Collider2D>();
                if (collider != null)
                    Object.DestroyImmediate(collider);

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
            var fallback = new GameObject("PARADOX_BadgeText");
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
