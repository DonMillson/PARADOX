using Hazel;
using Paradox.Roles.Doppelganger;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.UseRoleAbility)]
public sealed class UseRoleAbilityRpc : PlayerCustomRpc<ParadoxPlugin, UseRoleAbilityRpc.Data>
{
    public UseRoleAbilityRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    public readonly record struct Data(byte TargetPlayerId);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data data) =>
        writer.Write(data.TargetPlayerId);

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (!AmongUsClient.Instance.AmHost || player == null)
            return;

        var target = FindPlayer(data.TargetPlayerId);
        if (target == null)
        {
            Plugin.Log.LogWarning($"Rejected role ability request from {player.PlayerId}: target {data.TargetPlayerId} not found.");
            return;
        }

        if (!DoppelgangerRole.IsDoppelganger(player.PlayerId))
        {
            Plugin.Log.LogWarning($"Rejected Doppelganger ability request from non-Doppelganger player {player.PlayerId}.");
            return;
        }

        DoppelgangerRole.TryDisguise(player, target);
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null && candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }
}
