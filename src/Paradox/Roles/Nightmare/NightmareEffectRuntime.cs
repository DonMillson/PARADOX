using HarmonyLib;
using UnityEngine;

namespace Paradox.Roles.Nightmare;

public static class NightmareEffectRuntime
{
    private static SpriteRenderer? _overlay;
    private static float _effectEndsAt;
    private static float _startedAt;

    public static void ApplyIfLocal(byte targetPlayerId, float durationSeconds)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.PlayerId != targetPlayerId)
            return;

        _startedAt = Time.time;
        _effectEndsAt = Math.Max(_effectEndsAt, Time.time + Math.Max(0f, durationSeconds));
    }

    public static void Update(HudManager hud)
    {
        if (hud == null)
            return;

        if (Time.time >= _effectEndsAt)
        {
            Hide();
            return;
        }

        if (MeetingHud.Instance != null)
        {
            Hide();
            return;
        }

        EnsureOverlay(hud);
        if (_overlay == null)
            return;

        var elapsed = Math.Max(0f, Time.time - _startedAt);
        var pulse = (Mathf.Sin(elapsed * 8f) + 1f) * 0.5f;
        var black = new Color(0f, 0f, 0f, 0.58f + pulse * 0.15f);
        var red = new Color(0.32f, 0f, 0.03f, 0.48f + (1f - pulse) * 0.16f);
        _overlay.color = Color.Lerp(black, red, pulse);
    }

    private static void EnsureOverlay(HudManager hud)
    {
        if (_overlay != null)
            return;

        try
        {
            if (hud.FullScreen == null)
                return;

            _overlay = UnityEngine.Object.Instantiate(
                hud.FullScreen,
                hud.FullScreen.transform.parent);

            _overlay.name = "PARADOX_NightmareOverlay";
            _overlay.gameObject.SetActive(true);
            _overlay.enabled = true;
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.sortingOrder = hud.FullScreen.sortingOrder + 3;
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Nightmare overlay creation failed: {e.Message}");
            _overlay = null;
        }
    }

    private static void Hide()
    {
        if (_overlay != null)
            _overlay.color = new Color(0f, 0f, 0f, 0f);
    }

    public static void Reset()
    {
        try
        {
            if (_overlay != null)
                UnityEngine.Object.Destroy(_overlay.gameObject);
        }
        catch { }

        _overlay = null;
        _effectEndsAt = 0f;
        _startedAt = 0f;
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class NightmareEffectHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance) =>
        NightmareEffectRuntime.Update(__instance);
}
