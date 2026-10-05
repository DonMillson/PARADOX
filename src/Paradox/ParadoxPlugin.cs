using BepInEx;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Reactor;

namespace Paradox;

[BepInAutoPlugin("gg.paradox.mod", "PARADOX")]
[BepInProcess("Among Us.exe")]
[BepInDependency(ReactorPlugin.Id)]
public partial class ParadoxPlugin : BasePlugin
{
    public Harmony Harmony { get; } = new(Id);

    public override void Load()
    {
        Log.LogInfo($"PARADOX v{ParadoxInfo.Version} loading...");
        Harmony.PatchAll();
        Log.LogInfo("PARADOX loaded.");
    }
}
