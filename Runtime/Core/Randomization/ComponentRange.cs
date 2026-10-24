using System;

namespace FeelMotion;

/// <summary>
/// One randomizable component's range (e.g. a Vector3's Y) plus whether it's included at all. A
/// disabled component is left exactly at whatever value it already had, regardless of
/// RandomizationMode — this is how "configurable components" (randomize only some axes) works.
/// </summary>
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
