using HarmonyLib;
using Paradox.Localization;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;

namespace Paradox.UI;

[HarmonyPatch(typeof(MainMenuManager), nameof(MainMenuManager.Start))]
public static class ParadoxSettingsMenuPatch
{
    private const int RolesPerPage = 6;

    private static MainMenuManager? _menu;
    private static PassiveButton? _openButton;
    private static GameObject? _root;
    private static GameObject? _contentRoot;
    private static int _rolePage;
    private static MenuTab _tab = MenuTab.Roles;

    private enum MenuTab
    {
        Roles,
        Mechanics,
        Meter,
        Language
    }

    [HarmonyPostfix]
    public static void MainMenuStartPostfix(MainMenuManager __instance)
    {
        _menu = __instance;

        if (_root != null)
            UnityEngine.Object.Destroy(_root);

        if (_openButton != null)
            UnityEngine.Object.Destroy(_openButton.gameObject);

        _openButton = CloneButton(
            __instance.quitButton,
            __instance.quitButton.transform.parent,
            "PARADOX_SettingsButton",
            __instance.quitButton.transform.localPosition + new Vector3(0f, 0.62f, 0f),
            new Vector3(0.72f, 0.72f, 1f),
            ParadoxPlugin.Localizer.Get("ui.menu.open"),
            OpenMenu);

        _root = new GameObject("PARADOX_SettingsRoot");
        _root.transform.SetParent(__instance.transform, false);
        _root.transform.localPosition = new Vector3(0f, 0f, -20f);

        CreateBackdrop(__instance, _root.transform);
        _root.SetActive(false);
    }

    private static void OpenMenu()
    {
        if (_root == null)
            return;

        _root.SetActive(true);
        Render();
    }

    private static void CloseMenu()
    {
        if (_root != null)
            _root.SetActive(false);
    }

