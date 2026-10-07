using HarmonyLib;
using Paradox.Networking;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.Collector;

[HarmonyPatch(typeof(AbilityButton), nameof(AbilityButton.DoClick))]
public static class CollectorAbilityButtonPatch
{
    [HarmonyPrefix]
    public static bool DoClickPrefix(AbilityButton __instance)
    {
        var local=PlayerControl.LocalPlayer;
        if(local==null||!CollectorRole.IsCollector(local.PlayerId))return true;
        if(HudManager.Instance==null||__instance!=HudManager.Instance.AbilityButton)return true;
        var target=CollectorHudPatch.CurrentTarget;
        if(target==null)return false;
        if(AmongUsClient.Instance!=null&&AmongUsClient.Instance.AmHost) CollectorRole.TryCollect(local,target);
        else Rpc<CollectorSampleRpc>.Instance.Send(local,new CollectorSampleRpc.Data(local.PlayerId,target.PlayerId,0,0f,0),immediately:true);
        return false;
    }
}