using HarmonyLib;
using Paradox.Core;

namespace Paradox.Roles.RiftWarden;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class RiftWardenHudPatch
{
    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager hud)
    {
        var local = PlayerControl.LocalPlayer;
        if (local == null || hud.AbilityButton == null ||
            !RiftWardenRole.IsRiftWarden(local.PlayerId))
            return;

        var visible = local.Data != null && !local.Data.IsDead &&
                      !local.Data.Disconnected && MeetingHud.Instance == null;
        hud.AbilityButton.ToggleVisible(visible);
        if (!visible)
            return;

        var activeJam = ParadoxEventRuntime.RoleAbilitiesBlocked;
        var remaining = RiftWardenRole.CanBurst(local.PlayerId);
        hud.AbilityButton.OverrideText(
            ParadoxPlugin.Localizer.Get(
                remaining ? "role.RiftWarden.ability" :
                activeJam ? "role.RiftWarden.spent" : "role.RiftWarden.wait"));

        hud.AbilityButton.SetCoolDown(0f, 1f);
        if (local.CanMove && remaining)
            hud.AbilityButton.SetEnabled();
        else
            hud.AbilityButton.SetDisabled();
    }
}
