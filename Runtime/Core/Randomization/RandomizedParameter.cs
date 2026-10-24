namespace FeelMotion;

/// Which authored parameter a FeelMotionRandomizer perturbs. StartValue/EndValue need the owning
/// action's own TValue (resolved through ValueComponentCodec); Duration/Delay are always float,
/// regardless of the action's value type.
public enum RandomizedParameter
{
    StartValue,
    EndValue,
    Duration,
    Delay
}
