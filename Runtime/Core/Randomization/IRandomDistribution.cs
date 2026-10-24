namespace FeelMotion;

/// Pluggable sampling shape for a FeelMotionRandomizer's [Min, Max] range — UniformDistribution is
/// the only built-in implementation; a new one is a [Serializable] class implementing this interface,
/// picked via [SerializeReference], no change to FeelMotionRandomizer needed.
public interface IRandomDistribution
{
    /// Draws one value in [min, max] from `state`, advancing it.
    float Sample(float min, float max, ref RandomState state);
}
