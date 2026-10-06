namespace Paradox.Networking;

public enum ParadoxRpcId : uint
{
    HandshakeHello = 0,
    SyncParadoxMeter = 1,
    AssignRole = 2,
    UseRoleAbility = 3,
    SyncRoleState = 4,
    ObserverTrace = 5,
    ParasiteInfection = 6,
    SyncRoleSetting = 7,
    CleanerCleanBody = 8,
    TriggerParadoxFinalEvent = 9,
    DoppelgangerState = 10,
    AnomalyPulse = 11,
    GuardianProtect = 12,
    StabilizerPulse = 13,
    MedicScan = 14,
    TrackerTrack = 15
}
