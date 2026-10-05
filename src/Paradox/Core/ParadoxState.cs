namespace Paradox.Core;

public sealed class ParadoxState
{
    public float Meter { get; private set; }

    public void Reset() => Meter = 0f;

    public void Add(float amount)
    {
        Meter = Math.Clamp(Meter + amount, 0f, 100f);
    }
}
