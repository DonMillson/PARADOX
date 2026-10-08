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

        // Shuffle both rosters so the same lobby join order cannot determine who
        // receives each enabled special role across consecutive matches.
        Shuffle(impostors);
        Shuffle(crew);

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

        Paradox.Roles.BountyHunter.BountyHunterRole.InitializeTargetsHost();

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

        // A successful spawn roll only makes a role eligible. Randomize the
        // eligible pool before assigning roles to players; otherwise the first
        // few entries in InitialRolePool monopolize small matches.
        Shuffle(roles);
        return roles;
    }

    private static void Shuffle<T>(IList<T> items)
    {
        // Fisher-Yates: unbiased for UnityEngine.Random.Range with exclusive
        // integer upper bounds. No role is repeated until the pool is exhausted.
        for (var i = items.Count - 1; i > 0; i--)
        {
            var j = UnityEngine.Random.Range(0, i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    private static bool PassesSpawnRoll(int chancePercent)
    {
        var chance = Math.Clamp(chancePercent, 0, 100);
        return chance >= 100 || (chance > 0 && UnityEngine.Random.Range(0, 100) < chance);
    }
}
