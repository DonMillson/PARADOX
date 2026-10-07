using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.EngineerX;

public static class EngineerXRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    public static bool IsEngineerX(byte id) => PlayerRoleRegistry.TryGet(id,out var role)&&role==RoleId.EngineerX;
    public static float CooldownRemaining(byte id,float now)=>CooldownEndsAt.TryGetValue(id,out var end)?Math.Max(0f,end-now):0f;

    public static bool TryEmergencyRepair(PlayerControl source)
    {
        if(source==null||AmongUsClient.Instance==null||!AmongUsClient.Instance.AmHost||
           !IsEngineerX(source.PlayerId)||source.Data==null||source.Data.IsDead||source.Data.Disconnected||
           ParadoxEventRuntime.RoleAbilitiesBlocked||CorruptorRole.IsCorrupted(source.PlayerId))
            return false;
        var now=Time.time;
        if(CooldownRemaining(source.PlayerId,now)>0f)return false;
        if(!RoleAbilityService.Use(source,RoleId.EngineerX))return false;

        // MVP: an Engineer X stabilizes the emergency layer immediately.
        // System-specific repair hooks stay isolated here for later map extensions.
        CooldownEndsAt[source.PlayerId]=now+ParadoxRoleSettings.EngineerXCooldownSeconds;
        if(ParadoxGame.State.Meter>0f)
            ParadoxGame.AddMeter(-Math.Min(ParadoxRoleSettings.EngineerXStabilizeAmount,ParadoxGame.State.Meter));

        ApplySyncedState(source.PlayerId,ParadoxRoleSettings.EngineerXCooldownSeconds);
        BroadcastAccepted(source.PlayerId);
        return true;
    }

    public static void ApplySyncedState(byte id,float cooldown)
    {
        CooldownEndsAt[id]=Time.time+Math.Max(0f,cooldown);
        try
        {
            var local=PlayerControl.LocalPlayer; var hud=HudManager.Instance;
            if(local!=null&&local.PlayerId==id&&hud?.Notifier!=null)
                hud.Notifier.AddDisconnectMessage(ParadoxPlugin.Localizer.Get("role.EngineerX.feedback")
                    .Replace("{amount}",ParadoxRoleSettings.EngineerXStabilizeAmount.ToString("0.#")));
        } catch(Exception e){ParadoxPlugin.Instance.Log.LogWarning($"Engineer X feedback failed: {e.Message}");}
    }

    private static void BroadcastAccepted(byte id)
    {
        var sender=PlayerControl.LocalPlayer;if(sender==null)return;
        Rpc<EngineerXRepairRpc>.Instance.Send(sender,new EngineerXRepairRpc.Data(id,ParadoxRoleSettings.EngineerXCooldownSeconds,1),immediately:true);
    }
    public static void ResetRuntime(){CooldownEndsAt.Clear();}
}
