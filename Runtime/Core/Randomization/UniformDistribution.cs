using System;

namespace FeelMotion;

/// Flat probability across [min, max] — every value in range is equally likely.
[Serializable]
public sealed class UniformDistribution : IRandomDistribution
{
    public float Sample(float min, float max, ref RandomState state) => state.NextFloat(min, max);
}
