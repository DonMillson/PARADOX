using HarmonyLib;
using Paradox.Localization;
using Paradox.Core;
using Paradox.Roles;
using Paradox.Settings;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

/// <summary>
/// Native-looking PARADOX tab inside the Among Us lobby settings.
/// Uses the game's own GameOptionsMenu rows/scroller instead of drawing text over the vanilla menu.
/// </summary>
[HarmonyPatch(typeof(GameSettingMenu))]
public static class ParadoxLobbySettingsPatch
{
    private const string TabName = "PARADOX_SettingsTab";

    private static GameOptionsMenu? _tab;
    private static PassiveButton? _tabButton;
    private static GameSettingMenu? _menu;

    private static readonly Dictionary<IntPtr, RowDefinition> Rows = new();
    private static readonly List<(CategoryHeaderMasked Header, string Title)> Headers = new();
    private static readonly List<(StringOption Row, RowDefinition Definition)> OrderedRows = new();

    private enum RowKind
    {
        Language,
        Role,
        MeterGain,
        Info
    }

    private sealed class RowDefinition
    {
        public RowKind Kind { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public RoleId Role { get; init; }
        public ParadoxMeterSource MeterSource { get; init; }
        public RoleFaction Faction { get; init; }
        public bool Planned { get; init; }
    }

    [HarmonyPatch(nameof(GameSettingMenu.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameSettingMenu __instance)
    {
        try
        {
            Cleanup();
            _menu = __instance;

            if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
                return;

            CreateTab(__instance);
            CreateTabButton(__instance);
            BuildRows(__instance);
            ApplyPolishFontsIfNeeded();

            ParadoxPlugin.Instance.Log.LogInfo("PARADOX native lobby settings tab created.");
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogError($"Failed to build PARADOX lobby settings: {e}");
            Cleanup();
        }
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab))]
    [HarmonyPrefix]
    public static void ChangeTabPrefix(bool previewOnly)
    {
        if (previewOnly)
            return;

        if (_tab != null)
            _tab.gameObject.SetActive(false);

        _tabButton?.SelectButton(false);
    }

    [HarmonyPatch(nameof(GameSettingMenu.Close))]
    [HarmonyPrefix]
    public static void ClosePrefix() => Cleanup();

    private static void CreateTab(GameSettingMenu menu)
    {
        var source = menu.GameSettingsTab;
        if (source == null)
            throw new InvalidOperationException("GameSettingsTab template is missing.");

        _tab = Object.Instantiate(source, source.transform.parent);
        _tab.name = TabName;

        foreach (var vanillaOption in _tab.GetComponentsInChildren<OptionBehaviour>(true))
            Object.Destroy(vanillaOption.gameObject);

        if (_tab.MapPicker != null)
            _tab.MapPicker.gameObject.SetActive(false);

        _tab.Children = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
        _tab.gameObject.SetActive(false);
    }

    private static void CreateTabButton(GameSettingMenu menu)
    {
        var template = menu.GameSettingsButton;
        var roles = menu.RoleSettingsButton;

        if (template == null)
            throw new InvalidOperationException("GameSettingsButton template is missing.");

        var delta = new Vector3(0f, -0.62f, 0f);
        if (roles != null)
        {
            var measured = roles.transform.localPosition - template.transform.localPosition;
            if (measured.magnitude > 0.05f)
                delta = measured;
        }

        var baseButton = roles ?? template;

        _tabButton = Object.Instantiate(template, template.transform.parent);
        _tabButton.name = "PARADOX_SettingsButton";
        _tabButton.transform.localPosition = baseButton.transform.localPosition + delta;
        _tabButton.transform.localScale = baseButton.transform.localScale;

        DestroyTranslator(_tabButton.buttonText);
        _tabButton.buttonText.text = "PARADOX";

        foreach (var go in new[] { _tabButton.activeSprites, _tabButton.selectedSprites })
        {
            if (go == null)
                continue;

            var renderer = go.GetComponent<SpriteRenderer>();
            if (renderer != null)
                renderer.color = new Color(0.33f, 0.78f, 0.76f, 1f);
        }

        _tabButton.OnClick = new();
        _tabButton.OnClick.AddListener((Action)(OpenParadoxTab));
        _tabButton.gameObject.SetActive(true);

        if (menu.ControllerSelectable != null)
            menu.ControllerSelectable.Add(_tabButton);
    }

