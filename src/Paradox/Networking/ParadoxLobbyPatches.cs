using HarmonyLib;

namespace Paradox.Networking;

[HarmonyPatch]
public static class ParadoxLobbyPatches
{
    [HarmonyPatch(typeof(PlayerControl), nameof(PlayerControl.Start))]
    [HarmonyPostfix]
    public static void PlayerControlStartPostfix(PlayerControl __instance)
    {
        if (__instance == null || PlayerControl.LocalPlayer == null)
            return;

        if (__instance.PlayerId != PlayerControl.LocalPlayer.PlayerId)
            return;

        if (Paradox.Maps.ParadoxStationRuntime.Active &&
            AmongUsClient.Instance != null && AmongUsClient.Instance.AmHost)
            Paradox.Maps.ParadoxStationRuntime.TryForceExitHost();

        ParadoxPlugin.Clients.Register(
            AmongUsClient.Instance.ClientId,
            ParadoxHandshake.Local);

        ParadoxNetwork.SendHandshake(__instance);
        ParadoxPlugin.Instance.Log.LogInfo(
            $"Sent PARADOX handshake v{ParadoxInfo.Version} / protocol {ParadoxInfo.ProtocolVersion}");
    }

    [HarmonyPatch(typeof(AmongUsClient), nameof(AmongUsClient.DisconnectInternal))]
    [HarmonyPostfix]
    public static void DisconnectPostfix()
    {
        ParadoxPlugin.Clients.Clear();
        Paradox.Maps.ParadoxStationMapHudPatch.Close();
        Paradox.Maps.ParadoxStationRuntime.Reset();
    }
}
