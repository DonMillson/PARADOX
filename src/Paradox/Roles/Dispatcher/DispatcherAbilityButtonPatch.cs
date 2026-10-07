using HarmonyLib;using Paradox.Networking;using Reactor.Networking.Rpc;
namespace Paradox.Roles.Dispatcher;
[HarmonyPatch(typeof(AbilityButton),nameof(AbilityButton.DoClick))]
public static class DispatcherAbilityButtonPatch
{
 [HarmonyPrefix]public static bool Click(AbilityButton b)
 {
  var p=PlayerControl.LocalPlayer;if(p==null||!DispatcherRole.IsDispatcher(p.PlayerId))return true;
  if(HudManager.Instance==null||b!=HudManager.Instance.AbilityButton)return true;
  if(AmongUsClient.Instance!=null&&AmongUsClient.Instance.AmHost)DispatcherRole.TryStatusSweep(p);
  else Rpc<DispatcherSweepRpc>.Instance.Send(p,new DispatcherSweepRpc.Data(p.PlayerId,0,0,0f,0f,0),immediately:true);
  return false;
 }
}