    private static void OpenParadoxTab()
    {
        if (_menu == null || _tab == null)
            return;

        _menu.ChangeTab(-1, false);

        _menu.PresetsTab?.gameObject.SetActive(false);
        _menu.GameSettingsTab?.gameObject.SetActive(false);
        _menu.RoleSettingsTab?.gameObject.SetActive(false);

        _menu.GamePresetsButton?.SelectButton(false);
        _menu.GameSettingsButton?.SelectButton(false);
        _menu.RoleSettingsButton?.SelectButton(false);

        _tab.gameObject.SetActive(true);
        _tabButton?.SelectButton(true);

        if (_menu.MenuDescriptionText != null)
        {
            DestroyTranslator(_menu.MenuDescriptionText);
            _menu.MenuDescriptionText.text = ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish
                ? "Ustawienia PARADOX. Host wybiera aktywne role, szanse pojawienia, język i mechaniki moda."
                : "PARADOX settings. The host controls enabled roles, spawn chances, language and mod mechanics.";
        }

        ApplyPolishFontsIfNeeded();
        RefreshRows();
        LayoutRows();
    }

    private static void ApplyPolishFontsIfNeeded()
    {
        if (ParadoxPlugin.Localizer.CurrentLanguage != Language.Polish)
            return;

        ParadoxFontSupport.EnsurePolishGlyphs();

        foreach (var header in Headers)
            ParadoxFontSupport.ApplyTo(header.Header.Title);

        foreach (var item in OrderedRows)
        {
            ParadoxFontSupport.ApplyTo(item.Row.TitleText);
            ParadoxFontSupport.ApplyTo(item.Row.ValueText);
        }

        if (_menu?.MenuDescriptionText != null)
            ParadoxFontSupport.ApplyTo(_menu.MenuDescriptionText);
    }

    private static void BuildRows(GameSettingMenu menu)
    {
        if (_tab == null)
            return;

        Rows.Clear();
        Headers.Clear();
        OrderedRows.Clear();

        CreateHeader(menu, "GENERAL / OGÓLNE");
        CreateRow(menu, new RowDefinition
        {
            Kind = RowKind.Language,
            Title = "Language / Język"
        });

        CreateHeader(menu, "IMPOSTOR ROLES");
        foreach (var definition in RoleRegistry.All.Where(x => x.Faction == RoleFaction.Impostor))
            CreateRoleRow(menu, definition);

        CreateHeader(menu, "CREWMATE ROLES");
        foreach (var definition in RoleRegistry.All.Where(x => x.Faction == RoleFaction.Crewmate))
            CreateRoleRow(menu, definition);

        CreateHeader(menu, "NEUTRAL ROLES");
        foreach (var definition in RoleRegistry.All.Where(x => x.Faction == RoleFaction.Neutral))
            CreateRoleRow(menu, definition);

        CreateHeader(menu, "PARADOX METER");
        foreach (var source in Enum.GetValues<ParadoxMeterSource>())
            CreateMeterRow(menu, source);
        CreateInfoRow(menu, "Events", "25 / 50 / 75 / 100%");

        CreateHeader(menu, "MECHANICS");
        CreateInfoRow(menu, "Doppelgänger", "12s disguise / 30s cooldown");
        CreateInfoRow(menu, "Parasite", "15s infection / 30s cooldown");

        _tab.Children = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
        foreach (var item in OrderedRows)
            _tab.Children.Add(item.Row);

        RefreshRows();
        LayoutRows();
    }

