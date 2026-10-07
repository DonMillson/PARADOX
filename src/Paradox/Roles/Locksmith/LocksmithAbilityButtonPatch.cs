using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Locksmith;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class LocksmithAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;

        if (local == null ||
            !LocksmithRole.IsLocksmith(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var doorIndex = LocksmithHudPatch.CurrentDoorIndex;
        if (doorIndex < 0)
            return false;

        if (AmongUsClient.Instance != null &&
            AmongUsClient.Instance.AmHost)
        {
            LocksmithRole.TryOpenDoor(local, doorIndex);
        }
        else
        {
            Rpc<LocksmithOpenDoorRpc>.Instance.Send(
                local,
                new LocksmithOpenDoorRpc.Data(
                    local.PlayerId,
                    doorIndex,
                    0f,
                    0),
                immediately: true);
        }

        return false;
    }
}
