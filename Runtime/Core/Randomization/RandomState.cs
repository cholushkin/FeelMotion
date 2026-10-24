namespace FeelMotion;

/// <summary>
/// Small deterministic RNG stream, independent of UnityEngine.Random's global state — so a seeded
/// FeelMotionRandomizer always produces the same sequence regardless of what else calls
/// UnityEngine.Random the same frame, and so two randomizers on the same action never perturb each
/// other's sequence. xorshift32 — not cryptographic, fine for authoring-time value variation.
/// </summary>
public struct RandomState
{
    uint _state;

    RandomState(uint state) => _state = state;

    /// <summary>Deterministic stream from a user-authored seed.</summary>
    public static RandomState FromSeed(int seed)
    {
        var state = unchecked((uint)seed);
        return new RandomState(state == 0 ? 0x9E3779B9u : state); // 0 would never advance under xorshift
    }

    /// <summary>Non-deterministic stream — a fresh sequence every Play().</summary>
    public static RandomState NonDeterministic()
    {
        var state = unchecked((uint)System.Environment.TickCount) ^ 0x9E3779B9u;
        return new RandomState(state == 0 ? 0x9E3779B9u : state);
    }

    uint NextUInt()
    {
        var x = _state;
        x ^= x << 13;
        x ^= x >> 17;
        x ^= x << 5;
        _state = x;
        return x;
    }

    /// <summary>Uniform float in [0, 1).</summary>
    public float NextFloat01() => (NextUInt() & 0x00FFFFFFu) / (float)0x01000000u;

    /// <summary>Uniform float in [min, max]. Returns min unchanged when max &lt;= min.</summary>
    public float NextFloat(float min, float max) => max <= min ? min : min + (max - min) * NextFloat01();
}
