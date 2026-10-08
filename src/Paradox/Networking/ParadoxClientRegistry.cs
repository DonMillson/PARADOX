namespace Paradox.Networking;

public sealed class ParadoxClientRegistry
{
    private readonly Dictionary<int, ParadoxHandshake> _clients = new();

    public IReadOnlyDictionary<int, ParadoxHandshake> Clients => _clients;

    public void Register(int clientId, ParadoxHandshake handshake) =>
        _clients[clientId] = handshake;

    public void Remove(int clientId) => _clients.Remove(clientId);

    public void Clear() => _clients.Clear();

    public ClientCompatibility GetCompatibility(int clientId)
    {
        if (!_clients.TryGetValue(clientId, out var remote))
            return ClientCompatibility.MissingParadox;

        var local = ParadoxHandshake.Local;
        if (local.ProtocolVersion != remote.ProtocolVersion)
            return ClientCompatibility.ProtocolMismatch;

        return local.IsCompatibleWith(remote)
            ? ClientCompatibility.Compatible
            : ClientCompatibility.BuildMismatch;
    }
}
