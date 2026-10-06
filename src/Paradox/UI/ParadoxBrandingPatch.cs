using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Small PARADOX edition mark integrated with the original AMONG US logo.
/// It intentionally has no large backing plate, so it cannot cover menu buttons.
/// </summary>
[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxBrandingPatch
{
    private const string RootName = "PARADOX_MainMenuMark";
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

            _root = new GameObject(RootName);
            _root.transform.SetParent(__instance.transform, false);
            _root.transform.localRotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;

            if (logo != null)
            {
                // Use world space from the real AU logo, but parent the mark to
                // MainMenuManager itself. This avoids the stretched Sizer scale
                // that previously created the huge cyan line / oversized badge.
                _root.transform.position =
                    logo.position + new Vector3(1.15f, -0.48f, -0.45f);
            }
            else
            {
                _root.transform.localPosition = new Vector3(-3.0f, 1.55f, -8f);
            }

            var logoRenderer = logo?.GetComponent<SpriteRenderer>();
            var baseOrder = logoRenderer != null ? logoRenderer.sortingOrder : 0;

            CreateAccent(_root.transform, baseOrder);

            var text = CreateBrandText(__instance, _root.transform);
            if (text == null)
                return;

            text.text =
                "<size=108%><b><color=#55D9D2>PARADOX</color></b> " +
                "<color=#FF5A67>///</color></size>\n" +
                $"<size=42%><color=#8B99A5>REALITY FRACTURE  •  DONMILLSON  •  v{ParadoxInfo.Version}</color></size>";

            text.alignment = TextAlignmentOptions.Center;
            text.richText = true;
            text.enableWordWrapping = false;
            text.fontStyle = FontStyles.Normal;
            text.fontSize = 0.72f;
            text.characterSpacing = 1.0f;
            text.lineSpacing = -12f;
            text.color = Color.white;
            text.outlineColor = new Color32(0, 0, 0, 220);
            text.outlineWidth = 0.08f;
            text.renderer.sortingOrder = baseOrder + 5;
            text.transform.localPosition = new Vector3(0f, 0f, -0.05f);

            var rt = text.rectTransform;
            if (rt != null)
            {
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(2.05f, 0.65f);
            }

            text.gameObject.SetActive(true);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX main-menu mark failed: {e}");
        }
    }

    private static void CreateAccent(Transform parent, int baseOrder)
    {
        var sprite = GetSolidSprite();
        if (sprite == null)
            return;

        var lineGo = new GameObject("PARADOX_MarkLine");
        lineGo.transform.SetParent(parent, false);
        lineGo.transform.localPosition = new Vector3(0f, 0.31f, 0.04f);
        lineGo.transform.localScale = new Vector3(1.55f, 0.018f, 1f);

        var line = lineGo.AddComponent<SpriteRenderer>();
        line.sprite = sprite;
        line.color = new Color32(85, 217, 210, 190);
        line.sortingOrder = baseOrder + 3;

        var redGo = new GameObject("PARADOX_MarkRed");
        redGo.transform.SetParent(parent, false);
        redGo.transform.localPosition = new Vector3(0.79f, 0.31f, 0.03f);
        redGo.transform.localScale = new Vector3(0.09f, 0.028f, 1f);

        var red = redGo.AddComponent<SpriteRenderer>();
        red.sprite = sprite;
        red.color = new Color32(255, 90, 103, 245);
        red.sortingOrder = baseOrder + 4;
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
            var templateTransform = menu.transform.Find(
                "MainUI/AspectScaler/LeftPanel/Main Buttons/PlayButton/FontPlacer/Text_TMP");

            if (templateTransform != null)
            {
                var clone = Object.Instantiate(templateTransform.gameObject, parent);
                clone.name = "PARADOX_MainMenuMarkText";

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
                $"PARADOX menu text clone failed: {e.Message}");
        }

        return null;
    }
}
