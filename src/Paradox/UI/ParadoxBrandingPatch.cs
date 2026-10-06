using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// PARADOX main-menu identity placed inside the empty black right-hand window.
/// Keeps the vanilla AMONG US logo and menu buttons untouched.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxBrandingPatch
{
    private const string RootName = "PARADOX_MainMenuIdentity";
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

            var panel = FindRightPanel(__instance);
            if (panel == null)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    "PARADOX branding: RightPanel not found.");
                return;
            }

            GetScreenRect(panel, out var center, out var width, out var height, out var sortingLayer, out var sortingBase);

            _root = new GameObject(RootName);
            _root.transform.SetParent(panel, false);
            _root.transform.localRotation = Quaternion.identity;

            // Neutralise any unusual scaling inherited from the 2026 RightPanel.
            var ps = panel.lossyScale;
            var sx = Mathf.Abs(ps.x) > 0.0001f ? 1f / Mathf.Abs(ps.x) : 1f;
            var sy = Mathf.Abs(ps.y) > 0.0001f ? 1f / Mathf.Abs(ps.y) : 1f;
            _root.transform.localScale = new Vector3(sx, sy, 1f);

            // User-selected area: left side of the large black window, clearly
            // separated from PLAY / INVENTORY / SHOP and from the AU logo.
            var target = center + new Vector3(-width * 0.20f, height * 0.13f, -0.7f);
            _root.transform.position = target;

            CreateIdentityDecoration(_root.transform, sortingLayer, sortingBase);

            var text = CreateBrandText(__instance, _root.transform);
            if (text == null)
                return;

            text.text =
                "<size=128%><b>" +
                "<color=#55D9D2>PARA</color>" +
                "<color=#F4F7FA>DO</color>" +
                "<color=#FF5A67>X</color>" +
                "</b> <color=#FF5A67>///</color></size>\n" +
                "<size=52%><color=#AAB6C0>REALITY FRACTURE</color></size>\n" +
                $"<size=42%><color=#74828D>BY DONMILLSON  //  v{ParadoxInfo.Version}</color></size>";

            text.alignment = TextAlignmentOptions.Center;
            text.richText = true;
            text.enableWordWrapping = false;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = 1.02f;
            text.characterSpacing = 1.6f;
            text.lineSpacing = -9f;
            text.color = Color.white;
            text.outlineColor = new Color32(0, 0, 0, 230);
            text.outlineWidth = 0.09f;
            text.renderer.sortingLayerID = sortingLayer;
            text.renderer.sortingOrder = sortingBase + 12;
            text.transform.localPosition = new Vector3(0f, 0f, -0.04f);

            var rt = text.rectTransform;
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(3.15f, 1.15f);
            }

            text.gameObject.SetActive(true);

            ParadoxPlugin.Instance.Log.LogInfo(
                $"PARADOX branding positioned inside RightPanel at {target}.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX main-menu branding failed: {e}");
        }
    }

    private static Transform? FindRightPanel(MainMenuManager menu)
    {
        try
        {
            if (menu.gameModeButtons != null &&
                menu.gameModeButtons.transform.parent != null)
                return menu.gameModeButtons.transform.parent;
        }
        catch { }

        try
        {
            if (menu.mainMenuUI != null)
            {
                var found = menu.mainMenuUI.transform.Find("RightPanel");
                if (found != null)
                    return found;
            }
        }
        catch { }

        try
        {
            var go = GameObject.Find("RightPanel");
            return go != null ? go.transform : null;
        }
        catch
        {
            return null;
        }
    }

    private static void GetScreenRect(
        Transform panel,
        out Vector3 center,
        out float width,
        out float height,
        out int sortingLayer,
        out int sortingBase)
    {
        center = panel.position + new Vector3(-0.16f, 0f, 0f);
        width = 7.35f;
        height = 4.5f;
        sortingLayer = 0;
        sortingBase = 0;

        try
        {
            SpriteRenderer? renderer = null;

            var inner = panel.Find("MaskedBlackScreen");
            if (inner != null)
                renderer = inner.GetComponent<SpriteRenderer>();

            if (renderer == null)
                renderer = panel.GetComponent<SpriteRenderer>();

            if (renderer != null)
            {
                sortingLayer = renderer.sortingLayerID;
                sortingBase = renderer.sortingOrder;

                var bounds = renderer.bounds;
                if (bounds.size.x >= 3f && bounds.size.x <= 20f &&
                    bounds.size.y >= 2f && bounds.size.y <= 12f)
                {
                    center = new Vector3(bounds.center.x, bounds.center.y, panel.position.z);
                    width = bounds.size.x;
                    height = bounds.size.y;
                }
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX branding screen bounds fallback: {e.Message}");
        }
    }

    private static void CreateIdentityDecoration(
        Transform parent,
        int sortingLayer,
        int sortingBase)
    {
        var sprite = GetSolidSprite();
        if (sprite == null)
            return;

        // Short cyan line above the mark.
        var cyanGo = new GameObject("PARADOX_IdentityCyan");
        cyanGo.transform.SetParent(parent, false);
        cyanGo.transform.localPosition = new Vector3(-0.20f, 0.57f, 0.04f);
        cyanGo.transform.localScale = new Vector3(2.25f, 0.022f, 1f);
        var cyan = cyanGo.AddComponent<SpriteRenderer>();
        cyan.sprite = sprite;
        cyan.color = new Color32(85, 217, 210, 205);
        cyan.sortingLayerID = sortingLayer;
        cyan.sortingOrder = sortingBase + 9;

        // Red fracture accent at the end.
        var redGo = new GameObject("PARADOX_IdentityRed");
        redGo.transform.SetParent(parent, false);
        redGo.transform.localPosition = new Vector3(1.00f, 0.57f, 0.03f);
        redGo.transform.localScale = new Vector3(0.22f, 0.045f, 1f);
        var red = redGo.AddComponent<SpriteRenderer>();
        red.sprite = sprite;
        red.color = new Color32(255, 90, 103, 245);
        red.sortingLayerID = sortingLayer;
        red.sortingOrder = sortingBase + 10;

        // Small left bracket gives it a proper mod-logo silhouette without
        // introducing another large rectangle over the menu.
        var bracketGo = new GameObject("PARADOX_IdentityBracket");
        bracketGo.transform.SetParent(parent, false);
        bracketGo.transform.localPosition = new Vector3(-1.33f, 0.10f, 0.03f);
        bracketGo.transform.localScale = new Vector3(0.025f, 0.82f, 1f);
        var bracket = bracketGo.AddComponent<SpriteRenderer>();
        bracket.sprite = sprite;
        bracket.color = new Color32(85, 217, 210, 150);
        bracket.sortingLayerID = sortingLayer;
        bracket.sortingOrder = sortingBase + 9;
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
        catch
        {
            return null;
        }

        return _solidSprite;
    }

    private static TextMeshPro? CreateBrandText(MainMenuManager menu, Transform parent)
    {
        try
        {
            TextMeshPro? template = null;

            var candidates = new[]
            {
                menu.playButton,
                menu.inventoryButton,
                menu.shopButton,
                menu.settingsButton,
                menu.newsButton
            };

            foreach (var button in candidates)
            {
                if (button == null)
                    continue;

                template = button.buttonText != null
                    ? button.buttonText
                    : button.GetComponentInChildren<TextMeshPro>(true);

                if (template != null && template.font != null)
                    break;
            }

            if (template == null)
                return null;

            var clone = Object.Instantiate(template.gameObject, parent);
            clone.name = "PARADOX_MainMenuIdentityText";

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
            if (text == null)
            {
                Object.Destroy(clone);
                return null;
            }

            clone.transform.localPosition = Vector3.zero;
            clone.transform.localRotation = Quaternion.identity;
            clone.transform.localScale = Vector3.one;
            return text;
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX branding text clone failed: {e.Message}");
            return null;
        }
    }
}
