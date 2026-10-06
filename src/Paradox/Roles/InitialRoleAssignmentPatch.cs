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

        var impostorIndex = 0;
        var crewIndex = 0;

        foreach (var player in PlayerControl.AllPlayerControls)
        {
            if (player == null || player.Data == null || player.Data.Role == null)
                continue;

            if (player.Data.Role.IsImpostor)
            {
                var role = InitialRolePool.Impostor[impostorIndex % InitialRolePool.Impostor.Count];

                if (role == RoleId.Doppelganger)
                {
                    if (!ParadoxRoleSettings.DoppelgangerEnabled ||
                        UnityEngine.Random.Range(0, 100) >= ParadoxRoleSettings.DoppelgangerSpawnChancePercent)
                    {
                        role = RoleId.Parasite;
                    }
                }

                RoleAssignment.Assign(player, role);
                impostorIndex++;
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
}
