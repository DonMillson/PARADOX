namespace Paradox.Networking;

public enum ParadoxMessageType : byte
{
    HandshakeHello = 1,
    HandshakeAck = 2,
    SyncParadoxMeter = 10,
    AssignRole = 20,
    UseRoleAbility = 21,
    SyncRoleState = 22
}
