using HarmonyLib;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Compact role/chance summary shown directly in the hosted lobby.
/// It is intentionally independent from the lobby settings controls so it cannot
/// interfere with the + / - buttons.
/// </summary>
[HarmonyPatch(typeof(GameStartManager))]
public static class ParadoxLobbyRoleSummaryPatch
{
    private static TextMeshPro? _roleSummary;
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

            if (__instance.PlayerCounter == null)
                return;

            _roleSummary = Object.Instantiate(
                __instance.PlayerCounter,
                __instance.PlayerCounter.transform.parent);

            _roleSummary.name = "PARADOX_LobbyRoleSummary";
            _roleSummary.text = string.Empty;
            _roleSummary.fontSize = 1.55f;
            _roleSummary.alignment = TextAlignmentOptions.TopRight;
            _roleSummary.autoSizeTextContainer = false;
            _roleSummary.richText = true;
            _roleSummary.color = Color.white;
            _roleSummary.transform.localPosition = new Vector3(4.65f, 2.25f, -10f);
            _roleSummary.transform.localScale = Vector3.one;
            _roleSummary.rectTransform.sizeDelta = new Vector2(5.2f, 6.2f);

            ParadoxFontSupport.ApplyTo(_roleSummary);
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
            Refresh();
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"PARADOX lobby role summary refresh failed: {e.Message}");
        }
    }

    private static void Refresh()
    {
        if (_roleSummary == null)
            return;

        var polish = ParadoxPlugin.Localizer.CurrentLanguage == Localization.Language.Polish;
        var lines = new List<string>
        {
            "<color=#55D9D2><b>PARADOX</b></color>",
            polish ? "<b>ROLE W TEJ GRZE</b>" : "<b>ROLES THIS GAME</b>"
        };

        AppendFaction(lines, RoleFaction.Impostor, "#FF5A5A",
            polish ? "IMPOSTORZY" : "IMPOSTORS");
        AppendFaction(lines, RoleFaction.Crewmate, "#55CFE8",
            polish ? "ZAŁOGA" : "CREWMATES");
        AppendFaction(lines, RoleFaction.Neutral, "#E7C64B",
            polish ? "NEUTRALNI" : "NEUTRALS");

        if (lines.Count == 2)
            lines.Add(polish ? "<color=#AAAAAA>Brak aktywnych ról</color>" : "<color=#AAAAAA>No active roles</color>");

        _roleSummary.text = string.Join("\n", lines);
    }

    private static void AppendFaction(
        List<string> lines,
        RoleFaction faction,
        string color,
        string header)
    {
        var roles = RoleRegistry.All
            .Where(definition =>
                definition.Faction == faction &&
                ParadoxRoleSettings.IsImplemented(definition.Id) &&
                ParadoxRoleSettings.IsEnabled(definition.Id))
            .ToArray();

        if (roles.Length == 0)
            return;

        lines.Add($"\n<color={color}><b>{header}</b></color>");

        foreach (var definition in roles)
        {
            var name = ParadoxPlugin.Localizer.Get(definition.NameKey);
            var chance = ParadoxRoleSettings.GetSpawnChance(definition.Id);
            lines.Add($"<color={color}>•</color> {name}  <b>{chance}%</b>");
        }
    }

    private static void DestroySummary()
    {
        if (_roleSummary != null)
            Object.Destroy(_roleSummary.gameObject);

        _roleSummary = null;
        _nextRefresh = 0f;
    }
}
