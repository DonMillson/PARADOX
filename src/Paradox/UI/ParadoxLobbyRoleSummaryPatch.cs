using HarmonyLib;
using Paradox.Networking;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Compact PARADOX role card shown in the lobby for every PARADOX client.
/// The card is intentionally small and styled like a native sci-fi HUD widget.
/// </summary>
[HarmonyPatch]
public static class ParadoxLobbyRoleSummaryPatch
{
    private static GameObject? _root;
    private static TextMeshPro? _roleSummary;
    private static PassiveButton? _roleSummaryButton;
    private static BoxCollider2D? _roleSummaryCollider;
    private static AspectPosition? _anchor;
    private static SpriteRenderer? _frame;
    private static SpriteRenderer? _panel;
    private static SpriteRenderer? _accent;
    private static Sprite? _solidSprite;
    private static TextMeshPro? _stationButtonLabel;
    private static PassiveButton? _stationButton;
    private static GameObject? _stationButtonBackground;
    // Fixed-height card: paginate instead of rendering 15 roles outside its border.
    private const int RolesPerPage = 6;
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
                $"PARADOX could not create lobby role card: {e}");
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
                    $"PARADOX lobby role card late build failed: {e.Message}");
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
                $"PARADOX lobby role card refresh failed: {e.Message}");
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

        var root = new GameObject("PARADOX_LobbyRoleCardRoot");
        root.transform.SetParent(HudManager.Instance.transform, false);
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;
        _root = root;

        try
        {
            _anchor = root.AddComponent<AspectPosition>();
            _anchor.Alignment = AspectPosition.EdgeAlignments.LeftTop;
            _anchor.DistanceFromEdge = new Vector3(0.34f, 1.18f, -20f);
            _anchor.updateAlways = true;
            _anchor.AdjustPosition();
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX role card AspectPosition failed: {e.Message}");

            var cam = Camera.main;
            if (cam != null)
                root.transform.position =
                    cam.ViewportToWorldPoint(new Vector3(0.03f, 0.79f, 10f));
        }

        CreateCardBackground(root.transform);

        var go = Object.Instantiate(template.gameObject, root.transform);
        go.name = "PARADOX_LobbyRoleCardText";

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
        go.transform.localPosition = new Vector3(0.13f, -0.10f, -0.15f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        _roleSummary.enabled = true;
        _roleSummary.text = string.Empty;
        _roleSummary.fontSize = 0.46f;
        _roleSummary.lineSpacing = -6f;
        _roleSummary.alignment = TextAlignmentOptions.TopLeft;
        _roleSummary.autoSizeTextContainer = false;
        _roleSummary.richText = true;
        _roleSummary.enableWordWrapping = false;
        _roleSummary.overflowMode = TextOverflowModes.Overflow;
        _roleSummary.color = Color.white;
        _roleSummary.outlineWidth = 0.10f;
        _roleSummary.outlineColor = new Color32(2, 8, 12, 230);
        _roleSummary.renderer.sortingOrder = 25;

        var rt = _roleSummary.rectTransform;
        if (rt != null)
        {
            rt.pivot = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(1.72f, 0.92f);
        }

        if (instance.StartButton != null)
            go.layer = instance.StartButton.gameObject.layer;

        _roleSummaryCollider = go.AddComponent<BoxCollider2D>();
        _roleSummaryCollider.size = new Vector2(1.92f, 0.90f);
        _roleSummaryCollider.offset = new Vector2(0.83f, -0.35f);
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
        _roleSummaryButton.OnMouseOver.AddListener((Action)(() => SetHover(true)));
        _roleSummaryButton.OnMouseOut.AddListener((Action)(() => SetHover(false)));

        ParadoxFontSupport.ApplyTo(_roleSummary);
        CreateSoloStationButton(template, root.transform);
        Refresh();

        ParadoxPlugin.Instance.Log.LogInfo(
            "PARADOX compact lobby role card built.");
    }

    private static void CreateSoloStationButton(TextMeshPro template, Transform parent)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return;

        var background = new GameObject("PARADOX_SoloMapTestButtonBackground");
        background.transform.SetParent(parent, false);
        background.transform.localPosition = new Vector3(0.96f, -1.14f, 0.06f);
        background.transform.localScale = new Vector3(1.92f, 0.34f, 1f);
        var panel = background.AddComponent<SpriteRenderer>();
        panel.sprite = GetSolidSprite();
        panel.color = new Color32(28, 109, 122, 235);
        panel.sortingOrder = 24;
        _stationButtonBackground = background;

        var go = Object.Instantiate(template.gameObject, parent);
        go.name = "PARADOX_SoloMapTestButton";
        foreach (var component in go.GetComponents<Component>())
        {
            if (component == null)
                continue;
            if (component.TryCast<TextTranslatorTMP>() != null ||
                component.TryCast<AspectPosition>() != null ||
                component.TryCast<PassiveButton>() != null ||
                component.TryCast<Collider2D>() != null)
                Object.DestroyImmediate(component);
        }

        for (var i = go.transform.childCount - 1; i >= 0; i--)
            Object.Destroy(go.transform.GetChild(i).gameObject);

        go.SetActive(true);
        go.transform.localPosition = new Vector3(0.96f, -1.14f, -0.12f);
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        _stationButtonLabel = go.GetComponent<TextMeshPro>();
        if (_stationButtonLabel == null)
            throw new InvalidOperationException("PARADOX station test button font not available.");

        _stationButtonLabel.enabled = true;
        _stationButtonLabel.fontSize = 0.4f;
        _stationButtonLabel.alignment = TextAlignmentOptions.Center;
        _stationButtonLabel.color = Color.white;
        _stationButtonLabel.richText = false;
        _stationButtonLabel.enableWordWrapping = false;
        _stationButtonLabel.overflowMode = TextOverflowModes.Overflow;
        _stationButtonLabel.renderer.sortingOrder = 26;
        _stationButtonLabel.rectTransform.sizeDelta = new Vector2(3.1f, 0.56f);

        var collider = go.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1.9f, 0.34f);
        collider.isTrigger = true;

        _stationButton = go.AddComponent<PassiveButton>();
        _stationButton.ClickMask = collider;
        _stationButton.Colliders = new Collider2D[] { collider };
        _stationButton.OnClick = new();
        _stationButton.OnMouseOver = new();
        _stationButton.OnMouseOut = new();
        _stationButton.enabled = true;
        _stationButton.SetButtonEnableState(true);
        _stationButton.OnClick.AddListener((Action)(() =>
            Paradox.Maps.ParadoxStationRuntime.TryToggleHost()));

        ParadoxFontSupport.ApplyTo(_stationButtonLabel);
        ParadoxPlugin.Instance.Log.LogInfo(
            "PARADOX: solo station test lobby button created.");
    }

    private static void CreateCardBackground(Transform parent)
    {
        var sprite = GetSolidSprite();
        if (sprite == null)
            return;

        var frameGo = new GameObject("PARADOX_RoleCardFrame");
        frameGo.transform.SetParent(parent, false);
        frameGo.transform.localPosition = new Vector3(0.96f, -0.47f, 0.10f);
        frameGo.transform.localScale = new Vector3(1.92f, 0.94f, 1f);
        _frame = frameGo.AddComponent<SpriteRenderer>();
        _frame.sprite = sprite;
        _frame.color = new Color32(70, 215, 210, 150);
        _frame.sortingOrder = 20;

        var panelGo = new GameObject("PARADOX_RoleCardPanel");
        panelGo.transform.SetParent(parent, false);
        panelGo.transform.localPosition = new Vector3(0.96f, -0.47f, 0.08f);
        panelGo.transform.localScale = new Vector3(1.86f, 0.88f, 1f);
        _panel = panelGo.AddComponent<SpriteRenderer>();
        _panel.sprite = sprite;
        _panel.color = new Color32(7, 15, 22, 220);
        _panel.sortingOrder = 21;

        var accentGo = new GameObject("PARADOX_RoleCardAccent");
        accentGo.transform.SetParent(parent, false);
        accentGo.transform.localPosition = new Vector3(0.055f, -0.47f, 0.05f);
        accentGo.transform.localScale = new Vector3(0.035f, 0.76f, 1f);
        _accent = accentGo.AddComponent<SpriteRenderer>();
        _accent.sprite = sprite;
        _accent.color = new Color32(255, 90, 90, 255);
        _accent.sortingOrder = 22;
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
                $"PARADOX solid UI sprite creation failed: {e.Message}");
        }

        return _solidSprite;
    }

    private static void SetHover(bool hover)
    {
        if (_frame != null)
            _frame.color = hover
                ? new Color32(85, 240, 235, 220)
                : new Color32(70, 215, 210, 150);
    }

    private static void NextPage()
    {
        var total = PageCount(RoleFaction.Impostor) +
                    PageCount(RoleFaction.Crewmate) +
                    PageCount(RoleFaction.Neutral);
        _pageIndex = (_pageIndex + 1) % total;
        Refresh();
    }

    private static int PageCount(RoleFaction faction)
    {
        var count = RoleRegistry.All.Count(definition =>
            definition.Faction == faction &&
            ParadoxRoleSettings.IsImplemented(definition.Id) &&
            ParadoxRoleSettings.IsEnabled(definition.Id));
        return Math.Max(1, (count + RolesPerPage - 1) / RolesPerPage);
    }

    private static RoleFaction CurrentPage(out int factionPage, out int pagesInFaction)
    {
        var impostorPages = PageCount(RoleFaction.Impostor);
        var crewPages = PageCount(RoleFaction.Crewmate);
        var neutralPages = PageCount(RoleFaction.Neutral);
        var total = impostorPages + crewPages + neutralPages;
        _pageIndex %= total;

        if (_pageIndex < impostorPages)
        {
            factionPage = _pageIndex;
            pagesInFaction = impostorPages;
            return RoleFaction.Impostor;
        }

        if (_pageIndex < impostorPages + crewPages)
        {
            factionPage = _pageIndex - impostorPages;
            pagesInFaction = crewPages;
            return RoleFaction.Crewmate;
        }

        factionPage = _pageIndex - impostorPages - crewPages;
        pagesInFaction = neutralPages;
        return RoleFaction.Neutral;
    }

    private static void Refresh()
    {
        if (_roleSummary == null)
            return;

        var polish = ParadoxPlugin.Localizer.CurrentLanguage == Localization.Language.Polish;
        var faction = CurrentPage(out var factionPage, out var pagesInFaction);

        var color = faction switch
        {
            RoleFaction.Impostor => "#FF5A5A",
            RoleFaction.Crewmate => "#55CFE8",
            _ => "#E7C64B"
        };

        if (_accent != null)
        {
            _accent.color = faction switch
            {
                RoleFaction.Impostor => new Color32(255, 90, 90, 255),
                RoleFaction.Crewmate => new Color32(85, 207, 232, 255),
                _ => new Color32(231, 198, 75, 255)
            };
        }

        var factionName = faction switch
        {
            RoleFaction.Impostor => polish ? "IMPOSTORZY" : "IMPOSTORS",
            RoleFaction.Crewmate => polish ? "ZAŁOGA" : "CREWMATES",
            _ => polish ? "NEUTRALNI" : "NEUTRALS"
        };

        var lines = new List<string>
        {
            "<size=59%><color=#59D4D2><b>PARADOX</b></color>  <color=#BDCBD4>BY DONMILLSON</color></size>",
            $"<size=74%><color={color}><b>{factionName}</b></color>  <color=#8C99A4>{factionPage + 1}/{pagesInFaction}  ></color></size>"
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
                ? "<size=66%><color=#89949D>Brak aktywnych ról</color></size>"
                : "<size=66%><color=#89949D>No active roles</color></size>");
        }
        else
        {
            // Six visible roles per page leave room for a title and category line.
            foreach (var definition in roles
                .Skip(factionPage * RolesPerPage)
                .Take(RolesPerPage))
            {
                var name = ParadoxPlugin.Localizer.Get(definition.NameKey);
                var chance = ParadoxRoleSettings.GetSpawnChance(definition.Id);
                lines.Add(
                    $"<size=68%><color={color}>■</color> " +
                    $"<color=#F2F5F7>{name}</color>  " +
                    $"<color=#C9D2D9><b>{chance}%</b></color></size>");
            }
        }

        // Click the card to advance through faction pages. Never render a
        // full 15-role faction into a short lobby widget.

        _roleSummary.text = string.Join("\n", lines);

        if (_stationButtonLabel != null)
        {
            _stationButtonLabel.text = Paradox.Maps.ParadoxStationRuntime.Active
                ? polish ? "WRÓĆ DO LOBBY" : "RETURN TO LOBBY"
                : polish ? "TESTUJ MAPĘ  >" : "TEST STATION  >";
            ParadoxFontSupport.ApplyTo(_stationButtonLabel);
        }
    }

    private static void DestroySummary()
    {
        if (_root != null)
            Object.Destroy(_root);

        _root = null;
        _roleSummary = null;
        _stationButtonLabel = null;
        _stationButton = null;
        _stationButtonBackground = null;
        _roleSummaryButton = null;
        _roleSummaryCollider = null;
        _anchor = null;
        _frame = null;
        _panel = null;
        _accent = null;
        _pageIndex = 0;
        _nextRefresh = 0f;
    }
}
