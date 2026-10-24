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
        Paradox.Roles.ParadoxTeam.ParadoxRole.ResetRuntime();

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

        // PARADOX + Anomaly are one neutral *team* and require two crew seats.
        // Lone Anomaly cannot be picked; both roles must pass their spawn rolls.
        var candidates = enabledNeutralRoles
            .Where(role => role != RoleId.Anomaly && role != RoleId.Paradox)
            .ToList();
        if (crew.Count >= 2 &&
            enabledNeutralRoles.Contains(RoleId.Paradox) &&
            enabledNeutralRoles.Contains(RoleId.Anomaly))
            candidates.Add(RoleId.Paradox);

        if (crew.Count > 0 && candidates.Count > 0)
        {
            var choice = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            var firstIndex = UnityEngine.Random.Range(0, crew.Count);
            var first = crew[firstIndex];
            RoleAssignment.Assign(first, choice);
            crew.RemoveAt(firstIndex);

            if (choice == RoleId.Paradox)
            {
                var secondIndex = UnityEngine.Random.Range(0, crew.Count);
                var second = crew[secondIndex];
                RoleAssignment.Assign(second, RoleId.Anomaly);
                Paradox.Roles.ParadoxTeam.ParadoxRole.InitializePairHost(
                    first.PlayerId, second.PlayerId);
                crew.RemoveAt(secondIndex);
            }
        }

        // Swapper currently owns one global pair of targets per meeting.
        // Assigning two Swappers would silently reject the second player's
        // ability; keep at most one until multi-Swapper voting is supported.
        var swapperAssigned = false;
        for (var i = 0; i < crew.Count && enabledCrewRoles.Count > 0; i++)
        {
            var role = enabledCrewRoles[i % enabledCrewRoles.Count];
            if (role == RoleId.Swapper)
            {
                if (swapperAssigned)
                {
                    // Leave this player as vanilla Crewmate if Swapper is the
                    // only eligible role, otherwise pick another active role.
                    var replacement = enabledCrewRoles.FirstOrDefault(
                        candidate => candidate != RoleId.Swapper, RoleId.Swapper);
                    if (replacement == RoleId.Swapper)
                        continue;
                    role = replacement;
                }
                else
                {
                    swapperAssigned = true;
                }
            }
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
