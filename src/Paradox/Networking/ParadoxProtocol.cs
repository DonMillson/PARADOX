namespace Paradox.Networking;

public static class ParadoxProtocol
{
    public const int Version = 8;

    public static bool Supports(ParadoxMessageType type) =>
        Enum.IsDefined(typeof(ParadoxMessageType), type);
}
