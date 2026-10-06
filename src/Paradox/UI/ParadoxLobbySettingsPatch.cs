using HarmonyLib;
using Paradox.Localization;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;

namespace Paradox.UI;

/// <summary>
/// Adds a dedicated PARADOX tab to the host lobby settings screen.
/// Vanilla Roles remains untouched; PARADOX roles live in their own tab.
/// </summary>
[HarmonyPatch]
public static class ParadoxLobbySettingsPatch
{
    private const int RolesPerPage = 7;

    private static GameSettingMenu? _menu;
    private static PassiveButton? _tabButton;
    private static GameObject? _root;
    private static GameObject? _content;
    private static int _page;
    private static LobbyTab _tab = LobbyTab.Roles;

    private enum LobbyTab
    {
        Roles,
        Mechanics,
        Meter,
        Language
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Start))]
    [HarmonyPostfix]
    public static void GameSettingMenuStartPostfix(GameSettingMenu __instance)
    {
        try
        {
            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            Cleanup();
            _menu = __instance;

            var template = __instance.GameSettingsButton;
            if (template == null)
                return;

            var roleButton = __instance.RoleSettingsButton;
            var delta = new Vector3(0f, -0.62f, 0f);

            if (roleButton != null)
            {
                var measured = roleButton.transform.localPosition - template.transform.localPosition;
                if (measured.magnitude > 0.05f)
                    delta = measured;
            }

            var baseButton = roleButton ?? template;
            _tabButton = CloneButton(
                template,
                template.transform.parent,
                "PARADOX_LobbyTabButton",
                baseButton.transform.localPosition + delta,
                baseButton.transform.localScale,
                "PARADOX",
                ShowParadox);

            if (__instance.ControllerSelectable != null)
                __instance.ControllerSelectable.Add(_tabButton);

            _root = new GameObject("PARADOX_LobbySettingsRoot");
            _root.transform.SetParent(__instance.transform, false);
            _root.transform.localPosition = new Vector3(0.85f, 0f, -20f);
            _root.SetActive(false);

            ParadoxPlugin.Instance.Log.LogInfo("PARADOX lobby settings tab created.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogError($"Failed to create PARADOX lobby settings tab: {e}");
            Cleanup();
        }
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.ChangeTab))]
    [HarmonyPrefix]
    public static void GameSettingMenuChangeTabPrefix()
    {
        // Any vanilla tab click takes ownership of the panel again.
        if (_root != null && _root.activeSelf)
            HideParadox();
    }

    [HarmonyPatch(typeof(GameSettingMenu), nameof(GameSettingMenu.Close))]
    [HarmonyPrefix]
    public static void GameSettingMenuClosePrefix() => Cleanup();

    private static void ShowParadox()
    {
        if (_menu == null || _root == null)
            return;

        if (_menu.PresetsTab != null)
            _menu.PresetsTab.gameObject.SetActive(false);
        if (_menu.GameSettingsTab != null)
            _menu.GameSettingsTab.gameObject.SetActive(false);
        if (_menu.RoleSettingsTab != null)
            _menu.RoleSettingsTab.gameObject.SetActive(false);

        _menu.GamePresetsButton?.SelectButton(false);
        _menu.GameSettingsButton?.SelectButton(false);
        _menu.RoleSettingsButton?.SelectButton(false);
        _tabButton?.SelectButton(true);

        _root.SetActive(true);
        Render();
    }

    private static void HideParadox()
    {
        if (_root != null)
            _root.SetActive(false);

        _tabButton?.SelectButton(false);
    }

    private static void Cleanup()
    {
        if (_root != null)
            UnityEngine.Object.Destroy(_root);

        if (_tabButton != null)
            UnityEngine.Object.Destroy(_tabButton.gameObject);

        _menu = null;
        _root = null;
        _content = null;
        _tabButton = null;
    }

    private static void Render()
    {
        if (_root == null || _menu == null)
            return;

        if (_content != null)
            UnityEngine.Object.Destroy(_content);

        _content = new GameObject("PARADOX_LobbySettingsContent");
        _content.transform.SetParent(_root.transform, false);

        CreateText(new Vector3(0f, 2.15f, -2f),
            $"PARADOX v{ParadoxInfo.Version} — by DonMillson", 2.25f, TextAlignmentOptions.Center);

        CreateTopButton(-2.25f, "ROLES", LobbyTab.Roles);
        CreateTopButton(-0.75f, ParadoxPlugin.Localizer.Get("ui.menu.mechanics"), LobbyTab.Mechanics);
        CreateTopButton(0.75f, ParadoxPlugin.Localizer.Get("ui.menu.meter"), LobbyTab.Meter);
        CreateTopButton(2.25f, ParadoxPlugin.Localizer.Get("ui.menu.language"), LobbyTab.Language);

        switch (_tab)
        {
            case LobbyTab.Roles:
                RenderRoles();
                break;
            case LobbyTab.Mechanics:
                RenderMechanics();
                break;
            case LobbyTab.Meter:
                RenderMeter();
                break;
            case LobbyTab.Language:
                RenderLanguage();
                break;
        }
    }

    private static void CreateTopButton(float x, string label, LobbyTab tab)
    {
        CreateButton(
            new Vector3(x, 1.55f, -2f),
            new Vector3(0.58f, 0.58f, 1f),
            label,
            () =>
            {
                _tab = tab;
                Render();
            });
    }

    private static void RenderRoles()
    {
        var roles = RoleRegistry.All;
        var pages = Math.Max(1, (int)Math.Ceiling(roles.Count / (double)RolesPerPage));
        _page = Math.Clamp(_page, 0, pages - 1);

        var start = _page * RolesPerPage;
        var end = Math.Min(start + RolesPerPage, roles.Count);

        for (var i = start; i < end; i++)
        {
            var definition = roles[i];
            var role = definition.Id;
            var row = i - start;
            var y = 1.0f - row * 0.43f;
            var faction = definition.Faction switch
            {
                RoleFaction.Impostor => "IMP",
                RoleFaction.Crewmate => "CREW",
                _ => "NEUTRAL"
            };

            CreateText(
                new Vector3(-2.15f, y, -2f),
                $"[{faction}] {ParadoxPlugin.Localizer.Get(definition.NameKey)}",
                1.35f,
                TextAlignmentOptions.Left);

            if (!ParadoxRoleSettings.IsImplemented(role))
            {
                CreateText(
                    new Vector3(2.0f, y, -2f),
                    ParadoxPlugin.Localizer.Get("ui.menu.planned"),
                    1.2f,
                    TextAlignmentOptions.Center);
                continue;
            }

            var enabled = ParadoxRoleSettings.IsEnabled(role)
                ? ParadoxPlugin.Localizer.Get("ui.menu.enabled")
                : ParadoxPlugin.Localizer.Get("ui.menu.disabled");

            CreateButton(
                new Vector3(1.25f, y, -2f),
                new Vector3(0.30f, 0.30f, 1f),
                enabled,
                () =>
                {
                    ParadoxPlugin.Instance.ToggleRoleEnabled(role);
                    Render();
                });

            CreateButton(
                new Vector3(2.35f, y, -2f),
                new Vector3(0.34f, 0.30f, 1f),
                $"{ParadoxRoleSettings.GetSpawnChance(role)}%",
                () =>
                {
                    ParadoxPlugin.Instance.CycleRoleSpawnChance(role);
                    Render();
                });
        }

        CreateText(new Vector3(0f, -2.15f, -2f), $"{_page + 1}/{pages}", 1.25f, TextAlignmentOptions.Center);

        if (_page > 0)
        {
            CreateButton(new Vector3(-1.25f, -2.15f, -2f), new Vector3(0.42f, 0.34f, 1f),
                ParadoxPlugin.Localizer.Get("ui.menu.previous"), () =>
                {
                    _page--;
                    Render();
                });
        }

        if (_page < pages - 1)
        {
            CreateButton(new Vector3(1.25f, -2.15f, -2f), new Vector3(0.42f, 0.34f, 1f),
                ParadoxPlugin.Localizer.Get("ui.menu.next"), () =>
                {
                    _page++;
                    Render();
                });
        }
    }

    private static void RenderMechanics()
    {
        CreateText(new Vector3(0f, 0.75f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsTitle"), 2.0f, TextAlignmentOptions.Center);

        var lines = new[]
        {
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsHost"),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsSaved"),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsMore")
        };

        for (var i = 0; i < lines.Length; i++)
            CreateText(new Vector3(0f, 0.1f - i * 0.6f, -2f), lines[i], 1.35f, TextAlignmentOptions.Center);
    }

    private static void RenderMeter()
    {
        CreateText(new Vector3(0f, 0.8f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.meterTitle"), 2.0f, TextAlignmentOptions.Center);

        var lines = new[]
        {
            ParadoxPlugin.Localizer.Get("ui.menu.kill"),
            ParadoxPlugin.Localizer.Get("ui.menu.sabotage"),
            ParadoxPlugin.Localizer.Get("ui.menu.ability"),
            ParadoxPlugin.Localizer.Get("ui.menu.anomaly"),
            ParadoxPlugin.Localizer.Get("ui.menu.thresholds")
        };

        for (var i = 0; i < lines.Length; i++)
            CreateText(new Vector3(0f, 0.2f - i * 0.47f, -2f), lines[i], 1.45f, TextAlignmentOptions.Center);
    }

    private static void RenderLanguage()
    {
        CreateText(new Vector3(0f, 0.65f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.languageHint"), 1.55f, TextAlignmentOptions.Center);

        CreateButton(new Vector3(-1.25f, -0.2f, -2f), new Vector3(0.62f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.language.english"), () =>
            {
                ParadoxPlugin.Instance.SetLanguage(Language.English);
                Render();
            });

        CreateButton(new Vector3(1.25f, -0.2f, -2f), new Vector3(0.62f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.language.polish"), () =>
            {
                ParadoxPlugin.Instance.SetLanguage(Language.Polish);
                Render();
            });

        var selected = ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish ? "Polski" : "English";
        CreateText(new Vector3(0f, -1.15f, -2f), $"✓ {selected}", 1.65f, TextAlignmentOptions.Center);
    }

    private static PassiveButton CreateButton(Vector3 position, Vector3 scale, string label, Action action)
    {
        if (_menu == null || _content == null)
            throw new InvalidOperationException("PARADOX lobby settings are not initialized.");

        return CloneButton(
            _menu.GameSettingsButton,
            _content.transform,
            $"PARADOX_LobbyButton_{label}",
            position,
            scale,
            label,
            action);
    }

    private static PassiveButton CloneButton(
        PassiveButton template,
        Transform parent,
        string name,
        Vector3 position,
        Vector3 scale,
        string label,
        Action action)
    {
        var button = UnityEngine.Object.Instantiate(template, parent);
        button.name = name;
        button.transform.localPosition = position;
        button.transform.localScale = scale;

        var aspect = button.GetComponent<AspectPosition>();
        if (aspect != null)
            aspect.enabled = false;

        button.OnClick = new();
        button.OnClick.AddListener(action);

        var text = button.buttonText ?? button.GetComponentInChildren<TextMeshPro>(true);
        if (text != null)
        {
            var translator = text.GetComponent<TextTranslatorTMP>();
            if (translator != null)
                translator.enabled = false;

            text.text = label;
            text.enableWordWrapping = false;
            text.alignment = TextAlignmentOptions.Center;
        }

        button.gameObject.SetActive(true);
        return button;
    }

    private static TextMeshPro CreateText(Vector3 position, string value, float fontSize, TextAlignmentOptions alignment)
    {
        if (_menu == null || _content == null)
            throw new InvalidOperationException("PARADOX lobby settings are not initialized.");

        var template = _menu.GameSettingsButton.buttonText
            ?? _menu.GameSettingsButton.GetComponentInChildren<TextMeshPro>(true);

        if (template == null)
            throw new InvalidOperationException("PARADOX could not find a text template.");

        var text = UnityEngine.Object.Instantiate(template, _content.transform);
        var translator = text.GetComponent<TextTranslatorTMP>();
        if (translator != null)
            translator.enabled = false;

        text.transform.localPosition = position;
        text.transform.localScale = Vector3.one;
        text.text = value;
        text.fontSize = fontSize;
        text.fontSizeMin = fontSize;
        text.fontSizeMax = fontSize;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.gameObject.SetActive(true);
        return text;
    }
}
