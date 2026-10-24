using System;

namespace FeelMotion;

/// One randomizable component's range (e.g. a Vector3's Y) plus whether it's included. A disabled
/// component is left at its existing value regardless of RandomizationMode — how "randomize only
/// some axes" works.
[Serializable]
public struct ComponentRange
{
    public bool Enabled;
    public float Min;
    public float Max;

    public ComponentRange(bool enabled, float min, float max)
    {
        Enabled = enabled;
        Min = min;
        Max = max;
    }
}