    private static void CreateRoleRow(GameSettingMenu menu, RoleDefinition definition)
    {
        CreateRow(menu, new RowDefinition
        {
            Kind = RowKind.Role,
            Title = ParadoxPlugin.Localizer.Get(definition.NameKey),
            Role = definition.Id,
            Faction = definition.Faction,
            Planned = !ParadoxRoleSettings.IsImplemented(definition.Id)
        });
    }

    private static void CreateMeterRow(GameSettingMenu menu, ParadoxMeterSource source)
    {
        CreateRow(menu, new RowDefinition
        {
            Kind = RowKind.MeterGain,
            Title = ParadoxPlugin.Localizer.Get($"ui.menu.meterGain.{source}"),
            MeterSource = source
        });
    }

    private static void CreateInfoRow(GameSettingMenu menu, string title, string value)
    {
        CreateRow(menu, new RowDefinition
        {
            Kind = RowKind.Info,
            Title = title,
            Value = value
        });
    }

    private static void CreateHeader(GameSettingMenu menu, string title)
    {
        if (_tab == null)
            return;

        var header = Object.Instantiate(
            menu.GameSettingsTab.categoryHeaderOrigin,
            Vector3.zero,
            Quaternion.identity,
            _tab.settingsContainer);

        header.name = $"PARADOX_Header_{title}";
        DestroyTranslator(header.Title);
        header.Title.text = title;

        var maskLayer = GameOptionsMenu.MASK_LAYER;
        header.Background.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);

        if (header.Divider != null)
            header.Divider.material.SetInt(PlayerMaterial.MaskLayer, maskLayer);

        header.Title.fontMaterial.SetFloat("_StencilComp", 3f);
        header.Title.fontMaterial.SetFloat("_Stencil", maskLayer);
        header.transform.localScale = Vector3.one * GameOptionsMenu.HEADER_SCALE;

        Headers.Add((header, title));
    }

    private static void CreateRow(GameSettingMenu menu, RowDefinition definition)
    {
        if (_tab == null)
            return;

        var row = Object.Instantiate(
            menu.GameSettingsTab.stringOptionOrigin,
            _tab.settingsContainer);

        row.name = $"PARADOX_Row_{definition.Title}";
        row.SetClickMask(menu.GameSettingsButton.ClickMask);
        row.SetUpFromData(row.data, GameOptionsMenu.MASK_LAYER);
        row.OnValueChanged = new Action<OptionBehaviour>(_ => { });

        // Current Among Us builds do not reliably route cloned StringOption
        // arrow clicks back through Increase/Decrease. Wire our own listeners
        // directly so changing language/role chance always works.
        if (row.PlusBtn != null)
        {
            row.PlusBtn.OnClick = new();
            row.PlusBtn.OnClick.AddListener((Action)(() => CycleForward(definition)));
            row.PlusBtn.SetButtonEnableState(true);
        }

        if (row.MinusBtn != null)
        {
            row.MinusBtn.OnClick = new();
            row.MinusBtn.OnClick.AddListener((Action)(() => CycleBackward(definition)));
            row.MinusBtn.SetButtonEnableState(true);
        }

        DestroyTranslator(row.TitleText);
        row.TitleText.text = definition.Title;

        if (definition.Kind == RowKind.Role)
        {
            row.LabelBackground.color = definition.Faction switch
            {
                RoleFaction.Impostor => new Color(0.64f, 0.18f, 0.18f, 1f),
                RoleFaction.Crewmate => new Color(0.18f, 0.55f, 0.66f, 1f),
                _ => new Color(0.60f, 0.48f, 0.15f, 1f)
            };
        }

        Rows[row.Pointer] = definition;
        OrderedRows.Add((row, definition));
    }

