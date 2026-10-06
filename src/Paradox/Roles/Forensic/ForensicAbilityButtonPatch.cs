using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Forensic;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class ForensicAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || !ForensicRole.IsForensic(local.PlayerId))
            return true;

        if (HudManager.Instance == null ||
            __instance != HudManager.Instance.AbilityButton)
            return true;

        var body = ForensicHudPatch.CurrentBody;
        if (body == null)
            return false;

        if (AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
        {
            ForensicRole.TryExamine(local, body);
        }
        else
        {
            Rpc<ForensicExamineRpc>.Instance.Send(
                local,
                new ForensicExamineRpc.Data(
                    local.PlayerId,
                    body.ParentId,
                    0f,
                    0,
                    0f,
                    0,
                    0),
                immediately: true);
        }

        return false;
    }
}
