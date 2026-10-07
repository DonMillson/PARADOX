using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.Collector;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class CollectorHudPatch
{
    public static PlayerControl? CurrentTarget { get; private set; }
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        var local=PlayerControl.LocalPlayer;
        if(local==null || __instance.AbilityButton==null) return;
        if(!CollectorRole.IsCollector(local.PlayerId)){CurrentTarget=null;return;}
        var show=local.Data!=null&&!local.Data.IsDead&&!local.Data.Disconnected&&MeetingHud.Instance==null;
        __instance.AbilityButton.ToggleVisible(show);
        if(!show){CurrentTarget=null;return;}
        CurrentTarget=FindTarget(local);
        var count=CollectorRole.GetSampleCount(local.PlayerId);
        __instance.AbilityButton.OverrideText(ParadoxPlugin.Localizer.Get("role.Collector.ability")
            .Replace("{count}",count.ToString()).Replace("{required}",ParadoxRoleSettings.CollectorRequiredSamples.ToString()));
        var remaining=CollectorRole.CooldownRemaining(local.PlayerId,Time.time);
        __instance.AbilityButton.SetCoolDown(remaining,ParadoxRoleSettings.CollectorCooldownSeconds);
        var canUse=local.CanMove&&!ParadoxEventRuntime.RoleAbilitiesBlocked&&!CorruptorRole.IsCorrupted(local.PlayerId)&&remaining<=0f&&CurrentTarget!=null;
        if(canUse)__instance.AbilityButton.SetEnabled();else __instance.AbilityButton.SetDisabled();
    }
    private static PlayerControl? FindTarget(PlayerControl source)
    {
        PlayerControl? best=null; var bestDistance=source.MaxReportDistance;
        foreach(var p in PlayerControl.AllPlayerControls)
        {
            if(p==null||p.PlayerId==source.PlayerId||p.Data==null||p.Data.IsDead||p.Data.Disconnected||CollectorRole.HasSample(source.PlayerId,p.PlayerId))continue;
            var d=Vector2.Distance(source.GetTruePosition(),p.GetTruePosition());
            if(d<=bestDistance){best=p;bestDistance=d;}
        }
        return best;
    }
}