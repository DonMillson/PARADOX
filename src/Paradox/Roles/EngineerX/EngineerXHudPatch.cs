using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;
namespace Paradox.Roles.EngineerX;
[HarmonyPatch(typeof(HudManager),nameof(HudManager.Update))]
public static class EngineerXHudPatch
{
 [HarmonyPostfix] public static void HudUpdatePostfix(HudManager h)
 {
  var p=PlayerControl.LocalPlayer;if(p==null||h.AbilityButton==null)return;
  if(!EngineerXRole.IsEngineerX(p.PlayerId))return;
  var show=p.Data!=null&&!p.Data.IsDead&&!p.Data.Disconnected&&MeetingHud.Instance==null;
  h.AbilityButton.ToggleVisible(show);if(!show)return;
  h.AbilityButton.OverrideText(ParadoxPlugin.Localizer.Get("role.EngineerX.ability"));
  var rem=EngineerXRole.CooldownRemaining(p.PlayerId,Time.time);h.AbilityButton.SetCoolDown(rem,ParadoxRoleSettings.EngineerXCooldownSeconds);
  if(p.CanMove&&!ParadoxEventRuntime.RoleAbilitiesBlocked&&!CorruptorRole.IsCorrupted(p.PlayerId)&&rem<=0f)h.AbilityButton.SetEnabled();else h.AbilityButton.SetDisabled();
 }
}