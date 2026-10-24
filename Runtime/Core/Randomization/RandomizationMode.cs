namespace FeelMotion;

/// How a FeelMotionRandomizer's sampled value combines with the authored base value on an enabled
/// component.
public enum RandomizationMode
{
    /// Replace the authored value outright with a value sampled from [Min, Max].
    AbsoluteRange,

    /// Add a value sampled from [Min, Max] to the authored value.
    Offset,

    /// Multiply the authored value by a factor sampled from [Min, Max].
    Multiplier
}
