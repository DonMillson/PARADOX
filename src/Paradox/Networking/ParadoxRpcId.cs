namespace Paradox.Networking;

public enum ParadoxRpcId : uint
{
    HandshakeHello = 0,
    SyncParadoxMeter = 1,
    AssignRole = 2,
    UseRoleAbility = 3,
    SyncRoleState = 4,
    ObserverTrace = 5,
    ParasiteInfection = 6
}
