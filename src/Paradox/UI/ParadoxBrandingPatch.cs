using HarmonyLib;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Minimal PARADOX credit. Avoids large branding elements that compete with
/// the vanilla Among Us UI and keeps only a subtle corner signature.
/// </summary>
[HarmonyPatch]
public static class ParadoxBrandingPatch
{
    private static GameObject? _root;
    private static TextMeshPro? _label;

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void LobbyStartPostfix(GameStartManager __instance)
    {
        DestroyBranding();
        TryCreateFromLobby(__instance);
    }

    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Start))]
    [HarmonyPostfix]
    public static void HudStartPostfix(HudManager __instance)
    {
        if (_label != null)
            return;

        TryCreateFromHud(__instance);
    }

    [HarmonyPatch(typeof(EndGameManager), nameof(EndGameManager.Start))]
    [HarmonyPrefix]
    public static void EndGameStartPrefix() =>
        DestroyBranding();

    private static void TryCreateFromLobby(GameStartManager instance)
    {
        TextMeshPro? template = instance.GameRoomNameCode;
        if (template == null || template.font == null)
            template = instance.PlayerCounter;
        if (template == null || template.font == null)
            template = instance.GameStartText;

        TryCreate(template, HudManager.Instance?.transform);
    }

    private static void TryCreateFromHud(HudManager hud)
    {
        TextMeshPro? template = null;

        try
        {
            if (hud.TaskPanel != null)
                template = hud.TaskPanel.GetComponentInChildren<TextMeshPro>(true);
        }
        catch { }

        TryCreate(template, hud.transform);
    }

    private static void TryCreate(TextMeshPro? template, Transform? parent)
    {
        if (_label != null || template == null || parent == null)
            return;

        try
        {
            _root = new GameObject("PARADOX_CornerCredit");
            _root.transform.SetParent(parent, false);
            _root.transform.localPosition = Vector3.zero;
            _root.transform.localScale = Vector3.one;

            var anchor = _root.AddComponent<AspectPosition>();
            anchor.Alignment = AspectPosition.EdgeAlignments.RightBottom;
            anchor.DistanceFromEdge = new Vector3(0.26f, 0.20f, -20f);
            anchor.updateAlways = true;
            anchor.AdjustPosition();

            var go = Object.Instantiate(template.gameObject, _root.transform);
            go.name = "PARADOX_CornerCreditText";

            foreach (var component in go.GetComponents<Component>())
            {
                if (component == null)
                    continue;

                if (component.TryCast<TextTranslatorTMP>() != null ||
                    component.TryCast<AspectPosition>() != null ||
                    component.TryCast<PassiveButton>() != null ||
                    component.TryCast<Collider2D>() != null)
                {
                    Object.DestroyImmediate(component);
                }
            }

            for (var i = go.transform.childCount - 1; i >= 0; i--)
                Object.Destroy(go.transform.GetChild(i).gameObject);

            _label = go.GetComponent<TextMeshPro>();
            if (_label == null)
            {
                DestroyBranding();
                return;
            }

            go.SetActive(true);
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;

            _label.enabled = true;
            _label.text = "Paradox by DonMillson";
            _label.fontSize = 0.34f;
            _label.alignment = TextAlignmentOptions.BottomRight;
            _label.color = new Color(0.82f, 0.88f, 0.90f, 0.55f);
            _label.outlineWidth = 0.06f;
            _label.outlineColor = new Color(0f, 0f, 0f, 0.50f);
            _label.richText = false;
            _label.enableWordWrapping = false;
            _label.overflowMode = TextOverflowModes.Overflow;
            _label.renderer.sortingOrder = 18;

            var rt = _label.rectTransform;
            if (rt != null)
            {
                rt.pivot = new Vector2(1f, 0f);
                rt.sizeDelta = new Vector2(3.2f, 0.45f);
            }

            ParadoxFontSupport.ApplyTo(_label);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX corner credit could not be created: {e.Message}");
            DestroyBranding();
        }
    }

    private static void DestroyBranding()
    {
        if (_root != null)
            Object.Destroy(_root);

        _root = null;
        _label = null;
    }
}
