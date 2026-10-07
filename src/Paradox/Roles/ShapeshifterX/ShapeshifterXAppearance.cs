namespace Paradox.Roles.ShapeshifterX;

public static class ShapeshifterXAppearance
{
    private static readonly Dictionary<byte, ShiftOutfit> Originals = new();

    public static bool Swap(PlayerControl source, PlayerControl target)
    {
        if (source == null ||
            target == null ||
            source.Data == null ||
            target.Data == null)
            return false;

        var sourceOutfit = Capture(source);
        var targetOutfit = Capture(target);

        Originals[source.PlayerId] = sourceOutfit;
        Originals[target.PlayerId] = targetOutfit;

        Apply(source, targetOutfit);
        Apply(target, sourceOutfit);
        return true;
    }

    public static void RestorePair(PlayerControl? source, PlayerControl? target)
    {
        if (source != null)
            Restore(source);

        if (target != null)
            Restore(target);
    }

    public static bool Restore(PlayerControl player)
    {
        if (player == null ||
            !Originals.TryGetValue(player.PlayerId, out var outfit))
            return false;

        Apply(player, outfit);
        Originals.Remove(player.PlayerId);
        return true;
    }

    public static void Reset() => Originals.Clear();

    private static ShiftOutfit Capture(PlayerControl player)
    {
        var data = player.Data;
        return new ShiftOutfit(
            data.PlayerName,
            (byte)data.DefaultOutfit.ColorId,
            data.DefaultOutfit.HatId,
            data.DefaultOutfit.SkinId,
            data.DefaultOutfit.PetId);
    }

    private static void Apply(PlayerControl player, ShiftOutfit outfit)
    {
        player.RpcSetName(outfit.PlayerName);
        player.RpcSetColor(outfit.ColorId);
        player.RpcSetHat(outfit.HatId);
        player.RpcSetSkin(outfit.SkinId);
        player.RpcSetPet(outfit.PetId);
    }

    private sealed record ShiftOutfit(
        string PlayerName,
        byte ColorId,
        string HatId,
        string SkinId,
        string PetId);
}
