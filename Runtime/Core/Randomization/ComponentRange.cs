using System;
using System.Collections.Generic;

namespace FeelMotion;

/// One randomizable component's range (e.g. a Vector3's Y) plus whether it's included. A disabled
/// component is left at its existing value regardless of RandomizationMode — how "randomize only
/// some axes" works. Source selects whether Min/Max (a continuous range) or Values (a discrete list
/// of candidates) supplies the draw.
[Serializable]
public struct ComponentRange
{
    public bool Enabled;
    public float Min;
    public float Max;
    public RandomizationValueSource Source;
    public List<float> Values;

    public ComponentRange(bool enabled, float min, float max)
    {
        Enabled = enabled;
        Min = min;
        Max = max;
        Source = RandomizationValueSource.Range;
        Values = new List<float>();
    }
}
