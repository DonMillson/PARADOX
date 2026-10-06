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
/// One faction is shown at a time; clicking the panel cycles Impostor/Crewmate/Neutral.
/// Host settings are synchronized through SyncRoleSettingRpc.
/// </summary>
[HarmonyPatch(typeof(GameStartManager))]
public static class ParadoxLobbyRoleSummaryPatch
{
    private static TextMeshPro? _roleSummary;
    private static PassiveButton? _roleSummaryButton;
    private static BoxCollider2D? _roleSummaryCollider;
    private static int _pageIndex;
    private static float _nextRefresh;

    [HarmonyPatch(nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameStartManager __instance)
    {
        try
        {
            DestroySummary();

            if (AmongUsClient.Instance == null)
                return;

            TextMeshPro? template = __instance.GameStartText;
            if (template == null)
                template = __instance.PlayerCounter;

            if (template == null)
                return;

            _pageIndex = 0;

            // Parent to the GameStartManager root, not PlayerCounter's right-side
            // lobby-info panel. The 2026 UI clips children of that panel.
            _roleSummary = Object.Instantiate(template, __instance.transform);
            _roleSummary.name = "PARADOX_LobbyRoleSummary";
            _roleSummary.gameObject.SetActive(true);

            var translator = _roleSummary.GetComponent<TextTranslatorTMP>();
            if (translator != null)
                Object.DestroyImmediate(translator);

            _roleSummary.text = string.Empty;
            _roleSummary.fontSize = 1.7f;
            _roleSummary.alignment = TextAlignmentOptions.TopLeft;
            _roleSummary.autoSizeTextContainer = false;
            _roleSummary.richText = true;
            _roleSummary.color = Color.white;
            _roleSummary.transform.localScale = Vector3.one;
            _roleSummary.rectTransform.sizeDelta = new Vector2(5.3f, 4.2f);

            // Anchor beside the room, above the WAITING FOR PLAYERS area.
            // This stays inside the visible 16:9 lobby and clear of the 2026
            // right-side Room Settings panel.
            var anchor = __instance.GameStartText != null
                ? __instance.GameStartText.transform.localPosition
                : template.transform.localPosition;

            _roleSummary.transform.localPosition =
                anchor + new Vector3(-5.25f, 4.15f, -2f);

            if (__instance.StartButton != null)
                _roleSummary.gameObject.layer = __instance.StartButton.gameObject.layer;

            _roleSummaryCollider = _roleSummary.gameObject.AddComponent<BoxCollider2D>();
            _roleSummaryCollider.size = new Vector2(5.3f, 4.2f);
            _roleSummaryCollider.offset = new Vector2(2.65f, -2.1f);
            _roleSummaryCollider.isTrigger = true;

            _roleSummaryButton = _roleSummary.gameObject.AddComponent<PassiveButton>();
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

            // Host publishes the authoritative role pool/chances. Clients update
            // their local lobby panel when the RPCs arrive.
            if (AmongUsClient.Instance.AmHost)
                ParadoxNetwork.BroadcastRoleSettings();

            ParadoxPlugin.Instance.Log.LogInfo(
                "PARADOX lobby role summary created for local client.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX could not create lobby role summary: {e}");
            DestroySummary();
        }
    }

    [HarmonyPatch(nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix()
    {
        if (_roleSummary == null)
            return;

        if (AmongUsClient.Instance == null)
        {
            DestroySummary();
            return;
        }

        if (Time.realtimeSinceStartup < _nextRefresh)
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
        if (_roleSummary != null)
            Object.Destroy(_roleSummary.gameObject);

        _roleSummary = null;
        _roleSummaryButton = null;
        _roleSummaryCollider = null;
        _pageIndex = 0;
        _nextRefresh = 0f;
    }
}
