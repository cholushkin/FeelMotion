using System;

namespace FeelMotion;

/// <summary>Flat probability across [min, max] — every value in range is equally likely.</summary>
[Serializable]
public sealed class UniformDistribution : IRandomDistribution
{
    public float Sample(float min, float max, ref RandomState state) => state.NextFloat(min, max);
}
