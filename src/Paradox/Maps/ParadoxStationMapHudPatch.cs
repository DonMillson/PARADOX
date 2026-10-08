using HarmonyLib;
using Paradox.Localization;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.Maps;

/// <summary>
/// In-game, asset-free PARADOX STATION blueprint viewer (F6).
/// This deliberately does not replace ShipStatus or pretend to implement
/// colliders, tasks, vent travel, spawning, or match-map selection.
/// </summary>
[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ParadoxStationMapHudPatch
{
    private static GameObject? _root;
    private static Sprite? _white;
    private static TextMeshPro? _fontTemplate;
    private const float XStep = 0.76f;
    private const float YStep = 0.73f;

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        if (Input.GetKeyDown(KeyCode.F6))
        {
            if (_root != null)
            {
                Close();
                return;
            }

            try
            {
                Build(__instance);
            }
            catch (Exception e)
            {
                Close();
                ParadoxPlugin.Instance.Log.LogWarning(
                    $"PARADOX STATION blueprint viewer failed: {e}");
            }
        }

        if (_root != null)
        {
            // Meetings hide the viewer without destroying its state.
            var shouldShow = AmongUsClient.Instance != null &&
                             MeetingHud.Instance == null;
            if (_root.activeSelf != shouldShow)
                _root.SetActive(shouldShow);
        }
    }

    public static void Close()
    {
        if (_root != null)
            Object.Destroy(_root);
        _root = null;
    }

    private static void Build(HudManager hud)
    {
        if (!ParadoxStation.TryValidateDesign(out var reason))
            throw new InvalidOperationException($"Station layout invalid: {reason}");

        var root = new GameObject("PARADOX_StationBlueprint");
        root.transform.SetParent(hud.transform, false);
        root.transform.localPosition = new Vector3(0f, 0f, -22f);
        root.transform.localScale = Vector3.one;
        _root = root;

        _fontTemplate = Object.FindObjectOfType<TextMeshPro>();
        var white = SolidSprite();
        if (white == null)
            throw new InvalidOperationException("Could not create map UI sprite.");

        Panel(root.transform, "OuterFrame", 0f, 0f, 8.65f, 6.55f,
            new Color32(61, 213, 218, 240), 200);
        Panel(root.transform, "Background", 0f, 0f, 8.59f, 6.49f,
            new Color32(6, 16, 27, 246), 201);

        var pl = ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish;
        Write(root.transform,
            pl ? "STACJA PARADOKS  /  PROJEKT MAPY" : "PARADOX STATION  /  MAP BLUEPRINT",
            0f, 2.91f, 0.27f, 220, new Color32(111, 241, 239, 255));
        Write(root.transform,
            pl ? "F6: zamknij  |  Pomieszczenia i przejścia  |  Zadania w przygotowaniu"
               : "F6: close  |  Room and passage layout  |  Tasks not yet playable",
            0f, -2.92f, 0.19f, 220, new Color32(195, 211, 225, 255));

        // Draw corridor links behind the room cards.
        foreach (var passage in ParadoxStation.Corridors)
        {
            var from = GetPoint(passage.FromRoomId);
            var to = GetPoint(passage.ToRoomId);
            Segment(root.transform, $"Corridor_{passage.FromRoomId}_{passage.ToRoomId}",
                from, to, 0.11f, new Color32(78, 136, 163, 245), 203);
            Segment(root.transform, $"CorridorLight_{passage.FromRoomId}_{passage.ToRoomId}",
                from, to, 0.025f, new Color32(154, 219, 234, 235), 204);
        }

        // Orange dashed links describe *planned* vents, not active vent travel.
        foreach (var passage in ParadoxStation.Vents)
        {
            var from = GetPoint(passage.FromRoomId);
            var to = GetPoint(passage.ToRoomId);
            for (var i = 1; i < 9; i++)
            {
                var point = Vector2.Lerp(from, to, i / 9f);
                Panel(root.transform,
                    $"Vent_{passage.FromRoomId}_{passage.ToRoomId}_{i}",
                    point.x, point.y, 0.055f, 0.055f,
                    new Color32(243, 161, 74, 255), 206);
            }
        }

        foreach (var room in ParadoxStation.Rooms)
        {
            var position = Point(room);
            Panel(root.transform, $"RoomEdge_{room.Id}",
                position.x, position.y, 1.02f, 0.66f,
                room.IsSpawn
                    ? new Color32(57, 220, 154, 255)
                    : new Color32(74, 152, 187, 255), 210);
            Panel(root.transform, $"RoomFloor_{room.Id}",
                position.x, position.y, 0.98f, 0.62f,
                room.IsSpawn
                    ? new Color32(12, 61, 58, 255)
                    : new Color32(16, 40, 59, 255), 211);
            var label = ParadoxPlugin.Localizer.Get(
                "map.paradoxStation.room." + room.Id);
            Write(root.transform, label,
                position.x, position.y + 0.02f, 0.145f, 221, Color.white);
            Panel(root.transform, $"TaskMarker_{room.TaskId}",
                position.x + 0.42f, position.y - 0.24f,
                0.085f, 0.085f, new Color32(247, 200, 92, 255), 215);
        }

        Write(root.transform,
            pl ? "ZIELONY: START     POMARANCZOWY: WENTYLACJA     ZOLTY: PLAN ZADANIA"
               : "GREEN: SPAWN     ORANGE: VENT     YELLOW: PLANNED TASK",
            0f, -2.56f, 0.17f, 220,
            new Color32(191, 209, 228, 255));

        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX STATION blueprint viewer: {ParadoxStation.Rooms.Count} rooms, " +
            $"{ParadoxStation.Corridors.Count} corridors, {ParadoxStation.Vents.Count} vents.");
    }

    private static Vector2 GetPoint(string roomId)
    {
        var room = ParadoxStation.Rooms.First(room => room.Id == roomId);
        return Point(room);
    }

    private static Vector2 Point(ParadoxStation.Room room) =>
        new(room.GridX * XStep, room.GridY * YStep - 0.22f);

    private static void Segment(
        Transform parent, string name, Vector2 from, Vector2 to,
        float width, Color color, int order)
    {
        var midpoint = (from + to) * 0.5f;
        var delta = to - from;
        var obj = Panel(parent, name, midpoint.x, midpoint.y,
            delta.magnitude, width, color, order);
        obj.transform.localRotation = Quaternion.Euler(
            0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
    }

    private static GameObject Panel(
        Transform parent, string name, float x, float y,
        float width, float height, Color color, int order)
    {
        var go = new GameObject("PARADOX_" + name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, 0f);
        go.transform.localScale = new Vector3(width, height, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = SolidSprite();
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static void Write(
        Transform parent, string value, float x, float y,
        float scale, int order, Color color)
    {
        // TMP components are available in Among Us; using the active font
        // avoids external assets and lets PARADOX font support add Polish glyphs.
        var go = new GameObject("PARADOX_MapLabel");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(x, y, -0.01f);
        go.transform.localScale = Vector3.one * scale;
        var label = go.AddComponent<TextMeshPro>();
        if (_fontTemplate != null && _fontTemplate.font != null)
            label.font = _fontTemplate.font;
        label.text = value;
        label.fontSize = 1.6f;
        label.richText = false;
        label.alignment = TextAlignmentOptions.Center;
        label.enableWordWrapping = false;
        label.overflowMode = TextOverflowModes.Overflow;
        label.color = color;
        label.renderer.sortingOrder = order;
        label.rectTransform.sizeDelta = new Vector2(32f, 3f);
        Paradox.UI.ParadoxFontSupport.ApplyTo(label);
    }

    private static Sprite? SolidSprite()
    {
        if (_white != null)
            return _white;

        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();
        _white = Sprite.Create(texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f), 1f);
        return _white;
    }
}
