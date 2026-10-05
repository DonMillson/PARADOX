using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Paradox.Networking;
using Reactor;

namespace Paradox;

[BepInAutoPlugin("gg.paradox.mod", "PARADOX")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class ParadoxPlugin : BasePlugin
{
    public static ParadoxClientRegistry Clients { get; } = new();

    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Clients.Clear();
        Log.LogInfo($"PARADOX v{ParadoxInfo.Version} / protocol {ParadoxInfo.ProtocolVersion} loading...");
        Harmony.PatchAll();
        Log.LogInfo("PARADOX loaded. Reactor RPC transport ready.");
    }
}
