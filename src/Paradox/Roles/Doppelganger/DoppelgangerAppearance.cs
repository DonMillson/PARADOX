namespace Paradox.Roles.Doppelganger;

public static class DoppelgangerAppearance
{
    private static readonly Dictionary<byte, DoppelgangerOutfit> Originals = new();

    public static bool Copy(PlayerControl player, PlayerControl target)
    {
        if (player == null || target == null || player.Data == null || target.Data == null)
            return false;

        if (!Originals.ContainsKey(player.PlayerId))
            Originals[player.PlayerId] = Capture(player);

        Apply(player, Capture(target));
        return true;
    }

    public static bool Restore(PlayerControl player)
    {
        if (player == null || !Originals.TryGetValue(player.PlayerId, out var original))
            return false;

        Apply(player, original);
        Originals.Remove(player.PlayerId);
        return true;
    }

    public static void Reset() => Originals.Clear();

    private static DoppelgangerOutfit Capture(PlayerControl player)
    {
        var data = player.Data;
        return new DoppelgangerOutfit(
            data.PlayerName,
            (byte)data.ColorId,
            data.HatId,
            data.SkinId,
            data.PetId);
    }

    private static void Apply(PlayerControl player, DoppelgangerOutfit outfit)
    {
        player.RpcSetName(outfit.PlayerName);
        player.RpcSetColor(outfit.ColorId);
        player.RpcSetHat(outfit.HatId);
        player.RpcSetSkin(outfit.SkinId);
        player.RpcSetPet(outfit.PetId);
    }
}
