using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.Roles.Swapper;

/// <summary>
/// Two-click selector in the native meeting voting grid. The icon is generated
/// locally and does not need an external asset bundle. The native vote buttons
/// and voter choices remain independent and are never replaced.
/// </summary>
[HarmonyPatch]
public static class SwapperMeetingSelectionPatch
{
    private static readonly List<(byte Id, SpriteRenderer Renderer)> Markers = new();
    private static byte? _first;
    private static byte? _second;
    private static Sprite? _swapIcon;

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(MeetingHud __instance)
    {
        Markers.Clear();
        _first = null;
        _second = null;

        var local = PlayerControl.LocalPlayer;
        if (local == null || !SwapperRole.IsSwapper(local.PlayerId) ||
            local.Data == null || local.Data.IsDead ||
            local.Data.Disconnected)
            return;

        // The vanilla CancelButton has a compatible collider/click mask.
        // Cloning it avoids introducing a full-screen UI blocker.
        try
        {
            foreach (var pva in __instance.playerStates)
            {
                if (pva == null || pva.PlayerId == local.PlayerId ||
                    pva.AmDead || SwapperRole.FindPlayer(pva.PlayerId)?.Data == null)
                    continue;

                var template = pva.Buttons?.transform.Find("CancelButton");
                if (template == null)
                    continue;

                var target = Object.Instantiate(template.gameObject, pva.transform);
                target.name = "PARADOX_SwapperSelect";
                target.transform.localPosition = new Vector3(-0.35f, 0.03f, -1.32f);
                target.transform.localScale = template.localScale;
                target.SetActive(true);

                var renderer = target.GetComponent<SpriteRenderer>();
                if (renderer == null)
                {
                    Object.Destroy(target);
                    continue;
                }

                renderer.sprite = Icon;
                renderer.color = new Color32(92, 205, 236, 255);
                var button = target.GetComponent<PassiveButton>();
                if (button == null)
                {
                    Object.Destroy(target);
                    continue;
                }

                var id = pva.PlayerId;
                button.OnClick = new();
                button.OnClick.AddListener((Action)(() => Pick(id)));
                button.SetButtonEnableState(true);
                Markers.Add((id, renderer));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX Swapper voting selector unavailable: {e.Message}");
        }
    }

    private static void Pick(byte id)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || local.Data == null || local.Data.IsDead ||
            !SwapperRole.IsSwapper(local.PlayerId) ||
            MeetingHud.Instance == null || id == local.PlayerId || _second.HasValue)
            return;

        if (!_first.HasValue)
        {
            _first = id;
            Refresh();
            return;
        }

        if (_first.Value == id)
        {
            _first = null;
            Refresh();
            return;
        }

        _second = id;
        Refresh();
        if (AmongUsClient.Instance == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (!SwapperRole.TrySetSwap(local, _first.Value, id))
            {
                SwapperRole.Notify(local.PlayerId, "role.Swapper.invalid");
                _second = null;
                Refresh();
            }
        }
        else
        {
            Rpc<SwapperMeetingRpc>.Instance.Send(local,
                new SwapperMeetingRpc.Data(0, local.PlayerId, _first.Value, id),
                immediately: true);
        }
    }

    private static void Refresh()
    {
        foreach (var (id, renderer) in Markers)
        {
            if (renderer == null)
                continue;
            renderer.color = id == _first || id == _second
                ? new Color32(188, 139, 250, 255)
                : new Color32(92, 205, 236, 255);
        }
    }

    [HarmonyPatch(typeof(MeetingHud), nameof(MeetingHud.Close))]
    [HarmonyPostfix]
    public static void ClosePostfix()
    {
        Markers.Clear();
        _first = null;
        _second = null;
    }

    private static Sprite Icon
    {
        get
        {
            if (_swapIcon != null)
                return _swapIcon;

            const int n = 64;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false)
            {
                name = "PARADOX_SwapperArrows",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            var transparent = new Color32(0, 0, 0, 0);
            var luminous = new Color32(248, 249, 255, 255);
            var pixels = new Color32[n * n];
            for (var y = 0; y < n; y++)
            for (var x = 0; x < n; x++)
            {
                var topLine = y >= 36 && y <= 40 && x >= 12 && x <= 51;
                var bottomLine = y >= 23 && y <= 27 && x >= 12 && x <= 51;
                // Right arrow at the upper path.
                var rightArrow = x >= 43 && x <= 56 &&
                                 Mathf.Abs(y - 38) <= (x - 42);
                // Left arrow at the lower path.
                var leftArrow = x >= 8 && x <= 21 &&
                                Mathf.Abs(y - 25) <= (22 - x);
                pixels[y * n + x] = topLine || bottomLine ||
                                    rightArrow || leftArrow
                    ? luminous : transparent;
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            _swapIcon = Sprite.Create(tex, new Rect(0, 0, n, n),
                new Vector2(0.5f, 0.5f), n);
            return _swapIcon;
        }
    }
}
