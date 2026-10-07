using HarmonyLib;using Paradox.Networking;using Reactor.Networking.Rpc;
namespace Paradox.Roles.EngineerX;
[HarmonyPatch(typeof(AbilityButton),nameof(AbilityButton.DoClick))]
public static class EngineerXAbilityButtonPatch
{
 [HarmonyPrefix] public static bool DoClickPrefix(AbilityButton b)
 {
  var p=PlayerControl.LocalPlayer;if(p==null||!EngineerXRole.IsEngineerX(p.PlayerId))return true;
  if(HudManager.Instance==null||b!=HudManager.Instance.AbilityButton)return true;
  if(AmongUsClient.Instance!=null&&AmongUsClient.Instance.AmHost)EngineerXRole.TryEmergencyRepair(p);
  else Rpc<EngineerXRepairRpc>.Instance.Send(p,new EngineerXRepairRpc.Data(p.PlayerId,0f,0),immediately:true);
  return false;
 }
}