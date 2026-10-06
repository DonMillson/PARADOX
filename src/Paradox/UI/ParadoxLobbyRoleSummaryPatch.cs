using HarmonyLib;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Clickable PARADOX role/chance summary shown directly in the hosted lobby.
/// Only one faction is shown at a time so the lobby is not covered by one long list.
/// The panel is independent from the settings + / - controls.
/// </summary>
[HarmonyPatch(typeof(GameStartManager))]
public static class ParadoxLobbyRoleSummaryPatch
{
    private static TextMeshPro? _roleSummary;
    private static PassiveButton? _pageButton;
    private static int _pageIndex;
    private static float _nextRefresh;

    [HarmonyPatch(nameof(GameStartManager.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameStartManager __instance)
    {
        try
        {
            DestroySummary();

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            if (__instance.PlayerCounter == null || __instance.StartButton == null)
                return;

            _pageIndex = 0;

            _roleSummary = Object.Instantiate(
                __instance.PlayerCounter,
                __instance.PlayerCounter.transform.parent);

            _roleSummary.name = "PARADOX_LobbyRoleSummary";
            _roleSummary.text = string.Empty;
            _roleSummary.fontSize = 1.65f;
            _roleSummary.alignment = TextAlignmentOptions.TopRight;
            _roleSummary.autoSizeTextContainer = false;
            _roleSummary.richText = true;
            _roleSummary.color = Color.white;
            _roleSummary.transform.localPosition = new Vector3(4.65f, 2.25f, -10f);
            _roleSummary.transform.localScale = Vector3.one;
            _roleSummary.rectTransform.sizeDelta = new Vector2(5.2f, 4.25f);

            _pageButton = Object.Instantiate(
                __instance.StartButton,
                __instance.StartButton.transform.parent);

            _pageButton.name = "PARADOX_LobbyRolePageButton";
            _pageButton.transform.localPosition = new Vector3(4.15f, -2.15f, -5f);
            _pageButton.transform.localScale = Vector3.one * 0.55f;
            _pageButton.OnClick = new();
            _pageButton.OnClick.AddListener((Action)(NextPage));

            if (_pageButton.buttonText != null)
            {
                var translator = _pageButton.buttonText.GetComponent<TextTranslatorTMP>();
                if (translator != null)
                    Object.DestroyImmediate(translator);
            }

            ParadoxFontSupport.ApplyTo(_roleSummary);
            if (_pageButton.buttonText != null)
                ParadoxFontSupport.ApplyTo(_pageButton.buttonText);

            Refresh();
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX could not create lobby role summary: {e.Message}");
            DestroySummary();
        }
    }

    [HarmonyPatch(nameof(GameStartManager.Update))]
    [HarmonyPostfix]
    public static void UpdatePostfix()
    {
        if (_roleSummary == null)
            return;

        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
        {
            DestroySummary();
            return;
        }

        if (Time.realtimeSinceStartup < _nextRefresh)
            return;

        _nextRefresh = Time.realtimeSinceStartup + 0.5f;

        try
        {
            ParadoxFontSupport.ApplyTo(_roleSummary);
            if (_pageButton?.buttonText != null)
                ParadoxFontSupport.ApplyTo(_pageButton.buttonText);
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
            "<color=#55D9D2><b>PARADOX</b></color>",
            $"<color={color}><b>{factionName}</b></color>   <color=#AAAAAA>{_pageIndex + 1}/3</color>"
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
            ? "\n<color=#888888>Kliknij przycisk, aby zmienić kategorię</color>"
            : "\n<color=#888888>Click the button to change category</color>");

        _roleSummary.text = string.Join("\n", lines);

        if (_pageButton?.buttonText != null)
        {
            _pageButton.buttonText.text = polish
                ? $"ROLE  {_pageIndex + 1}/3  >"
                : $"ROLES  {_pageIndex + 1}/3  >";
        }
    }

    private static void DestroySummary()
    {
        if (_roleSummary != null)
            Object.Destroy(_roleSummary.gameObject);

        if (_pageButton != null)
            Object.Destroy(_pageButton.gameObject);

        _roleSummary = null;
        _pageButton = null;
        _pageIndex = 0;
        _nextRefresh = 0f;
    }
}
