namespace Paradox.Roles.Witness;

public enum WitnessClueType
{
    BodyAge,
    NearbyActivity
}

public sealed record WitnessClue(
    WitnessClueType Type,
    string LocalizationKey,
    string Value);
