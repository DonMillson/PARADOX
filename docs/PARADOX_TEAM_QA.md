# PARADOX + Anomaly — experimental neutral duo

## Rules
- 44 roles: PARADOX is neutral RoleId 43; Anomaly remains RoleId 30.
- The duo competes as one neutral slot and consumes two crew players.
- Both must be enabled and pass independent spawn rolls; never lone Anomaly.
- PARADOX: +8 meter on RUPTURE, 30s cooldown. Anomaly: +5 on pulse, 20s cooldown.
- Living PARADOX + meter 100% held 15s => joint victory with living Anomaly.
- Meter falling to 99% cancels the countdown.
- On death/ejection/disconnect of PARADOX, a living Anomaly becomes PARADOX
  once per match; meter drops 20 and win countdown resets.
- Teammates remain anonymous until 30 seconds, then receive their teammate's
  name and color. When nearby, a private proximity notification appears.
  After reveal, F5 displays only approximate direction every 60 seconds.
- No secret private chat. No extra offensive ability that targets teammates.

## Pending visual improvements
The proximity cue currently uses a private notification rather than custom
purple aura artwork. The F5 hint currently uses a notifier instead of a
persistent 3-second widget.

## Live QA required (NOT YET PASSED)
- Spawn odds and 2-seat pairing with host + 2 clients.
- Ability on host and remote clients; permissions, 30s cooldown.
- 100% hold and interruption at 14 seconds; 15 seconds completes win.
- Ejection/murder/disconnect succession and -20 penalty.
- Team win display on all clients; surviving partner included once.
- Delayed name/color reveal, proximity, F5 direction and reset.
- Check Among Us 19.0.0 BepInEx IL2CPP + Reactor CI400 logs.

Passing build/tests is not a live multiplayer verification.