    private static void RefreshRows()
    {
        foreach (var item in OrderedRows)
        {
            var row = item.Row;
            var definition = item.Definition;

            row.TitleText.text = definition.Kind switch
            {
                RowKind.Role => ParadoxPlugin.Localizer.Get($"role.{definition.Role}.name"),
                RowKind.MeterGain => ParadoxPlugin.Localizer.Get($"ui.menu.meterGain.{definition.MeterSource}"),
                _ => definition.Title
            };

            switch (definition.Kind)
            {
                case RowKind.Language:
                    row.ValueText.text = ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish
                        ? "POLSKI"
                        : "ENGLISH";
                    SetArrowsVisible(row, true);
                    break;

                case RowKind.Role:
                    if (definition.Planned)
                    {
                        row.ValueText.text = ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish
                            ? "PLANOWANA"
                            : "PLANNED";
                        SetArrowsVisible(row, false);
                    }
                    else if (!ParadoxRoleSettings.IsEnabled(definition.Role))
                    {
                        row.ValueText.text = "OFF";
                        SetArrowsVisible(row, true);
                    }
                    else
                    {
                        row.ValueText.text = $"{ParadoxRoleSettings.GetSpawnChance(definition.Role)}%";
                        SetArrowsVisible(row, true);
                    }
                    break;

                case RowKind.MeterGain:
                    row.ValueText.text = $"+{ParadoxGameplaySettings.GetGain(definition.MeterSource)}";
                    SetArrowsVisible(row, true);
                    break;

                case RowKind.Info:
                    row.ValueText.text = definition.Value;
                    SetArrowsVisible(row, false);
                    break;
            }
        }
    }

    private static void SetArrowsVisible(StringOption row, bool visible)
    {
        if (row.MinusBtn != null)
            row.MinusBtn.gameObject.SetActive(visible);

        if (row.PlusBtn != null)
            row.PlusBtn.gameObject.SetActive(visible);
    }

    private static void LayoutRows()
    {
        if (_tab == null)
            return;

        var offset = 2.7f;
        var rowIndex = 0;
        var headerIndex = 0;

        foreach (var section in BuildSections())
        {
            if (headerIndex < Headers.Count)
            {
                offset -= GameOptionsMenu.HEADER_HEIGHT;
                Headers[headerIndex].Header.transform.localPosition =
                    new Vector3(GameOptionsMenu.HEADER_X, offset, -2f);
                headerIndex++;
            }

            foreach (var _ in section)
            {
                if (rowIndex >= OrderedRows.Count)
                    break;

                offset -= GameOptionsMenu.SPACING_Y;
                OrderedRows[rowIndex].Row.transform.localPosition =
                    new Vector3(GameOptionsMenu.START_POS_X, offset, -2f);
                rowIndex++;
            }

            offset -= 0.12f;
        }

        if (_tab.scrollBar != null)
            _tab.scrollBar.ContentYBounds.max = Math.Max(0f, -offset + 0.6f);
    }

    private static IEnumerable<int[]> BuildSections()
    {
        yield return new int[1];
        yield return new int[RoleRegistry.All.Count(x => x.Faction == RoleFaction.Impostor)];
        yield return new int[RoleRegistry.All.Count(x => x.Faction == RoleFaction.Crewmate)];
        yield return new int[RoleRegistry.All.Count(x => x.Faction == RoleFaction.Neutral)];
        yield return new int[5];
        yield return new int[2];
    }

    private static void CycleForward(RowDefinition definition)
    {
        switch (definition.Kind)
        {
            case RowKind.MeterGain:
                ParadoxPlugin.Instance.CycleMeterGain(definition.MeterSource, +1);
                break;

            case RowKind.Language:
                ParadoxPlugin.Instance.SetLanguage(
                    ParadoxPlugin.Localizer.CurrentLanguage == Language.Polish
                        ? Language.English
                        : Language.Polish);
                ApplyPolishFontsIfNeeded();
                break;

            case RowKind.Role when !definition.Planned:
                var current = ParadoxRoleSettings.IsEnabled(definition.Role)
                    ? ParadoxRoleSettings.GetSpawnChance(definition.Role)
                    : 0;

                var next = current switch
                {
                    < 25 => 25,
                    < 50 => 50,
                    < 75 => 75,
                    < 100 => 100,
                    _ => 0
                };

                if (next == 0)
                {
                    ParadoxPlugin.Instance.SetRoleEnabled(definition.Role, false);
                }
                else
                {
                    ParadoxPlugin.Instance.SetRoleEnabled(definition.Role, true);
                    ParadoxPlugin.Instance.SetRoleSpawnChance(definition.Role, next);
                }
                break;
        }

        RefreshRows();
    }

