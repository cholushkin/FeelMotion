using System;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Decomposes a TValue into up to 4 float components and recomposes it — the single bridge that
    /// lets RandomizationSet apply one generic, non-generic-module randomizer to any supported value
    /// type (float/int/long/double/Vector2/Vector3/Vector4/Color/Rect) without a per-type randomizer
    /// class. Mirrors the per-type arithmetic already in PropertyMotionActionBases.cs (Add/Multiply/
    /// ScaleReach), as a standalone static so it also serves ValueMotionAction, which doesn't derive
    /// from those bases. See Documentation/Architecture.md's Randomization section.
    /// </summary>
    public interface IValueComponentCodec<TValue> where TValue : unmanaged
    {
        int ComponentCount { get; }
        string[] ComponentLabels { get; }
        void Decompose(in TValue value, Span<float> components);
        TValue Recompose(ReadOnlySpan<float> components);
    }

    /// <summary>Non-generic companion to ValueComponentCodec&lt;TValue&gt; — for editor code that only
    /// has a runtime <see cref="Type"/> (not a compile-time TValue), e.g. labeling a Vector3 action's
    /// randomizer rows "X"/"Y"/"Z". Single source of truth for the per-type component shape, shared by
    /// both the runtime apply path and the Inspector. Empty for an unsupported type.</summary>
    public static class ValueComponentCodec
    {
        public static string[] GetComponentLabels(Type valueType)
        {
            if (valueType == typeof(float) || valueType == typeof(int) || valueType == typeof(long) || valueType == typeof(double))
                return FloatCodec.Instance.ComponentLabels;
            if (valueType == typeof(Vector2)) return Vector2Codec.Instance.ComponentLabels;
            if (valueType == typeof(Vector3)) return Vector3Codec.Instance.ComponentLabels;
            if (valueType == typeof(Vector4)) return Vector4Codec.Instance.ComponentLabels;
            if (valueType == typeof(Color)) return ColorCodec.Instance.ComponentLabels;
            if (valueType == typeof(Rect)) return RectCodec.Instance.ComponentLabels;
            return Array.Empty<string>();
        }
    }

    /// <summary>Resolves the codec for a closed TValue once per type (a static generic field is
    /// initialized exactly once per closed generic instantiation by the CLR) — never per Play(), let
    /// alone per frame. Null for an unsupported TValue (e.g. FixedString*): Start/End Value
    /// randomization is then unavailable for that action, but Duration/Delay randomization still works.</summary>
    public static class ValueComponentCodec<TValue> where TValue : unmanaged
    {
        public static readonly IValueComponentCodec<TValue> Instance = Resolve();

        static IValueComponentCodec<TValue> Resolve()
        {
            if (typeof(TValue) == typeof(float)) return As(FloatCodec.Instance);
            if (typeof(TValue) == typeof(int)) return As(IntCodec.Instance);
            if (typeof(TValue) == typeof(long)) return As(LongCodec.Instance);
            if (typeof(TValue) == typeof(double)) return As(DoubleCodec.Instance);
            if (typeof(TValue) == typeof(Vector2)) return As(Vector2Codec.Instance);
            if (typeof(TValue) == typeof(Vector3)) return As(Vector3Codec.Instance);
            if (typeof(TValue) == typeof(Vector4)) return As(Vector4Codec.Instance);
            if (typeof(TValue) == typeof(Color)) return As(ColorCodec.Instance);
            if (typeof(TValue) == typeof(Rect)) return As(RectCodec.Instance);
            return null;
        }

        // Safe: only ever called once typeof(TValue) has already been checked to match TCodecValue.
        static IValueComponentCodec<TValue> As<TCodecValue>(IValueComponentCodec<TCodecValue> codec) where TCodecValue : unmanaged
            => (IValueComponentCodec<TValue>)(object)codec;
    }

    sealed class FloatCodec : IValueComponentCodec<float>
    {
        public static readonly FloatCodec Instance = new();
        public int ComponentCount => 1;
        public string[] ComponentLabels { get; } = { "Value" };
        public void Decompose(in float value, Span<float> components) => components[0] = value;
        public float Recompose(ReadOnlySpan<float> components) => components[0];
    }

    sealed class IntCodec : IValueComponentCodec<int>
    {
        public static readonly IntCodec Instance = new();
        public int ComponentCount => 1;
        public string[] ComponentLabels { get; } = { "Value" };
        public void Decompose(in int value, Span<float> components) => components[0] = value;
        public int Recompose(ReadOnlySpan<float> components) => Mathf.RoundToInt(components[0]);
    }

    sealed class LongCodec : IValueComponentCodec<long>
    {
        public static readonly LongCodec Instance = new();
        public int ComponentCount => 1;
        public string[] ComponentLabels { get; } = { "Value" };
        public void Decompose(in long value, Span<float> components) => components[0] = value;
        public long Recompose(ReadOnlySpan<float> components) => (long)Math.Round(components[0]);
    }

    sealed class DoubleCodec : IValueComponentCodec<double>
    {
        public static readonly DoubleCodec Instance = new();
        public int ComponentCount => 1;
        public string[] ComponentLabels { get; } = { "Value" };
        public void Decompose(in double value, Span<float> components) => components[0] = (float)value;
        public double Recompose(ReadOnlySpan<float> components) => components[0];
    }

    sealed class Vector2Codec : IValueComponentCodec<Vector2>
    {
        public static readonly Vector2Codec Instance = new();
        public int ComponentCount => 2;
        public string[] ComponentLabels { get; } = { "X", "Y" };
        public void Decompose(in Vector2 value, Span<float> components) { components[0] = value.x; components[1] = value.y; }
        public Vector2 Recompose(ReadOnlySpan<float> components) => new(components[0], components[1]);
    }

    sealed class Vector3Codec : IValueComponentCodec<Vector3>
    {
        public static readonly Vector3Codec Instance = new();
        public int ComponentCount => 3;
        public string[] ComponentLabels { get; } = { "X", "Y", "Z" };
        public void Decompose(in Vector3 value, Span<float> components) { components[0] = value.x; components[1] = value.y; components[2] = value.z; }
        public Vector3 Recompose(ReadOnlySpan<float> components) => new(components[0], components[1], components[2]);
    }

    sealed class Vector4Codec : IValueComponentCodec<Vector4>
    {
        public static readonly Vector4Codec Instance = new();
        public int ComponentCount => 4;
        public string[] ComponentLabels { get; } = { "X", "Y", "Z", "W" };
        public void Decompose(in Vector4 value, Span<float> components) { components[0] = value.x; components[1] = value.y; components[2] = value.z; components[3] = value.w; }
        public Vector4 Recompose(ReadOnlySpan<float> components) => new(components[0], components[1], components[2], components[3]);
    }

    sealed class ColorCodec : IValueComponentCodec<Color>
    {
        public static readonly ColorCodec Instance = new();
        public int ComponentCount => 4;
        public string[] ComponentLabels { get; } = { "R", "G", "B", "A" };
        public void Decompose(in Color value, Span<float> components) { components[0] = value.r; components[1] = value.g; components[2] = value.b; components[3] = value.a; }
        public Color Recompose(ReadOnlySpan<float> components) => new(components[0], components[1], components[2], components[3]);
    }

    sealed class RectCodec : IValueComponentCodec<Rect>
    {
        public static readonly RectCodec Instance = new();
        public int ComponentCount => 4;
        public string[] ComponentLabels { get; } = { "X", "Y", "Width", "Height" };
        public void Decompose(in Rect value, Span<float> components) { components[0] = value.x; components[1] = value.y; components[2] = value.width; components[3] = value.height; }
        public Rect Recompose(ReadOnlySpan<float> components) => new(components[0], components[1], components[2], components[3]);
    }
}
