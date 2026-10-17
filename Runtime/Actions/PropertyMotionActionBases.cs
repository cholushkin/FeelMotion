using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Value-typed PropertyMotionAction bases — implement Add/ScaleReach once per value type so a
    /// concrete action (in this or another assembly) only has to supply GetValue/SetValue. Generic over
    /// TOptions/TAdapter so the same base covers plain, Punch, and Shake variants of a value type.
    /// </summary>
    [Serializable]
    public abstract class FloatPropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, float, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<float, TOptions>
    {
        protected internal override float Add(in float baseValue, in float delta) => baseValue + delta;
        protected internal override float ScaleReach(in float from, in float to, float factor) => from + (to - from) * factor;
    }

    [Serializable]
    public abstract class DoublePropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, double, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<double, TOptions>
    {
        protected internal override double Add(in double baseValue, in double delta) => baseValue + delta;
        protected internal override double ScaleReach(in double from, in double to, float factor) => from + (to - from) * factor;
    }

    [Serializable]
    public abstract class IntPropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, int, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<int, TOptions>
    {
        protected internal override int Add(in int baseValue, in int delta) => baseValue + delta;
        protected internal override int ScaleReach(in int from, in int to, float factor) => from + Mathf.RoundToInt((to - from) * factor);
    }

    [Serializable]
    public abstract class LongPropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, long, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<long, TOptions>
    {
        protected internal override long Add(in long baseValue, in long delta) => baseValue + delta;
        protected internal override long ScaleReach(in long from, in long to, float factor) => from + (long)Math.Round((to - from) * factor);
    }

    [Serializable]
    public abstract class Vector2PropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, Vector2, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<Vector2, TOptions>
    {
        protected internal override Vector2 Add(in Vector2 baseValue, in Vector2 delta) => baseValue + delta;
        protected internal override Vector2 ScaleReach(in Vector2 from, in Vector2 to, float factor) => from + (to - from) * factor;
    }

    [Serializable]
    public abstract class Vector3PropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, Vector3, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<Vector3, TOptions>
    {
        protected internal override Vector3 Add(in Vector3 baseValue, in Vector3 delta) => baseValue + delta;
        protected internal override Vector3 ScaleReach(in Vector3 from, in Vector3 to, float factor) => from + (to - from) * factor;
    }

    [Serializable]
    public abstract class Vector4PropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, Vector4, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<Vector4, TOptions>
    {
        protected internal override Vector4 Add(in Vector4 baseValue, in Vector4 delta) => baseValue + delta;
        protected internal override Vector4 ScaleReach(in Vector4 from, in Vector4 to, float factor) => from + (to - from) * factor;
    }

    [Serializable]
    public abstract class ColorPropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, Color, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<Color, TOptions>
    {
        protected internal override Color Add(in Color baseValue, in Color delta) => baseValue + delta;
        protected internal override Color ScaleReach(in Color from, in Color to, float factor) => from + (to - from) * factor;
    }

    [Serializable]
    public abstract class RectPropertyMotionAction<TObject, TOptions, TAdapter> : PropertyMotionAction<TObject, Rect, TOptions, TAdapter>
        where TObject : UnityEngine.Object
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<Rect, TOptions>
    {
        protected internal override Rect Add(in Rect baseValue, in Rect delta)
            => new(baseValue.x + delta.x, baseValue.y + delta.y, baseValue.width + delta.width, baseValue.height + delta.height);

        protected internal override Rect ScaleReach(in Rect from, in Rect to, float factor) => new(
            from.x + (to.x - from.x) * factor,
            from.y + (to.y - from.y) * factor,
            from.width + (to.width - from.width) * factor,
            from.height + (to.height - from.height) * factor);
    }
}
