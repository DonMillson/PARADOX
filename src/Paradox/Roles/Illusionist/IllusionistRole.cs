using Paradox.Core;
using Paradox.Networking;
using Paradox.Roles.Corruptor;
using Paradox.Settings;
using Reactor.Networking.Rpc;
using UnityEngine;

namespace Paradox.Roles.Illusionist;

public static class IllusionistRole
{
    private static readonly Dictionary<byte, float> CooldownEndsAt = new();
    private static readonly Dictionary<byte, IllusionState> ActiveIllusions = new();

    public static bool IsIllusionist(byte playerId) =>
        PlayerRoleRegistry.TryGet(playerId, out var role) &&
        role == RoleId.Illusionist;

    public static float CooldownRemaining(byte playerId, float now)
    {
        if (!CooldownEndsAt.TryGetValue(playerId, out var end))
            return 0f;

        return Math.Max(0f, end - now);
    }

    public static bool HasActiveIllusion(byte illusionistPlayerId, float now)
    {
        if (!ActiveIllusions.TryGetValue(illusionistPlayerId, out var state))
            return false;

        return now < state.EndsAt;
    }

    public static bool TryCast(PlayerControl source, PlayerControl victim)
    {
        if (source == null ||
            victim == null ||
            AmongUsClient.Instance == null ||
            !AmongUsClient.Instance.AmHost)
            return false;

        if (!IsIllusionist(source.PlayerId) ||
            source.Data == null ||
            source.Data.IsDead ||
            source.Data.Disconnected ||
            victim.Data == null ||
            victim.Data.IsDead ||
            victim.Data.Disconnected ||
            source.PlayerId == victim.PlayerId ||
            ParadoxEventRuntime.RoleAbilitiesBlocked ||
            CorruptorRole.IsCorrupted(source.PlayerId))
            return false;

        var now = Time.time;
        if (CooldownRemaining(source.PlayerId, now) > 0f ||
            HasActiveIllusion(source.PlayerId, now))
            return false;

        var distance = Vector2.Distance(
            source.GetTruePosition(),
            victim.GetTruePosition());

        if (distance > source.MaxReportDistance)
            return false;

        var disguise = PickDisguiseSource(source, victim);
        if (disguise == null)
            return false;

        if (!IllusionistAppearance.Copy(victim, disguise))
            return false;

        if (!RoleAbilityService.Use(source, RoleId.Illusionist))
        {
            IllusionistAppearance.Restore(victim);
            return false;
        }

        ApplySyncedState(
            source.PlayerId,
            victim.PlayerId,
            disguise.PlayerId,
            ParadoxRoleSettings.IllusionistDurationSeconds,
            ParadoxRoleSettings.IllusionistCooldownSeconds,
            active: true);

        BroadcastState(
            source.PlayerId,
            victim.PlayerId,
            disguise.PlayerId,
            ParadoxRoleSettings.IllusionistDurationSeconds,
            ParadoxRoleSettings.IllusionistCooldownSeconds,
            active: true);

        return true;
    }

    public static void UpdateHost()
    {
        if (AmongUsClient.Instance == null || !AmongUsClient.Instance.AmHost)
            return;

        var now = Time.time;

        foreach (var pair in ActiveIllusions.ToArray())
        {
            var sourceId = pair.Key;
            var state = pair.Value;
            var victim = FindPlayer(state.VictimPlayerId);

            if (victim == null ||
                victim.Data == null ||
                victim.Data.IsDead ||
                now >= state.EndsAt)
            {
                if (victim != null)
                    IllusionistAppearance.Restore(victim);

                ActiveIllusions.Remove(sourceId);

                BroadcastState(
                    sourceId,
                    state.VictimPlayerId,
                    byte.MaxValue,
                    0f,
                    CooldownRemaining(sourceId, now),
                    active: false);
            }
        }
    }

    public static void ApplySyncedState(
        byte sourcePlayerId,
        byte victimPlayerId,
        byte disguisePlayerId,
        float durationSeconds,
        float cooldownSeconds,
        bool active)
    {
        if (active)
        {
            ActiveIllusions[sourcePlayerId] = new IllusionState(
                victimPlayerId,
                disguisePlayerId,
                Time.time + Math.Max(0f, durationSeconds));

            CooldownEndsAt[sourcePlayerId] =
                Time.time + Math.Max(0f, cooldownSeconds);

            ShowFeedback(sourcePlayerId, victimPlayerId);
        }
        else
        {
            ActiveIllusions.Remove(sourcePlayerId);
        }
    }

    private static PlayerControl? PickDisguiseSource(
        PlayerControl source,
        PlayerControl victim)
    {
        var candidates = new List<PlayerControl>();

        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate == null ||
                candidate.PlayerId == source.PlayerId ||
                candidate.PlayerId == victim.PlayerId ||
                candidate.Data == null ||
                candidate.Data.IsDead ||
                candidate.Data.Disconnected)
                continue;

            candidates.Add(candidate);
        }

        if (candidates.Count == 0)
            return null;

        return candidates[UnityEngine.Random.Range(0, candidates.Count)];
    }

    private static void ShowFeedback(byte sourcePlayerId, byte victimPlayerId)
    {
        try
        {
            var local = PlayerControl.LocalPlayer;
            var hud = HudManager.Instance;
            if (local == null || hud == null || hud.Notifier == null)
                return;

            if (local.PlayerId == sourcePlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Illusionist.feedback.source"));
            }
            else if (local.PlayerId == victimPlayerId)
            {
                hud.Notifier.AddDisconnectMessage(
                    ParadoxPlugin.Localizer.Get("role.Illusionist.feedback.target"));
            }
        }
        catch (Exception e)
        {
            ParadoxPlugin.Instance.Log.LogWarning(
                $"Illusionist feedback failed: {e.Message}");
        }
    }

    private static void BroadcastState(
        byte sourcePlayerId,
        byte victimPlayerId,
        byte disguisePlayerId,
        float durationSeconds,
        float cooldownSeconds,
        bool active)
    {
        var sender = PlayerControl.LocalPlayer;
        if (sender == null)
            return;

        Rpc<IllusionistMirageRpc>.Instance.Send(
            sender,
            new IllusionistMirageRpc.Data(
                sourcePlayerId,
                victimPlayerId,
                disguisePlayerId,
                durationSeconds,
                cooldownSeconds,
                active ? (byte)1 : (byte)0),
            immediately: true);
    }

    private static PlayerControl? FindPlayer(byte playerId)
    {
        foreach (var candidate in PlayerControl.AllPlayerControls)
        {
            if (candidate != null && candidate.PlayerId == playerId)
                return candidate;
        }

        return null;
    }

    public static void ResetRuntime()
    {
        CooldownEndsAt.Clear();
        ActiveIllusions.Clear();
        IllusionistAppearance.Reset();
    }

    private sealed record IllusionState(
        byte VictimPlayerId,
        byte DisguisePlayerId,
        float EndsAt);
}
