using UnityEngine;

namespace Paradox.Core;

public static class ParadoxFinalEventSelector
{
    public static ParadoxFinalEventType Pick()
    {
        var value = Random.Range(0, 3);
        return (ParadoxFinalEventType)value;
    }
}
