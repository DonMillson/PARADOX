using Hazel;
using ParadoxEngineerXRole=Paradox.Roles.EngineerX.EngineerXRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
namespace Paradox.Networking;
[RegisterCustomRpc((uint)ParadoxRpcId.EngineerXRepair)]
public sealed class EngineerXRepairRpc:PlayerCustomRpc<ParadoxPlugin,EngineerXRepairRpc.Data>
{
 public EngineerXRepairRpc(ParadoxPlugin plugin,uint id):base(plugin,id){}
 public readonly record struct Data(byte PlayerId,float CooldownSeconds,byte Accepted);
 public override RpcLocalHandling LocalHandling=>RpcLocalHandling.None;
 public override void Write(MessageWriter w,Data d){w.Write(d.PlayerId);w.Write(d.CooldownSeconds);w.Write(d.Accepted);}
 public override Data Read(MessageReader r)=>new(r.ReadByte(),r.ReadSingle(),r.ReadByte());
 public override void Handle(PlayerControl player,Data d)
 {
  if(AmongUsClient.Instance==null||player==null)return;
  if(AmongUsClient.Instance.AmHost)
  {
   if(player.OwnerId==AmongUsClient.Instance.HostId)return;
   if(d.Accepted!=0||d.PlayerId!=player.PlayerId||!ParadoxEngineerXRole.IsEngineerX(player.PlayerId)){Plugin.Log.LogWarning($"Rejected Engineer X request from player {player.PlayerId}.");return;}
   ParadoxEngineerXRole.TryEmergencyRepair(player);return;
  }
  if(player.OwnerId!=AmongUsClient.Instance.HostId||d.Accepted!=1||!ParadoxEngineerXRole.IsEngineerX(d.PlayerId))return;
  ParadoxEngineerXRole.ApplySyncedState(d.PlayerId,d.CooldownSeconds);
 }
}