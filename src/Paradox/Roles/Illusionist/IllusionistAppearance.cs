namespace Paradox.Roles.Illusionist;

public static class IllusionistAppearance
{
    private static readonly Dictionary<byte, IllusionistOutfit> Originals = new();

    public static bool Copy(PlayerControl victim, PlayerControl disguiseSource)
    {
        if (victim == null ||
            disguiseSource == null ||
            victim.Data == null ||
            disguiseSource.Data == null)
            return false;

        if (!Originals.ContainsKey(victim.PlayerId))
            Originals[victim.PlayerId] = Capture(victim);

        Apply(victim, Capture(disguiseSource));
        return true;
    }

    public static bool Restore(PlayerControl victim)
    {
        if (victim == null ||
            !Originals.TryGetValue(victim.PlayerId, out var original))
            return false;

        Apply(victim, original);
        Originals.Remove(victim.PlayerId);
        return true;
    }

    public static void Reset() => Originals.Clear();

    private static IllusionistOutfit Capture(PlayerControl player)
    {
        var data = player.Data;
        return new IllusionistOutfit(
            data.PlayerName,
            (byte)data.DefaultOutfit.ColorId,
            data.DefaultOutfit.HatId,
            data.DefaultOutfit.SkinId,
            data.DefaultOutfit.PetId);
    }

    private static void Apply(PlayerControl player, IllusionistOutfit outfit)
    {
        player.RpcSetName(outfit.PlayerName);
        player.RpcSetColor(outfit.ColorId);
        player.RpcSetHat(outfit.HatId);
        player.RpcSetSkin(outfit.SkinId);
        player.RpcSetPet(outfit.PetId);
    }
}

public sealed record IllusionistOutfit(
    string PlayerName,
    byte ColorId,
    string HatId,
    string SkinId,
    string PetId);
