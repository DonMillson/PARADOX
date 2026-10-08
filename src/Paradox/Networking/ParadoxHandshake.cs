namespace Paradox.Networking;

public sealed record ParadoxHandshake(
    string ModName,
    string ModVersion,
    int ProtocolVersion)
{
    public static ParadoxHandshake Local =>
        new(ParadoxInfo.Name, ParadoxInfo.Version, ParadoxInfo.ProtocolVersion);

    public bool IsCompatibleWith(ParadoxHandshake other) =>
        string.Equals(ModName, other.ModName, StringComparison.Ordinal) &&
        ProtocolVersion == other.ProtocolVersion &&
        string.Equals(ModVersion, other.ModVersion, StringComparison.Ordinal);
}
