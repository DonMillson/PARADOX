using HarmonyLib;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles;

[HarmonyPatch(typeof(RoleManager), nameof(RoleManager.SelectRoles))]
public static class InitialRoleAssignmentPatch
{
    [HarmonyPostfix]
    public static void SelectRolesPostfix()
    {
        if (!AmongUsClient.Instance.AmHost)
            return;

        RoleAssignment.Reset();

        var enabledImpostorRoles = BuildEnabledPool(InitialRolePool.Impostor);
        var enabledCrewRoles = BuildEnabledPool(InitialRolePool.Crewmate);
        var enabledNeutralRoles = BuildEnabledPool(InitialRolePool.Neutral);

        var impostors = new List<PlayerControl>();
        var crew = new List<PlayerControl>();

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Role == null)
                continue;

            if (player.Data.Role.IsImpostor)
                impostors.Add(player);
            else
                crew.Add(player);
        }

        for (var i = 0; i < impostors.Count && enabledImpostorRoles.Count > 0; i++)
        {
            var role = enabledImpostorRoles[i % enabledImpostorRoles.Count];
            RoleAssignment.Assign(impostors[i], role);
        }

        if (crew.Count > 0 && enabledNeutralRoles.Count > 0)
        {
            var neutralIndex = UnityEngine.Random.Range(0, crew.Count);
            var neutralPlayer = crew[neutralIndex];
            var neutralRole = enabledNeutralRoles[
                UnityEngine.Random.Range(0, enabledNeutralRoles.Count)];

            RoleAssignment.Assign(neutralPlayer, neutralRole);
            crew.RemoveAt(neutralIndex);
        }

        for (var i = 0; i < crew.Count && enabledCrewRoles.Count > 0; i++)
        {
            var role = enabledCrewRoles[i % enabledCrewRoles.Count];
            RoleAssignment.Assign(crew[i], role);
        }

        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX initial roles assigned: {PlayerRoleRegistry.All.Count} players.");
    }

    private static List<RoleId> BuildEnabledPool(IReadOnlyList<RoleId> source)
    {
        var roles = new List<RoleId>();

        foreach (var role in source)
        {
            if (!ParadoxRoleSettings.IsEnabled(role))
                continue;

            if (PassesSpawnRoll(ParadoxRoleSettings.GetSpawnChance(role)))
                roles.Add(role);
        }

        return roles;
    }

    private static bool PassesSpawnRoll(int chancePercent)
    {
        var chance = Math.Clamp(chancePercent, 0, 100);
        return chance >= 100 || (chance > 0 && UnityEngine.Random.Range(0, 100) < chance);
    }
}
