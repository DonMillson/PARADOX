using HarmonyLib;
using Paradox.Settings;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Paradox.UI;

[HarmonyPatch(typeof(GameSettingMenu))]
public static class ParadoxSettingsMenuPatch
{
    private static GameOptionsMenu? ParadoxTab;
    private static PassiveButton? ParadoxButton;

    private static readonly float[] DisguiseDurations = { 5f, 8f, 10f, 12f, 15f, 20f, 30f };
    private static readonly float[] Cooldowns = { 10f, 15f, 20f, 25f, 30f, 40f, 45f, 60f };

    [HarmonyPatch(nameof(GameSettingMenu.Start))]
    [HarmonyPostfix]
    public static void StartPostfix(GameSettingMenu __instance)
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        ParadoxTab = Object.Instantiate(
            __instance.GameSettingsTab,
            __instance.GameSettingsTab.transform.parent);
        ParadoxTab.name = "PARADOXSettingsTab";

        foreach (var option in ParadoxTab.GetComponentsInChildren<OptionBehaviour>())
            Object.Destroy(option.gameObject);

        ParadoxButton = Object.Instantiate(
            __instance.GameSettingsButton,
            __instance.GameSettingsButton.transform.parent);
        ParadoxButton.name = "PARADOXSettingsButton";
        ParadoxButton.transform.localPosition =
            __instance.RoleSettingsButton.transform.localPosition +
            (__instance.RoleSettingsButton.transform.localPosition -
             __instance.GameSettingsButton.transform.localPosition);

        ParadoxButton.buttonText.text = "PARADOX";

        var options = new Il2CppSystem.Collections.Generic.List<OptionBehaviour>();
        AddFloatOption(
            __instance,
            "Doppelgänger — Disguise Duration",
            DisguiseDurations,
            ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds,
            value => ParadoxRoleSettings.DoppelgangerDisguiseDurationSeconds = value,
            options);

        AddFloatOption(
            __instance,
            "Doppelgänger — Cooldown",
            Cooldowns,
            ParadoxRoleSettings.DoppelgangerCooldownSeconds,
            value => ParadoxRoleSettings.DoppelgangerCooldownSeconds = value,
            options);

        ParadoxTab.Children = options;
        ParadoxTab.gameObject.SetActive(false);

        ParadoxButton.OnClick.AddListener((Action)(() =>
        {
            __instance.ChangeTab(-1, false);
            ParadoxTab.gameObject.SetActive(true);
            __instance.MenuDescriptionText.text = "PARADOX role and gameplay settings";
            ParadoxButton.SelectButton(true);
        }));
    }

    [HarmonyPatch(nameof(GameSettingMenu.ChangeTab))]
    [HarmonyPrefix]
    public static void ChangeTabPrefix(bool previewOnly)
    {
        if (previewOnly)
            return;

        if (ParadoxTab)
            ParadoxTab.gameObject.SetActive(false);

        if (ParadoxButton)
            ParadoxButton.SelectButton(false);
    }

    private static void AddFloatOption(
        GameSettingMenu menu,
        string title,
        float[] values,
        float current,
        Action<float> onChanged,
        Il2CppSystem.Collections.Generic.List<OptionBehaviour> options)
    {
        if (ParadoxTab == null)
            return;

        var option = Object.Instantiate(
            menu.GameSettingsTab.stringOptionOrigin,
            ParadoxTab.settingsContainer);

        option.SetClickMask(menu.GameSettingsButton.ClickMask);
        option.SetUpFromData(option.data, GameOptionsMenu.MASK_LAYER);
        option.TitleText.text = title;

        var index = Array.IndexOf(values, current);
        if (index < 0)
            index = 0;

        option.Value = option.oldValue = index;
        option.ValueText.text = $"{values[index]:0.#} s";
        option.OnValueChanged = new Action<OptionBehaviour>(behaviour =>
        {
            var stringOption = behaviour.Cast<StringOption>();
            var selected = Mathf.Clamp(stringOption.Value, 0, values.Length - 1);
            onChanged(values[selected]);
            stringOption.ValueText.text = $"{values[selected]:0.#} s";
        });

        options.Add(option);
    }
}
