namespace FeelMotion;

/// <summary>
/// Which authored parameter a FeelMotionRandomizer perturbs. StartValue/EndValue need the owning
/// action's own TValue (resolved through ValueComponentCodec&lt;TValue&gt;); Duration/Delay are always
/// float, regardless of the action's value type. See Documentation/Architecture.md's Randomization
/// section.
/// </summary>
public enum RandomizedParameter
{
    StartValue,
    EndValue,
    Duration,
    Delay
}
