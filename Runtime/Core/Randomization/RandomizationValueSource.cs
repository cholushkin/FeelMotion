namespace FeelMotion;

/// How one ComponentRange draws its sample: a continuous [Min, Max] range, or a discrete list of
/// authored candidate values (one picked uniformly at random per draw).
public enum RandomizationValueSource
{
    Range,
    List,
}