    private static void Render()
    {
        if (_root == null || _menu == null)
            return;

        if (_contentRoot != null)
            UnityEngine.Object.Destroy(_contentRoot);

        _contentRoot = new GameObject("PARADOX_SettingsContent");
        _contentRoot.transform.SetParent(_root.transform, false);

        CreateText(
            _contentRoot.transform,
            "PARADOX_Title",
            new Vector3(0f, 2.55f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.title"),
            3.2f,
            TextAlignmentOptions.Center);

        CreateButton(new Vector3(-3.15f, 1.92f, -2f), new Vector3(0.43f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.menu.roles"), () =>
            {
                _tab = MenuTab.Roles;
                Render();
            });

        CreateButton(new Vector3(-1.58f, 1.92f, -2f), new Vector3(0.43f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanics"), () =>
            {
                _tab = MenuTab.Mechanics;
                Render();
            });

        CreateButton(new Vector3(0f, 1.92f, -2f), new Vector3(0.43f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.menu.meter"), () =>
            {
                _tab = MenuTab.Meter;
                Render();
            });

        CreateButton(new Vector3(1.58f, 1.92f, -2f), new Vector3(0.43f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.menu.language"), () =>
            {
                _tab = MenuTab.Language;
                Render();
            });

        CreateButton(new Vector3(3.15f, 1.92f, -2f), new Vector3(0.43f, 0.5f, 1f),
            ParadoxPlugin.Localizer.Get("ui.menu.close"), CloseMenu);

        switch (_tab)
        {
            case MenuTab.Roles:
                RenderRoles();
                break;
            case MenuTab.Mechanics:
                RenderMechanics();
                break;
            case MenuTab.Meter:
                RenderMeter();
                break;
            case MenuTab.Language:
                RenderLanguage();
                break;
        }
    }

    private static void RenderRoles()
    {
        if (_contentRoot == null)
            return;

        var roles = RoleRegistry.All;
        var pageCount = Math.Max(1, (int)Math.Ceiling(roles.Count / (double)RolesPerPage));
        _rolePage = Math.Clamp(_rolePage, 0, pageCount - 1);

        CreateText(
            _contentRoot.transform,
            "PARADOX_RoleHint",
            new Vector3(0f, 1.42f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.roleHint"),
            1.45f,
            TextAlignmentOptions.Center);

        var start = _rolePage * RolesPerPage;
        var end = Math.Min(start + RolesPerPage, roles.Count);

        for (var i = start; i < end; i++)
        {
            var definition = roles[i];
            var row = i - start;
            var y = 0.92f - row * 0.52f;
            var role = definition.Id;
            var roleName = ParadoxPlugin.Localizer.Get(definition.NameKey);
            var faction = definition.Faction switch
            {
                RoleFaction.Impostor => "IMP",
                RoleFaction.Crewmate => "CREW",
                _ => "NEUTRAL"
            };

            CreateText(
                _contentRoot.transform,
                $"PARADOX_Role_{role}",
                new Vector3(-1.75f, y, -2f),
                $"[{faction}] {roleName}",
                1.65f,
                TextAlignmentOptions.Left);

            if (!ParadoxRoleSettings.IsImplemented(role))
            {
                CreateText(
                    _contentRoot.transform,
                    $"PARADOX_Planned_{role}",
                    new Vector3(2.0f, y, -2f),
                    ParadoxPlugin.Localizer.Get("ui.menu.planned"),
                    1.45f,
                    TextAlignmentOptions.Center);

                continue;
            }

            var enabledText = ParadoxRoleSettings.IsEnabled(role)
                ? ParadoxPlugin.Localizer.Get("ui.menu.enabled")
                : ParadoxPlugin.Localizer.Get("ui.menu.disabled");

            CreateButton(
                new Vector3(1.25f, y, -2f),
                new Vector3(0.34f, 0.34f, 1f),
                enabledText,
                () =>
                {
                    ParadoxPlugin.Instance.ToggleRoleEnabled(role);
                    Render();
                });

            CreateButton(
                new Vector3(2.55f, y, -2f),
                new Vector3(0.4f, 0.34f, 1f),
                $"{ParadoxRoleSettings.GetSpawnChance(role)}%",
                () =>
                {
                    ParadoxPlugin.Instance.CycleRoleSpawnChance(role);
                    Render();
                });
        }

        CreateText(
            _contentRoot.transform,
            "PARADOX_Page",
            new Vector3(0f, -2.18f, -2f),
            $"{_rolePage + 1}/{pageCount}",
            1.5f,
            TextAlignmentOptions.Center);

        if (_rolePage > 0)
        {
            CreateButton(
                new Vector3(-1.5f, -2.18f, -2f),
                new Vector3(0.45f, 0.4f, 1f),
                ParadoxPlugin.Localizer.Get("ui.menu.previous"),
                () =>
                {
                    _rolePage--;
                    Render();
                });
        }

        if (_rolePage < pageCount - 1)
        {
            CreateButton(
                new Vector3(1.5f, -2.18f, -2f),
                new Vector3(0.45f, 0.4f, 1f),
                ParadoxPlugin.Localizer.Get("ui.menu.next"),
                () =>
                {
                    _rolePage++;
                    Render();
                });
        }
    }

    private static void RenderMechanics()
    {
        if (_contentRoot == null)
            return;

        CreateText(_contentRoot.transform, "PARADOX_MechanicsTitle", new Vector3(0f, 1.0f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsTitle"), 2.4f, TextAlignmentOptions.Center);

        var lines = new[]
        {
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsHost"),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsSaved"),
            ParadoxPlugin.Localizer.Get("ui.menu.mechanicsMore")
        };

        for (var i = 0; i < lines.Length; i++)
        {
            CreateText(_contentRoot.transform, $"PARADOX_Mechanic_{i}",
                new Vector3(0f, 0.25f - i * 0.58f, -2f),
                lines[i], 1.65f, TextAlignmentOptions.Center);
        }
    }

    private static void RenderMeter()
    {
        if (_contentRoot == null)
            return;

        CreateText(_contentRoot.transform, "PARADOX_MeterTitle", new Vector3(0f, 1.0f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.meterTitle"), 2.4f, TextAlignmentOptions.Center);

        var lines = new[]
        {
            ParadoxPlugin.Localizer.Get("ui.menu.kill"),
            ParadoxPlugin.Localizer.Get("ui.menu.sabotage"),
            ParadoxPlugin.Localizer.Get("ui.menu.ability"),
            ParadoxPlugin.Localizer.Get("ui.menu.anomaly"),
            ParadoxPlugin.Localizer.Get("ui.menu.thresholds")
        };

        for (var i = 0; i < lines.Length; i++)
        {
            CreateText(_contentRoot.transform, $"PARADOX_Meter_{i}",
                new Vector3(0f, 0.4f - i * 0.48f, -2f),
                lines[i], 1.8f, TextAlignmentOptions.Center);
        }
    }

    private static void RenderLanguage()
    {
        if (_contentRoot == null)
            return;

        CreateText(
            _contentRoot.transform,
            "PARADOX_LanguageHint",
            new Vector3(0f, 0.85f, -2f),
            ParadoxPlugin.Localizer.Get("ui.menu.languageHint"),
            1.9f,
            TextAlignmentOptions.Center);

        CreateButton(
            new Vector3(-1.35f, -0.15f, -2f),
            new Vector3(0.72f, 0.62f, 1f),
            ParadoxPlugin.Localizer.Get("ui.language.english"),
            () =>
            {
                ParadoxPlugin.Instance.SetLanguage(Language.English);
                Render();
            });

        CreateButton(
            new Vector3(1.35f, -0.15f, -2f),
            new Vector3(0.72f, 0.62f, 1f),
            ParadoxPlugin.Localizer.Get("ui.language.polish"),
            () =>
            {
                ParadoxPlugin.Instance.SetLanguage(Language.Polish);
                Render();
            });

        var selected = ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish ? "Polski" : "English";
        CreateText(
            _contentRoot.transform,
            "PARADOX_SelectedLanguage",
            new Vector3(0f, -1.1f, -2f),
            $"✓ {selected}",
            2.0f,
            TextAlignmentOptions.Center);
    }

    private static void CreateBackdrop(MainMenuManager menu, Transform parent)
    {
        if (menu.screenTint == null)
            return;

        var backdrop = UnityEngine.Object.Instantiate(menu.screenTint.gameObject, parent);
        backdrop.name = "PARADOX_SettingsBackdrop";

        var aspect = backdrop.GetComponent<AspectPosition>();
        if (aspect != null)
            aspect.enabled = false;

        backdrop.transform.localPosition = new Vector3(0f, 0f, 5f);
        backdrop.transform.localScale = new Vector3(1.2f, 1.2f, 1f);

        var renderer = backdrop.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.color = new Color(0f, 0f, 0f, 0.88f);

        backdrop.SetActive(true);
    }

    private static PassiveButton CreateButton(Vector3 position, Vector3 scale, string label, Action action)
    {
        if (_menu == null || _contentRoot == null)
            throw new InvalidOperationException("PARADOX menu is not initialized.");

        return CloneButton(
            _menu.quitButton,
            _contentRoot.transform,
            $"PARADOX_Button_{label}",
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

        SetButtonText(button, label);
        button.gameObject.SetActive(true);
        return button;
    }

    private static void SetButtonText(PassiveButton button, string text)
    {
        if (button.buttonText == null)
            return;

        var translator = button.buttonText.GetComponent<TextTranslatorTMP>();
        if (translator != null)
            translator.enabled = false;

        button.buttonText.text = text;
        button.buttonText.enableWordWrapping = false;
        button.buttonText.alignment = TextAlignmentOptions.Center;
    }

    private static TextMeshPro CreateText(
        Transform parent,
        string name,
        Vector3 position,
        string value,
        float fontSize,
        TextAlignmentOptions alignment)
    {
        if (_menu == null)
            throw new InvalidOperationException("PARADOX menu is not initialized.");

        var text = UnityEngine.Object.Instantiate(_menu.quitButton.buttonText, parent);
        text.name = name;

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
