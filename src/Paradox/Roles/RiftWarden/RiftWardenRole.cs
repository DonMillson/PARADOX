using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Reactor.Networking.Rpc;

namespace Paradox.Roles.RiftWarden;

/// <summary>
/// PARADOX-exclusive global emergency ability. Once per match a Crewmate
/// can shatter an active Paradox ability jam for the entire lobby.
/// It does not reduce instability or rewind threshold progression.
/// </summary>
public static class RiftWardenRole
{
    private static readonly HashSet<byte> Spent = new();

    public static bool IsRiftWarden(byte id) =>
        PlayerRoleRegistry.TryGet(id, out var role) && role == RoleId.RiftWarden;

    public static bool CanBurst(byte id) =>
        IsRiftWarden(id) && !Spent.Contains(id) &&
        ParadoxEventRuntime.RoleAbilitiesBlocked;

    public static bool TryBurst(PlayerControl source)
    {
        if (source == null || AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost ||
            source.Data == null || source.Data.IsDead || source.Data.Disconnected ||
            !source.CanMove || MeetingHud.Instance != null ||
            CorruptorRole.IsCorrupted(source.PlayerId) ||
            !CanBurst(source.PlayerId))
            return false;

        // The shared service explicitly exempts ONLY RiftWarden from the
        // Paradox jam check, but Corruptor/meeting/role validations still apply.
        if (!RoleAbilityService.Use(source, RoleId.RiftWarden))
            return false;

        ApplySyncedBurst(source.PlayerId);
        if (PlayerControl.LocalPlayer != null)
            Rpc<RiftWardenBurstRpc>.Instance.Send(PlayerControl.LocalPlayer,
                new RiftWardenBurstRpc.Data(source.PlayerId, 1), immediately: true);

        return true;
    }

    public static void ApplySyncedBurst(byte id)
    {
        if (!Spent.Add(id))
            return;

        ParadoxEventRuntime.TryDispelJam();
        try
        {
            if (HudManager.Instance?.Notifier != null)
                HudManager.Instance.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.RiftWarden.success"));
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Rift Warden notification failed: {e.Message}");
        }
    }

    public static void ResetRuntime() => Spent.Clear();
}
