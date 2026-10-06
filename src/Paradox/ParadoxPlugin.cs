using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Paradox.Localization;
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
    }

    private void LoadPreferences()
    {
        _language = Config.Bind(
            "General",
            "Language",
            Language.English,
            "PARADOX interface language.");

        Localizer.CurrentLanguage = _language.Value;

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
