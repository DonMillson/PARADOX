using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Dispatcher;

public static class DispatcherRole
{
    private static readonly Dictionary<byte,float> CooldownEndsAt=new();
    public static bool IsDispatcher(byte id)=>PlayerRoleRegistry.TryGet(id,out var role)&&role==RoleId.Dispatcher;
    public static float CooldownRemaining(byte id,float now)=>CooldownEndsAt.TryGetValue(id,out var end)?Math.Max(0f,end-now):0f;

    public static bool TryStatusSweep(PlayerControl source)
    {
        if(source==null||AmongUsClient.Instance==null||!AmongUsClient.Instance.AmHost||
           !IsDispatcher(source.PlayerId)||source.Data==null||source.Data.IsDead||source.Data.Disconnected||
           ParadoxEventRuntime.RoleAbilitiesBlocked||CorruptorRole.IsCorrupted(source.PlayerId))
            return false;
        var now=Time.time;if(CooldownRemaining(source.PlayerId,now)>0f)return false;
        var alive=0;var dead=0;
        foreach(var p in PlayerControl.AllPlayerControls)
        {
            if(p?.Data==null||p.Data.Disconnected)continue;
            if(p.Data.IsDead)dead++;else alive++;
        }
        if(!RoleAbilityService.Use(source,RoleId.Dispatcher))return false;
        CooldownEndsAt[source.PlayerId]=now+ParadoxRoleSettings.DispatcherCooldownSeconds;
        ApplySyncedResult(source.PlayerId,alive,dead,ParadoxGame.State.Meter,ParadoxRoleSettings.DispatcherCooldownSeconds);
        Broadcast(source.PlayerId,alive,dead,ParadoxGame.State.Meter);
        return true;
    }

    public static void ApplySyncedResult(byte id,int alive,int dead,float meter,float cooldown)
    {
        CooldownEndsAt[id]=Time.time+Math.Max(0f,cooldown);
        try
        {
            var local=PlayerControl.LocalPlayer;var hud=HudManager.Instance;
            if(local==null||local.PlayerId!=id||hud?.Notifier==null)return;
            hud.Notifier.AddDisconnectMessage(ParadoxPlugin.Localizer.Get("role.Dispatcher.feedback")
                .Replace("{alive}",alive.ToString()).Replace("{dead}",dead.ToString()).Replace("{meter}",meter.ToString("0")));
        }catch(Exception e){ParadoxPlugin.Instance.Log.LogWarning($"Dispatcher feedback failed: {e.Message}");}
    }
    private static void Broadcast(byte id,int alive,int dead,float meter)
    {
        var sender=PlayerControl.LocalPlayer;if(sender==null)return;
        Rpc<DispatcherSweepRpc>.Instance.Send(sender,new DispatcherSweepRpc.Data(id,alive,dead,meter,ParadoxRoleSettings.DispatcherCooldownSeconds,1),immediately:true);
    }
    public static void ResetRuntime()=>CooldownEndsAt.Clear();
}