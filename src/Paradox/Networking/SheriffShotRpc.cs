using Hazel;
using Paradox.Roles.Sheriff;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace Paradox.Networking;

[RegisterCustomRpc((uint)ParadoxRpcId.SheriffShot)]
public sealed class SheriffShotRpc : PlayerCustomRpc<ParadoxPlugin, SheriffShotRpc.Data>
{
    public SheriffShotRpc(ParadoxPlugin plugin, uint id) : base(plugin, id) { }

    // Accepted=0 client request; Accepted=1 host result and cooldown.
    public readonly record struct Data(
        byte ShooterId, byte TargetId, float Cooldown, byte Correct, byte Accepted);

    public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

    public override void Write(MessageWriter writer, Data value)
    {
        writer.Write(value.ShooterId);
        writer.Write(value.TargetId);
        writer.Write(value.Cooldown);
        writer.Write(value.Correct);
        writer.Write(value.Accepted);
    }

    public override Data Read(MessageReader reader) =>
        new(reader.ReadByte(), reader.ReadByte(), reader.ReadSingle(),
            reader.ReadByte(), reader.ReadByte());

    public override void Handle(PlayerControl player, Data data)
    {
        if (AmongUsClient.Instance == null || player == null)
            return;

        if (AmongUsClient.Instance.AmHost)
        {
            if (player.OwnerId == AmongUsClient.Instance.HostId ||
                data.Accepted != 0 || data.ShooterId != player.PlayerId ||
                !SheriffRole.IsSheriff(player.PlayerId))
                return;

            var target = SheriffRole.ClosestTarget(player);
            // Validate the exact submitted target. Never replace it with the
            // nearest target after the request was sent.
            if (target == null || target.PlayerId != data.TargetId)
            {
                foreach (var candidate in PlayerControl.AllPlayerControls)
                    if (candidate != null && candidate.PlayerId == data.TargetId)
                    {
                        target = candidate;
                        break;
                    }
            }

            if (target != null && target.PlayerId == data.TargetId)
                SheriffRole.TryShoot(player, target);
            return;
        }

        if (player.OwnerId != AmongUsClient.Instance.HostId ||
            data.Accepted != 1 || data.Correct > 1 ||
            !SheriffRole.IsSheriff(data.ShooterId))
            return;

        SheriffRole.ApplySyncedResult(data.ShooterId, data.Cooldown, data.Correct == 1);
    }
}
