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

        var enabledImpostorRoles = BuildEnabledImpostorPool();
        var impostorIndex = 0;
        var crewIndex = 0;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Role == null)
                continue;

            if (player.Data.Role.IsImpostor)
            {
                // If every PARADOX impostor role is disabled or fails its spawn roll,
                // leave this player as a vanilla impostor instead of forcing a disabled role.
                if (enabledImpostorRoles.Count > 0)
                {
                    var role = enabledImpostorRoles[impostorIndex % enabledImpostorRoles.Count];
                    RoleAssignment.Assign(player, role);
                    impostorIndex++;
                }

                continue;
            }

            var crewRole = InitialRolePool.Crewmate[crewIndex % InitialRolePool.Crewmate.Count];
            RoleAssignment.Assign(player, crewRole);
            crewIndex++;
        }

        // First playable pass keeps vanilla factions intact.
        // Anomaly becomes eligible once neutral win conditions are implemented.
        ParadoxPlugin.Instance.Log.LogInfo(
            $"PARADOX initial roles assigned: {PlayerRoleRegistry.All.Count} players.");
    }

    private static List<RoleId> BuildEnabledImpostorPool()
    {
        var roles = new List<RoleId>();

        if (ParadoxRoleSettings.DoppelgangerEnabled &&
            PassesSpawnRoll(ParadoxRoleSettings.DoppelgangerSpawnChancePercent))
        {
            roles.Add(RoleId.Doppelganger);
        }

        if (ParadoxRoleSettings.ParasiteEnabled &&
            PassesSpawnRoll(ParadoxRoleSettings.ParasiteSpawnChancePercent))
        {
            roles.Add(RoleId.Parasite);
        }

        return roles;
    }

    private static bool PassesSpawnRoll(int chancePercent)
    {
        var chance = Math.Clamp(chancePercent, 0, 100);
        return chance >= 100 || (chance > 0 && UnityEngine.Random.Range(0, 100) < chance);
    }
}
