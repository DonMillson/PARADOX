using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Paradox.Localization;
using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles;
using Paradox.Settings;
using Paradox.UI;
using Reactor;

namespace Paradox;

[BepInAutoPlugin("gg.paradox.mod", "PARADOX")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class ParadoxPlugin : BasePlugin
{
    public static ParadoxPlugin Instance { get; private set; } = null!;
    public static ParadoxClientRegistry Clients { get; } = new();
    public static Localizer Localizer { get; } = DefaultTranslations.Create();

    private ConfigEntry<Language>? _language;
    private readonly Dictionary<RoleId, ConfigEntry<bool>> _roleEnabled = new();
    private readonly Dictionary<RoleId, ConfigEntry<int>> _roleChance = new();
    private readonly Dictionary<ParadoxMeterSource, ConfigEntry<int>> _meterGains = new();

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Instance = this;
        Clients.Clear();

        LoadPreferences();

        Log.LogInfo($"PARADOX v{ParadoxInfo.Version} / protocol {ParadoxInfo.ProtocolVersion} loading...");
        Harmony.PatchAll();
        Log.LogInfo("PARADOX loaded. Reactor RPC transport ready.");
    }

    public void SetLanguage(Language language)
    {
        Localizer.CurrentLanguage = language;

        if (_language != null)
            _language.Value = language;
    }

    public void SetRoleEnabled(RoleId role, bool enabled)
    {
        ParadoxRoleSettings.SetEnabled(role, enabled);

        if (_roleEnabled.TryGetValue(role, out var entry))
            entry.Value = enabled;

        ParadoxNetwork.BroadcastRoleSetting(role);
    }

    public void ToggleRoleEnabled(RoleId role) =>
        SetRoleEnabled(role, !ParadoxRoleSettings.IsEnabled(role));

    public void SetRoleSpawnChance(RoleId role, int chance)
    {
        ParadoxRoleSettings.SetSpawnChance(role, chance);

        if (_roleChance.TryGetValue(role, out var entry))
            entry.Value = ParadoxRoleSettings.GetSpawnChance(role);

        ParadoxNetwork.BroadcastRoleSetting(role);
    }

    public void CycleRoleSpawnChance(RoleId role)
    {
        ParadoxRoleSettings.CycleSpawnChance(role);

        if (_roleChance.TryGetValue(role, out var entry))
            entry.Value = ParadoxRoleSettings.GetSpawnChance(role);

        // Cycling the host's chance in the UI must update clients just like SetRoleSpawnChance.
        ParadoxNetwork.BroadcastRoleSetting(role);
    }

    public void CycleMeterGain(ParadoxMeterSource source, int direction)
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return;

        var value = ParadoxGameplaySettings.CycleGain(source, direction);
        if (_meterGains.TryGetValue(source, out var entry))
            entry.Value = value;

        ParadoxNetwork.BroadcastMeterSetting(source);
    }

    private void LoadPreferences()
    {
        _language = Config.Bind(
            "General",
            "Language",
            Language.English,
            "PARADOX interface language.");

        Localizer.CurrentLanguage = _language.Value;

        foreach (var source in Enum.GetValues<ParadoxMeterSource>())
        {
            var entry = Config.Bind(
                "ParadoxMeter",
                $"{source}.Gain",
                ParadoxGameplaySettings.GetGain(source),
                $"Instability added by {source} (0 to 20).");

            _meterGains[source] = entry;
            ParadoxGameplaySettings.SetGain(source, entry.Value);
        }

        foreach (var definition in RoleRegistry.All)
        {
            if (!ParadoxRoleSettings.IsImplemented(definition.Id))
                continue;

            var role = definition.Id;
            var enabled = Config.Bind(
                "Roles",
                $"{role}.Enabled",
                ParadoxRoleSettings.IsEnabled(role),
                $"Enable the PARADOX role {role}.");

            var chance = Config.Bind(
                "Roles",
                $"{role}.SpawnChance",
                ParadoxRoleSettings.GetSpawnChance(role),
                $"Spawn chance for {role}, from 0 to 100.");

            _roleEnabled[role] = enabled;
            _roleChance[role] = chance;

            ParadoxRoleSettings.SetEnabled(role, enabled.Value);
            ParadoxRoleSettings.SetSpawnChance(role, chance.Value);
        }
    }
}
