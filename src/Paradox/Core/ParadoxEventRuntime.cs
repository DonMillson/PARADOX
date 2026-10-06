using HarmonyLib;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.Core;

/// <summary>
/// Client-side presentation and short gameplay effects for Paradox thresholds.
/// 25% = Reality Disturbance (visual interference + light camera shake).
/// 50% = Reality Distortion (stronger interference + 6s role-ability jam).
/// 75% = Critical Instability (heavy interference + intermittent blackout + 8s role-ability jam).
/// </summary>
public static class ParadoxEventRuntime
{
    private static readonly HashSet<ParadoxThreshold> Triggered = new();

    private static ParadoxThreshold _activeThreshold = ParadoxThreshold.None;
    private static float _effectStartedAt;
    private static float _effectEndsAt;

    private static SpriteRenderer? _overlay;
    private static float _previousShakeAmount;
    private static float _previousShakePeriod;
    private static bool _shakeCaptured;

    public static bool RoleAbilitiesBlocked =>
        _activeThreshold is ParadoxThreshold.Distortion or ParadoxThreshold.Instability &&
        Time.time < _effectEndsAt;

    public static void Trigger(ParadoxEvent paradoxEvent)
    {
        if (paradoxEvent == null ||
            paradoxEvent.Threshold is not (ParadoxThreshold.Disturbance or ParadoxThreshold.Distortion or ParadoxThreshold.Instability) ||
            !Triggered.Add(paradoxEvent.Threshold))
            return;

        var duration = paradoxEvent.Threshold switch
        {
            ParadoxThreshold.Disturbance => 4f,
            ParadoxThreshold.Distortion => 6f,
            ParadoxThreshold.Instability => 8f,
            _ => 0f
        };

        _activeThreshold = paradoxEvent.Threshold;
        _effectStartedAt = Time.time;
        _effectEndsAt = Time.time + duration;

        TryShowNotification(paradoxEvent);
        ApplyInitialCameraShake();

        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX runtime event started: {(int)paradoxEvent.Threshold}% for {duration:0.#}s.");
    }

    public static void TriggerCrossed(float previousMeter, float currentMeter)
    {
        if (currentMeter <= previousMeter)
            return;

        if (previousMeter < 25f && currentMeter >= 25f)
            Trigger(ParadoxEventCatalog.For(ParadoxThreshold.Disturbance));

        if (previousMeter < 50f && currentMeter >= 50f)
            Trigger(ParadoxEventCatalog.For(ParadoxThreshold.Distortion));

        if (previousMeter < 75f && currentMeter >= 75f)
            Trigger(ParadoxEventCatalog.For(ParadoxThreshold.Instability));
    }

    public static void Update(HudManager hud)
    {
        if (hud == null)
            return;

        if (_activeThreshold == ParadoxThreshold.None)
        {
            HideOverlay();
            return;
        }

        if (Time.time >= _effectEndsAt)
        {
            EndActiveEffect(hud);
            return;
        }

        if (MeetingHud.Instance != null)
        {
            HideOverlay();
            return;
        }

        EnsureOverlay(hud);
        UpdateOverlay();
        UpdateCameraShake(hud);
    }

    private static void TryShowNotification(ParadoxEvent paradoxEvent)
    {
        try
        {
            var hud = HudManager.Instance;
            if (hud == null || hud.Notifier == null)
                return;

            var title = ParadoxPlugin.Localizer.Get(paradoxEvent.LocalizationKey);
            var detailKey = paradoxEvent.Threshold switch
            {
                ParadoxThreshold.Disturbance => "event.25.detail",
                ParadoxThreshold.Distortion => "event.50.detail",
                ParadoxThreshold.Instability => "event.75.detail",
                _ => paradoxEvent.LocalizationKey
            };
            var detail = ParadoxPlugin.Localizer.Get(detailKey);

            hud.Notifier.AddDisconnectMessage(
                $"PARADOX {(int)paradoxEvent.Threshold}% — {title}\n{detail}");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX event notification failed: {e.Message}");
        }
    }

    private static void EnsureOverlay(HudManager hud)
    {
        if (_overlay != null)
            return;

        try
        {
            if (hud.FullScreen == null)
                return;

            _overlay = Object.Instantiate(
                hud.FullScreen,
                hud.FullScreen.transform.parent);

            _overlay.name = "PARADOX_EventOverlay";
            _overlay.gameObject.SetActive(true);
            _overlay.enabled = true;
            _overlay.color = new Color(0f, 0f, 0f, 0f);
            _overlay.sortingOrder = hud.FullScreen.sortingOrder + 2;
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX event overlay creation failed: {e.Message}");
            _overlay = null;
        }
    }

    private static void UpdateOverlay()
    {
        if (_overlay == null)
            return;

        var elapsed = Math.Max(0f, Time.time - _effectStartedAt);
        var pulse = (Mathf.Sin(elapsed * 11f) + 1f) * 0.5f;

        if (_activeThreshold == ParadoxThreshold.Disturbance)
        {
            var cyan = new Color(0.10f, 0.75f, 0.82f, 0.05f + pulse * 0.07f);
            var violet = new Color(0.55f, 0.12f, 0.75f, 0.04f + (1f - pulse) * 0.05f);
            _overlay.color = Color.Lerp(cyan, violet, pulse);
            return;
        }

        if (_activeThreshold == ParadoxThreshold.Distortion)
        {
            var red = new Color(0.95f, 0.08f, 0.18f, 0.10f + pulse * 0.10f);
            var cyanStrong = new Color(0.08f, 0.85f, 0.90f, 0.08f + (1f - pulse) * 0.08f);
            _overlay.color = Color.Lerp(red, cyanStrong, pulse);
            return;
        }

        var criticalPulse = (Mathf.Sin(elapsed * 16f) + 1f) * 0.5f;
        var deepViolet = new Color(0.45f, 0.02f, 0.62f, 0.20f + pulse * 0.14f);
        var blackout = new Color(0.01f, 0.00f, 0.03f, 0.28f + criticalPulse * 0.22f);
        _overlay.color = Color.Lerp(deepViolet, blackout, criticalPulse);
    }

    private static void ApplyInitialCameraShake()
    {
        try
        {
            var hud = HudManager.Instance;
            if (hud == null || hud.PlayerCam == null)
                return;

            if (!_shakeCaptured)
            {
                _previousShakeAmount = hud.PlayerCam.shakeAmount;
                _previousShakePeriod = hud.PlayerCam.shakePeriod;
                _shakeCaptured = true;
            }

            UpdateCameraShake(hud);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX event camera shake setup failed: {e.Message}");
        }
    }

    private static void UpdateCameraShake(HudManager hud)
    {
        if (hud.PlayerCam == null)
            return;

        var amount = _activeThreshold switch
        {
            ParadoxThreshold.Instability => 0.055f,
            ParadoxThreshold.Distortion => 0.035f,
            _ => 0.018f
        };

        hud.PlayerCam.shakeAmount = Math.Max(_previousShakeAmount, amount);
        hud.PlayerCam.shakePeriod = Math.Max(_previousShakePeriod, 16f);
    }

    private static void EndActiveEffect(HudManager hud)
    {
        HideOverlay();

        if (_shakeCaptured && hud.PlayerCam != null)
        {
            hud.PlayerCam.shakeAmount = _previousShakeAmount;
            hud.PlayerCam.shakePeriod = _previousShakePeriod;
        }

        _shakeCaptured = false;
        _activeThreshold = ParadoxThreshold.None;
        _effectStartedAt = 0f;
        _effectEndsAt = 0f;
    }

    private static void HideOverlay()
    {
        if (_overlay != null)
            _overlay.color = new Color(0f, 0f, 0f, 0f);
    }

    public static void Reset()
    {
        try
        {
            if (_overlay != null)
                Object.Destroy(_overlay.gameObject);
        }
        catch { }

        try
        {
            var hud = HudManager.Instance;
            if (_shakeCaptured && hud != null && hud.PlayerCam != null)
            {
                hud.PlayerCam.shakeAmount = _previousShakeAmount;
                hud.PlayerCam.shakePeriod = _previousShakePeriod;
            }
        }
        catch { }

        _overlay = null;
        _shakeCaptured = false;
        _activeThreshold = ParadoxThreshold.None;
        _effectStartedAt = 0f;
        _effectEndsAt = 0f;
        Triggered.Clear();
    }
}

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ParadoxEventHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance) =>
        ParadoxEventRuntime.Update(__instance);
}
