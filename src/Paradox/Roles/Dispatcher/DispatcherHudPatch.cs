using HarmonyLib;using Paradox.Core;using Paradox.Roles.Corruptor;using Paradox.Settings;using UnityEngine;
namespace Paradox.Roles.Dispatcher;
[HarmonyPatch(typeof(HudManager),nameof(HudManager.Update))]
public static class DispatcherHudPatch
{
 [HarmonyPostfix]public static void Update(HudManager h)
 {
  var p=PlayerControl.LocalPlayer;if(p==null||h.AbilityButton==null)return;if(!DispatcherRole.IsDispatcher(p.PlayerId))return;
  var show=p.Data!=null&&!p.Data.IsDead&&!p.Data.Disconnected&&MeetingHud.Instance==null;h.AbilityButton.ToggleVisible(show);if(!show)return;
  h.AbilityButton.OverrideText(ParadoxPlugin.Localizer.Get("role.Dispatcher.ability"));
  var rem=DispatcherRole.CooldownRemaining(p.PlayerId,Time.time);h.AbilityButton.SetCoolDown(rem,ParadoxRoleSettings.DispatcherCooldownSeconds);
  if(p.CanMove&&!ParadoxEventRuntime.RoleAbilitiesBlocked&&!CorruptorRole.IsCorrupted(p.PlayerId)&&rem<=0f)h.AbilityButton.SetEnabled();else h.AbilityButton.SetDisabled();
 }
}