using BepInEx;
using BepInEx.Configuration;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Paradox.Localization;
using Paradox.Maps;
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
    private ConfigEntry<bool>? _experimentalLargeLobbies;
    private ConfigEntry<int>? _experimentalMaxPlayers;

    // This is a host create-room UI experiment, NOT a server capacity bypass.
    public bool ExperimentalLargeLobbiesEnabled => _experimentalLargeLobbies?.Value ?? false;
    public int ExperimentalLobbyMaxPlayers =>
        LargeLobbyPolicy.ClampRequestedLimit(_experimentalMaxPlayers?.Value ?? 50);
    private readonly Dictionary<RoleId, ConfigEntry<bool>> _roleEnabled = new();
    private readonly Dictionary<RoleId, ConfigEntry<int>> _roleChance = new();
    private readonly Dictionary<ParadoxMeterSource, ConfigEntry<int>> _meterGains = new();

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Instance = this;
        Clients.Clear();

        LoadPreferences();

        if (ExperimentalLargeLobbiesEnabled)
            Log.LogWarning($"Experimental large lobby capacity requested: {ExperimentalLobbyMaxPlayers}. " +
                "Only use a private modded server. 16–50 player gameplay is untested.");

        if (!ParadoxStation.TryValidateDesign(out var stationIssue))
            Log.LogError($"PARADOX STATION design manifest invalid: {stationIssue}");
        else
            Log.LogInfo($"PARADOX STATION design manifest: {ParadoxStation.Rooms.Count} connected rooms.");

        Log.LogInfo($"PARADOX v{ParadoxInfo.Version} / protocol {ParadoxInfo.ProtocolVersion} loading...");
        try
        {
            var gameVersion = UnityEngine.Application.version;
            var reactorAssembly = typeof(ReactorPlugin).Assembly.GetName().Version;
            Log.LogInfo($"PARADOX diagnostic: game application version '{gameVersion}', " +
                        $"process {IntPtr.Size * 8}-bit, Reactor assembly {reactorAssembly}.");
            if (IntPtr.Size != 8)
                Log.LogWarning("PARADOX dev.4 test package targets Windows x64 Among Us v19.");
            if (!gameVersion.StartsWith("19.", StringComparison.Ordinal) &&
                !gameVersion.Contains("2026.9.29", StringComparison.Ordinal))
                Log.LogWarning($"PARADOX v19 test baseline does not recognize game version '{gameVersion}'.");
        }
        catch (Exception e)
        {
            Log.LogWarning($"PARADOX could not read runtime diagnostics: {e.Message}");
        }

        // A single v19 method signature mismatch used to abort PatchAll(),
        // leaving *every* PARADOX UI/role patch unavailable. Register each
        // attributed Harmony class separately and report all failures.
        // Never claim a complete boot when any runtime patch could not bind.
        RegisterPatchesWithDiagnostics();
    }

    private void RegisterPatchesWithDiagnostics()
    {
        var errors = new List<string>();
        var successful = 0;

        var patchClasses = typeof(ParadoxPlugin).Assembly.GetTypes()
            .Where(type => Attribute.IsDefined(type, typeof(HarmonyPatch), false))
            // The lobby/settings diagnostic UI needs priority, so it can
            // still appear when an unrelated ability patch is incompatible.
            .OrderBy(type => type.Namespace == "Paradox.UI" ? 0 : 1)
            .ThenBy(type => type.FullName, StringComparer.Ordinal)
            .ToArray();

        foreach (var patchClass in patchClasses)
        {
            try
            {
                Harmony.CreateClassProcessor(patchClass).Patch();
                successful++;
            }
            catch (Exception ex)
            {
                var name = patchClass.FullName ?? patchClass.Name;
                errors.Add(name);
                Log.LogError($"PARADOX PATCH FAILED: {name} on Among Us v19: {ex}");
            }
        }

        Log.LogInfo($"PARADOX Harmony audit: {successful}/{patchClasses.Length} patch classes registered.");
        if (errors.Count > 0)
        {
            Log.LogError($"PARADOX PARTIAL START: {errors.Count} failed classes: " +
                         string.Join(", ", errors) +
                         ". Settings/abilities can be missing. Send BepInEx/LogOutput.log for diagnosis.");
            return;
        }

        Log.LogInfo("PARADOX loaded. Reactor RPC transport registered; multiplayer test still required.");
        Log.LogInfo("PARADOX UI: host -> Create Game -> lobby Settings -> PARADOX tab; " +
                    "other clients have the lobby role card, not host-only settings.");
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
        _experimentalLargeLobbies = Config.Bind(
            "ExperimentalLargeLobby",
            "Enable",
            false,
            "EXPERIMENTAL: Unlock a 16–50 seat create-room picker only on a custom/modded server. " +
            "Disabled by default; does not upgrade the server or fix the 15-player meeting UI.");
        _experimentalMaxPlayers = Config.Bind(
            "ExperimentalLargeLobby",
            "MaxPlayers",
            50,
            "Experimental upper limit on custom servers (15–50). Ignored unless Enable is true.");

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
