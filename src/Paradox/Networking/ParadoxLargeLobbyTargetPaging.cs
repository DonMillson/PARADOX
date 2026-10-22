using HarmonyLib;
using Paradox.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.Networking;

/// <summary>
/// Experimental large-lobby paging for the two stock panels which assume <= 15 players.
/// No network data, selections or role RPCs are altered. These are UI-only patches.
/// </summary>
[HarmonyPatch]
public static class ParadoxLargeLobbyTargetPaging
{
    private static VitalsMinigame? _vitals;
    private static ShapeshifterMinigame? _shifter;
    private static TextMeshPro? _vitalsPager;
    private static TextMeshPro? _shifterPager;
    private static int _vitalsPage;
    private static int _shifterPage;

    [HarmonyPatch(typeof(VitalsMinigame), nameof(VitalsMinigame.Begin))]
    [HarmonyPostfix]
    public static void VitalsBeginPostfix(VitalsMinigame __instance)
    {
        ResetVitals();
        if (__instance?.vitals == null ||
            __instance.vitals.Count <= LargeLobbyPagination.CardsPerPage)
            return;

        _vitals = __instance;
        _vitalsPage = 0;
        _vitalsPager = CreatePageLabel(__instance.transform,
            new Vector3(2.7f, -2f, -1f));
        LayoutVitals();
    }

    [HarmonyPatch(typeof(ShapeshifterMinigame), nameof(ShapeshifterMinigame.Begin))]
    [HarmonyPostfix]
    public static void ShifterBeginPostfix(ShapeshifterMinigame __instance)
    {
        ResetShifter();
        if (__instance?.potentialVictims == null ||
            __instance.potentialVictims.Count <= LargeLobbyPagination.CardsPerPage)
            return;

        _shifter = __instance;
        _shifterPage = 0;
        _shifterPager = CreatePageLabel(__instance.transform,
            new Vector3(4.1f, -2.36f, -1f));
        LayoutShifter();
    }

    // HudManager.Update is already present in supported 2026 GameLibs;
    // driving the overlays here avoids patching virtual Minigame.Update.
    [HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
    [HarmonyPostfix]
    public static void HudUpdatePostfix()
    {
        if (_vitals != null)
        {
            if (_vitals.gameObject == null || !_vitals.gameObject.activeInHierarchy)
                ResetVitals();
            else
            {
                if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.RightArrow))
                    _vitalsPage = LargeLobbyPagination.CyclePage(
                        _vitalsPage, +1, _vitals.vitals.Count);
                else if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.LeftArrow))
                    _vitalsPage = LargeLobbyPagination.CyclePage(
                        _vitalsPage, -1, _vitals.vitals.Count);
                LayoutVitals();
            }
        }

        if (_shifter != null)
        {
            if (_shifter.gameObject == null || !_shifter.gameObject.activeInHierarchy)
                ResetShifter();
            else
            {
                if (Input.GetKeyDown(KeyCode.PageDown) || Input.GetKeyDown(KeyCode.RightArrow))
                    _shifterPage = LargeLobbyPagination.CyclePage(
                        _shifterPage, +1, _shifter.potentialVictims.Count);
                else if (Input.GetKeyDown(KeyCode.PageUp) || Input.GetKeyDown(KeyCode.LeftArrow))
                    _shifterPage = LargeLobbyPagination.CyclePage(
                        _shifterPage, -1, _shifter.potentialVictims.Count);
                LayoutShifter();
            }
        }
    }

    private static TextMeshPro? CreatePageLabel(Transform parent, Vector3 location)
    {
        try
        {
            var source = HudManager.Instance?.KillButton?.cooldownTimerText;
            if (source == null)
                return null;
            var label = Object.Instantiate(source, parent);
            label.name = "PARADOX_LargeLobbyPageIndicator";
            label.enableWordWrapping = false;
            label.transform.localPosition = location;
            label.transform.localScale *= 0.5f;
            label.gameObject.SetActive(true);
            return label;
        }
        catch (Exception error)
        {
            ParadoxPlugin.Instance?.Log.LogWarning(
                $"Large-lobby page label unavailable: {error.Message}");
            return null;
        }
    }

    private static void LayoutVitals()
    {
        if (_vitals?.vitals == null)
            return;
        try
        {
            var count = _vitals.vitals.Count;
            _vitalsPage = LargeLobbyPagination.ClampPage(_vitalsPage, count);
            var index = 0;
            foreach (var panel in _vitals.vitals)
            {
                if (panel != null)
                {
                    var visible = LargeLobbyPagination.IsOnPage(
                        index, _vitalsPage, count);
                    if (panel.gameObject.activeSelf != visible)
                        panel.gameObject.SetActive(visible);
                    if (visible)
                    {
                        var (row, column) = LargeLobbyPagination.PositionWithinPage(index);
                        var transform = panel.transform;
                        transform.localPosition = new Vector3(
                            _vitals.XStart + _vitals.XOffset * column,
                            _vitals.YStart + _vitals.YOffset * row,
                            transform.localPosition.z);
                    }
                }
                index++;
            }

            if (_vitalsPager != null)
                _vitalsPager.text = $"PARADOX VITALS {_vitalsPage + 1}/" +
                    $"{LargeLobbyPagination.PageCount(count)} PgUp/PgDn";
        }
        catch (Exception error)
        {
            ParadoxPlugin.Instance?.Log.LogWarning(
                $"Large-lobby vitals layout error: {error.Message}");
            ResetVitals();
        }
    }

    private static void LayoutShifter()
    {
        if (_shifter?.potentialVictims == null)
            return;
        try
        {
            var count = _shifter.potentialVictims.Count;
            _shifterPage = LargeLobbyPagination.ClampPage(_shifterPage, count);
            var index = 0;
            foreach (var panel in _shifter.potentialVictims)
            {
                if (panel != null)
                {
                    var visible = LargeLobbyPagination.IsOnPage(
                        index, _shifterPage, count);
                    if (panel.gameObject.activeSelf != visible)
                        panel.gameObject.SetActive(visible);
                    if (visible)
                    {
                        var (row, column) = LargeLobbyPagination.PositionWithinPage(index);
                        var transform = panel.transform;
                        transform.localPosition = new Vector3(
                            _shifter.XStart + _shifter.XOffset * column,
                            _shifter.YStart + _shifter.YOffset * row,
                            transform.localPosition.z);
                    }
                }
                index++;
            }

            if (_shifterPager != null)
                _shifterPager.text = $"PARADOX WYBOR CELU {_shifterPage + 1}/" +
                    $"{LargeLobbyPagination.PageCount(count)} PgUp/PgDn";
        }
        catch (Exception error)
        {
            ParadoxPlugin.Instance?.Log.LogWarning(
                $"Large-lobby shifter layout error: {error.Message}");
            ResetShifter();
        }
    }

    private static void ResetVitals()
    {
        if (_vitalsPager != null)
            Object.Destroy(_vitalsPager.gameObject);
        _vitalsPager = null;
        _vitals = null;
        _vitalsPage = 0;
    }

    private static void ResetShifter()
    {
        if (_shifterPager != null)
            Object.Destroy(_shifterPager.gameObject);
        _shifterPager = null;
        _shifter = null;
        _shifterPage = 0;
    }
}
