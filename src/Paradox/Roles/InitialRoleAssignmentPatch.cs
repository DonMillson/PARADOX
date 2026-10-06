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
        var impostorIndex = 0;
        var crewIndex = 0;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Role == null)
                continue;

            if (player.Data.Role.IsImpostor)
            {
                if (enabledImpostorRoles.Count > 0)
                {
                    var role = enabledImpostorRoles[impostorIndex % enabledImpostorRoles.Count];
                    RoleAssignment.Assign(player, role);
                    impostorIndex++;
                }

                continue;
            }

            if (enabledCrewRoles.Count > 0)
            {
                var crewRole = enabledCrewRoles[crewIndex % enabledCrewRoles.Count];
                RoleAssignment.Assign(player, crewRole);
                crewIndex++;
            }
        }

        // First playable pass keeps vanilla factions intact.
        // Anomaly becomes eligible once neutral win conditions are implemented.
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