    private static void CycleBackward(RowDefinition definition)
    {
        switch (definition.Kind)
        {
            case RowKind.MeterGain:
                ParadoxPlugin.Instance.CycleMeterGain(definition.MeterSource, -1);
                break;

            case RowKind.Language:
                CycleForward(definition);
                break;

            case RowKind.Role when !definition.Planned:
                var current = ParadoxRoleSettings.IsEnabled(definition.Role)
                    ? ParadoxRoleSettings.GetSpawnChance(definition.Role)
                    : 0;

                var previous = current switch
                {
                    <= 0 => 100,
                    <= 25 => 0,
                    <= 50 => 25,
                    <= 75 => 50,
                    _ => 75
                };

                if (previous == 0)
                {
                    ParadoxPlugin.Instance.SetRoleEnabled(definition.Role, false);
                }
                else
                {
                    ParadoxPlugin.Instance.SetRoleEnabled(definition.Role, true);
                    ParadoxPlugin.Instance.SetRoleSpawnChance(definition.Role, previous);
                }
                break;
        }

        RefreshRows();
    }

    private static bool TryGetRow(StringOption row, out RowDefinition definition)
    {
        definition = null!;

        try
        {
            return row != null && Rows.TryGetValue(row.Pointer, out definition!);
        }
        catch
        {
            return false;
        }
    }

    private static void DestroyTranslator(Component? component)
    {
        if (component == null)
            return;

        var translator = component.GetComponent<TextTranslatorTMP>();
        if (translator != null)
            Object.DestroyImmediate(translator);
    }

    private static void Cleanup()
    {
        Rows.Clear();
        Headers.Clear();
        OrderedRows.Clear();

        if (_tab != null)
            Object.Destroy(_tab.gameObject);

        if (_tabButton != null)
            Object.Destroy(_tabButton.gameObject);

        _tab = null;
        _tabButton = null;
        _menu = null;
    }

    [HarmonyPatch(typeof(GameOptionsMenu), nameof(GameOptionsMenu.Update))]
    private static class GameOptionsUpdatePatch
    {
        [HarmonyPostfix]
        public static void Postfix(GameOptionsMenu __instance)
        {
            if (__instance == null || __instance.name != TabName)
                return;

            LayoutRows();
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Initialize))]
    private static class StringInitializePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(StringOption __instance)
        {
            if (!TryGetRow(__instance, out var definition))
                return true;

            DestroyTranslator(__instance.TitleText);
            __instance.TitleText.text = definition.Title;
            RefreshRows();
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Increase))]
    private static class StringIncreasePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(StringOption __instance)
        {
            if (!TryGetRow(__instance, out var definition))
                return true;

            CycleForward(definition);
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.Decrease))]
    private static class StringDecreasePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(StringOption __instance)
        {
            if (!TryGetRow(__instance, out var definition))
                return true;

            CycleBackward(definition);
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.UpdateValue))]
    private static class StringUpdateValuePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(StringOption __instance)
        {
            if (!TryGetRow(__instance, out _))
                return true;

            RefreshRows();
            return false;
        }
    }

    [HarmonyPatch(typeof(StringOption), nameof(StringOption.FixedUpdate))]
    private static class StringFixedUpdatePatch
    {
        [HarmonyPrefix]
        public static bool Prefix(StringOption __instance)
        {
            if (!TryGetRow(__instance, out _))
                return true;

            return false;
        }
    }
}
