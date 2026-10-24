namespace FeelMotion;

/// <summary>How a FeelMotionRandomizer's sampled value combines with the authored base value on an
/// enabled component — see Documentation/Architecture.md's Randomization section.</summary>
public enum RandomizationMode
{
    /// <summary>Replace the authored value outright with a value sampled from [Min, Max].</summary>
    AbsoluteRange,

    /// <summary>Add a value sampled from [Min, Max] to the authored value.</summary>
    Offset,

    /// <summary>Multiply the authored value by a factor sampled from [Min, Max].</summary>
    Multiplier
}
