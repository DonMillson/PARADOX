using HarmonyLib;
using Paradox.Networking;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// PARADOX role/chance summary shown directly in the lobby for every PARADOX client.
/// Anchored to the HUD top-left so the 2026 lobby layout cannot push or clip it off-screen.
/// </summary>
[HarmonyPatch]
public static class ParadoxLobbyRoleSummaryPatch
{
    private static GameObject? _root;
    private static TextMeshPro? _roleSummary;
    private static PassiveButton? _roleSummaryButton;
    private static BoxCollider2D? _roleSummaryCollider;
    private static AspectPosition? _anchor;
    private static int _pageIndex;
    private static float _nextRefresh;
    private static bool _buildFailed;

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameStartManager __instance)
    {
        DestroySummary();
        _buildFailed = false;
        _pageIndex = 0;
        _nextRefresh = 0f;

        try
        {
            Build(__instance);

            if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
                ParadoxNetwork.BroadcastRoleSettings();
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX could not create lobby role summary: {e}");
            DestroySummary();
            _buildFailed = true;
        }
    }

    [HarmonyPatch(typeof(GameStartManager), nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix(GameStartManager __instance)
    {
        if (AmongUsClient.Instance == null)
        {
            DestroySummary();
            return;
        }

        if (_roleSummary == null && !_buildFailed)
        {
            try { Build(__instance); }
            catch (Exception e)
            {
                ParadoxPlugin.Instance.Log.LogWarning(
                    $"PARADOX lobby role summary late build failed: {e.Message}");
                _buildFailed = true;
            }
        }

        if (_roleSummary == null || Time.realtimeSinceStartup < _nextRefresh)
            return;

        _nextRefresh = Time.realtimeSinceStartup + 0.35f;

        try
        {
            ParadoxFontSupport.ApplyTo(_roleSummary);
            Refresh();
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX lobby role summary refresh failed: {e.Message}");
        }
    }

    private static void Build(GameStartManager instance)
    {
        if (_roleSummary != null || HudManager.Instance == null)
            return;

        TextMeshPro? template = instance.GameRoomNameCode;
        if (template == null || template.font == null)
            template = instance.PlayerCounter;
        if (template == null || template.font == null)
            template = instance.GameStartText;
        if (template == null)
            return;

        var root = new GameObject("PARADOX_LobbyRoleSummaryRoot");
        root.transform.SetParent(HudManager.Instance.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        _root = root;

        // Use Among Us' own aspect-aware anchoring instead of guessing local
        // coordinates from GameStartManager. This keeps the panel on-screen at
        // 16:9, ultrawide and other supported resolutions.
        try
        {
            _anchor = root.AddComponent<AspectPosition>();
            _anchor.Alignment = AspectPosition.EdgeAlignments.LeftTop;
            _anchor.DistanceFromEdge = new Vector3(0.55f, 1.55f, -20f);
            _anchor.updateAlways = true;
            _anchor.AdjustPosition();
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX role summary AspectPosition failed: {e.Message}");

            var cam = Camera.main;
            if (cam != null)
                root.transform.position = cam.ViewportToWorldPoint(new Vector3(0.035f, 0.82f, 10f));
        }

        var go = Object.Instantiate(template.gameObject, root.transform);
        go.name = "PARADOX_LobbyRoleSummary";

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

        _roleSummary = go.GetComponent<TextMeshPro>();
        if (_roleSummary == null)
        {
            DestroySummary();
            _buildFailed = true;
            return;
        }

        go.SetActive(true);
        go.transform.localPosition = Vector3.zero;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        _roleSummary.enabled = true;
        _roleSummary.text = string.Empty;
        _roleSummary.fontSize = 1.48f;
        _roleSummary.alignment = TextAlignmentOptions.TopLeft;
        _roleSummary.autoSizeTextContainer = false;
        _roleSummary.richText = true;
        _roleSummary.enableWordWrapping = false;
        _roleSummary.overflowMode = TextOverflowModes.Overflow;
        _roleSummary.color = Color.white;
        _roleSummary.outlineWidth = 0.16f;
        _roleSummary.outlineColor = Color.black;

        var rt = _roleSummary.rectTransform;
        if (rt != null)
        {
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(4.35f, 3.15f);
        }

        if (instance.StartButton != null)
            go.layer = instance.StartButton.gameObject.layer;

        _roleSummaryCollider = go.AddComponent<BoxCollider2D>();
        _roleSummaryCollider.size = new Vector2(4.35f, 3.15f);
        _roleSummaryCollider.offset = new Vector2(2.175f, -1.575f);
        _roleSummaryCollider.isTrigger = true;

        _roleSummaryButton = go.AddComponent<PassiveButton>();
        _roleSummaryButton.ClickMask = _roleSummaryCollider;
        _roleSummaryButton.Colliders = new Collider2D[] { _roleSummaryCollider };
        _roleSummaryButton.OnClick = new();
        _roleSummaryButton.OnMouseOver = new();
        _roleSummaryButton.OnMouseOut = new();
        _roleSummaryButton.enabled = true;
        _roleSummaryButton.SetButtonEnableState(true);
        _roleSummaryButton.OnClick.AddListener((Action)NextPage);

        ParadoxFontSupport.ApplyTo(_roleSummary);
        Refresh();

        ParadoxPlugin.Instance.Log.LogInfo(
            "PARADOX lobby role summary built and anchored to HUD top-left.");
    }

    private static void NextPage()
    {
        _pageIndex = (_pageIndex + 1) % 3;
        Refresh();
    }

    private static void Refresh()
    {
        if (_roleSummary == null)
            return;

        var polish = ParadoxPlugin.Localizer.CurrentLanguage == Localization.Language.Polish;
        var faction = _pageIndex switch
        {
            0 => RoleFaction.Impostor,
            1 => RoleFaction.Crewmate,
            _ => RoleFaction.Neutral
        };

        var color = faction switch
        {
            RoleFaction.Impostor => "#FF5A5A",
            RoleFaction.Crewmate => "#55CFE8",
            _ => "#E7C64B"
        };

        var factionName = faction switch
        {
            RoleFaction.Impostor => polish ? "IMPOSTORZY" : "IMPOSTORS",
            RoleFaction.Crewmate => polish ? "ZAŁOGA" : "CREWMATES",
            _ => polish ? "NEUTRALNI" : "NEUTRALS"
        };

        var lines = new List<string>
        {
            "<color=#55D9D2><b>PARADOX — ROLE</b></color>",
            $"<color={color}><b>{factionName}</b></color>   <color=#AAAAAA>{_pageIndex + 1}/3  ></color>"
        };

        var roles = RoleRegistry.All
            .Where(definition =>
                definition.Faction == faction &&
                ParadoxRoleSettings.IsImplemented(definition.Id) &&
                ParadoxRoleSettings.IsEnabled(definition.Id))
            .ToArray();

        if (roles.Length == 0)
        {
            lines.Add(polish
                ? "<color=#AAAAAA>Brak aktywnych ról</color>"
                : "<color=#AAAAAA>No active roles</color>");
        }
        else
        {
            foreach (var definition in roles)
            {
                var name = ParadoxPlugin.Localizer.Get(definition.NameKey);
                var chance = ParadoxRoleSettings.GetSpawnChance(definition.Id);
                lines.Add($"<color={color}>•</color> {name}  <b>{chance}%</b>");
            }
        }

        lines.Add(polish
            ? "\n<color=#888888>Kliknij listę, aby zmienić kategorię</color>"
            : "\n<color=#888888>Click the list to change category</color>");

        _roleSummary.text = string.Join("\n", lines);
    }

    private static void DestroySummary()
    {
        if (_root != null)
            Object.Destroy(_root);

        _root = null;
        _roleSummary = null;
        _roleSummaryButton = null;
        _roleSummaryCollider = null;
        _anchor = null;
        _pageIndex = 0;
        _nextRefresh = 0f;
    }
}
