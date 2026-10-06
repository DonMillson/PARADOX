using Hazel;
using Paradox.Roles.Parasite;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.ParasiteInfection)]
public sealed class ParasiteInfectionRpc : PlayerCustomRpc<ParadoxPlugin, ParasiteInfectionRpc.Data>
{
    public ParasiteInfectionRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte SourcePlayerId, byte TargetPlayerId, float DurationSeconds);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data)
    {
        writer.Write(data.SourcePlayerId);
        writer.Write(data.TargetPlayerId);
        writer.Write(data.DurationSeconds);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadByte(), reader.ReadSingle());

    public override void Handle(PlayerControl player, Data data)
    {
        ParasiteRole.ApplySyncedInfection(
            data.SourcePlayerId,
            data.TargetPlayerId,
            Time.time,
            data.DurationSeconds);
    }
}
