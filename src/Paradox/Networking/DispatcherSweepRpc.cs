using Hazel;
using ParadoxDispatcherRole=Paradox.Roles.Dispatcher.DispatcherRole;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
namespace Paradox.Networking;
[RegisterCustomRpc((uint)ParadoxRpcId.DispatcherSweep)]
public sealed class DispatcherSweepRpc:PlayerCustomRpc<ParadoxPlugin,DispatcherSweepRpc.Data>
{
 public DispatcherSweepRpc(ParadoxPlugin plugin,uint id):base(plugin,id){}
 public readonly record struct Data(byte PlayerId,int Alive,int Dead,float Meter,float CooldownSeconds,byte Accepted);
 public override RpcLocalHandling LocalHandling=>RpcLocalHandling.None;
 public override void Write(MessageWriter w,Data d){w.Write(d.PlayerId);w.Write(d.Alive);w.Write(d.Dead);w.Write(d.Meter);w.Write(d.CooldownSeconds);w.Write(d.Accepted);}
 public override Data Read(MessageReader r)=>new(r.ReadByte(),r.ReadInt32(),r.ReadInt32(),r.ReadSingle(),r.ReadSingle(),r.ReadByte());
 public override void Handle(PlayerControl p,Data d)
 {
  if(AmongUsClient.Instance==null||p==null)return;
  if(AmongUsClient.Instance.AmHost)
  {
   if(p.OwnerId==AmongUsClient.Instance.HostId)return;
   if(d.Accepted!=0||d.PlayerId!=p.PlayerId||!ParadoxDispatcherRole.IsDispatcher(p.PlayerId)){Plugin.Log.LogWarning($"Rejected Dispatcher request from player {p.PlayerId}.");return;}
   ParadoxDispatcherRole.TryStatusSweep(p);return;
  }
  if(p.OwnerId!=AmongUsClient.Instance.HostId||d.Accepted!=1||!ParadoxDispatcherRole.IsDispatcher(d.PlayerId))return;
  ParadoxDispatcherRole.ApplySyncedResult(d.PlayerId,d.Alive,d.Dead,d.Meter,d.CooldownSeconds);
 }
}