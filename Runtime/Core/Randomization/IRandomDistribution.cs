namespace FeelMotion;

/// <summary>
/// Pluggable sampling shape for a FeelMotionRandomizer's [Min, Max] range — Uniform
/// (<see cref="UniformDistribution"/>) is the only built-in implementation; additional distributions
/// (Gaussian, curve-driven, ...) are a new [Serializable] class implementing this interface, picked via
/// the same [SerializeReference] mechanism FeelMotionRandomizer already uses — no change to
/// FeelMotionRandomizer or the runtime apply path needed. See Documentation/Architecture.md.
/// </summary>
public interface IRandomDistribution
{
    /// <summary>Draws one value in [min, max] from <paramref name="state"/>, advancing it.</summary>
    float Sample(float min, float max, ref RandomState state);
}
