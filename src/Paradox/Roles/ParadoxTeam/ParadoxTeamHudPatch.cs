using HarmonyLib;
using Paradox.Core;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using UnityEngine;

namespace Paradox.Roles.ParadoxTeam;

[HarmonyPatch(typeof(HudManager), nameof(HudManager.Update))]
public static class ParadoxTeamHudPatch
{
    private static float _assignedAt = -1f;
    private static bool _introShown;
    private static bool _revealed;
    private static bool _near;
    private static float _nextDirectionAt;
    private static byte _localId = byte.MaxValue;
    private static RoleId _lastRole = RoleId.Paradox;

    [HarmonyPostfix]
    public static void HudUpdatePostfix(HudManager __instance)
    {
        ParadoxRole.UpdateHost();
        var local = PlayerControl.LocalPlayer;
        if (local == null ||
            !PlayerRoleRegistry.TryGet(local.PlayerId, out var role) ||
            (role != RoleId.Paradox && role != RoleId.Anomaly) ||
            local.Data == null || local.Data.IsDead || local.Data.Disconnected)
            return;

        if (_localId != local.PlayerId || _assignedAt < 0f || _lastRole != role)
        {
            _localId = local.PlayerId;
            _lastRole = role;
            _assignedAt = Time.time;
            _introShown = false;
            _revealed = false;
            _near = false;
            _nextDirectionAt = 0f;
        }

        if (!_introShown)
        {
            _introShown = true;
            Notify(__instance, "role.Paradox.hidden");
        }

        var partner = ParadoxRole.FindAliveRole(role == RoleId.Paradox
            ? RoleId.Anomaly : RoleId.Paradox);

        if (!_revealed && Time.time - _assignedAt >= ParadoxTeamRules.RevealSeconds &&
            partner != null && partner.Data != null)
        {
            _revealed = true;
            NotifyText(__instance, ParadoxPlugin.Localizer.Get("role.Paradox.revealed")
                .Replace("{player}", partner.Data.PlayerName)
                .Replace("{color}", partner.Data.DefaultOutfit.ColorId.ToString()));
        }

        if (_revealed && partner != null)
        {
            var current = local.GetTruePosition();
            var target = partner.GetTruePosition();
            var distance = Vector2.Distance(current, target);
            if (distance <= 2.5f && !_near)
            {
                _near = true;
                Notify(__instance, "role.Paradox.near");
            }
            else if (distance > 3.5f)
                _near = false;

            if (MeetingHud.Instance == null && Input.GetKeyDown(KeyCode.F5) &&
                Time.time >= _nextDirectionAt)
            {
                _nextDirectionAt = Time.time + ParadoxTeamRules.DirectionCooldownSeconds;
                var diff = target - current;
                var direction = Mathf.Abs(diff.x) >= Mathf.Abs(diff.y)
                    ? (diff.x >= 0f ? "E →" : "W ←")
                    : (diff.y >= 0f ? "N ↑" : "S ↓");
                NotifyText(__instance, ParadoxPlugin.Localizer.Get("role.Paradox.direction")
                    .Replace("{direction}", direction));
            }
        }

        if (role != RoleId.Paradox || __instance.AbilityButton == null)
            return;
        var visible = MeetingHud.Instance == null;
        __instance.AbilityButton.ToggleVisible(visible);
        if (!visible)
            return;
        __instance.AbilityButton.OverrideText(ParadoxPlugin.Localizer.Get("role.Paradox.ability"));
        var remaining = ParadoxRole.CooldownRemaining(local.PlayerId, Time.time);
        __instance.AbilityButton.SetCoolDown(
            remaining, ParadoxRoleSettings.ParadoxRuptureCooldownSeconds);
        if (local.CanMove && !ParadoxEventRuntime.RoleAbilitiesBlocked &&
            !CorruptorRole.IsCorrupted(local.PlayerId) &&
            ParadoxGame.State.Meter < 100f && remaining <= 0f)
            __instance.AbilityButton.SetEnabled();
        else
            __instance.AbilityButton.SetDisabled();
    }

    private static void Notify(HudManager hud, string key) =>
        NotifyText(hud, ParadoxPlugin.Localizer.Get(key));

    private static void NotifyText(HudManager hud, string message)
    {
        try
        {
            if (hud.Notifier != null)
                hud.Notifier.AddDisconnectMessage(message);
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning("PARADOX team HUD: " + e.Message);
        }
    }

    public static void ResetLocal()
    {
        _assignedAt = -1f;
        _introShown = false;
        _revealed = false;
        _near = false;
        _nextDirectionAt = 0f;
        _localId = byte.MaxValue;
    }
}
