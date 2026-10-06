using UnityEngine;

namespace Paradox.Roles.Guardian;

public static class GuardianTargeting
{
    public static PlayerControl? FindClosestValidTarget(PlayerControl source, float maxDistance)
    {
        if (source == null || source.Data == null || source.Data.IsDead)
            return null;

        PlayerControl? closest = null;
        var closestDistance = Math.Max(0f, maxDistance);
        var sourcePosition = source.GetTruePosition();

        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (!IsValidTarget(source, candidate))
                continue;

            var distance = Vector2.Distance(sourcePosition, candidate.GetTruePosition());
            if (distance > closestDistance)
                continue;

            closest = candidate;
            closestDistance = distance;
        }

        return closest;
    }

    public static bool IsValidTarget(PlayerControl source, PlayerControl? target) =>
        source != null &&
        target != null &&
        target.PlayerId != source.PlayerId &&
        target.Data != null &&
        !target.Data.IsDead &&
        !target.Data.Disconnected &&
        !GuardianRole.IsProtected(target.PlayerId);
}
