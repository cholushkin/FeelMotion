using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.UI;

namespace FeelMotion
{
    /// <summary>Animates the color of any UGUI Graphic — Image, RawImage, Text, or a custom subclass.</summary>
    [Serializable]
    [FeelMotionActionMenu("UI/Graphic/Color")]
    public sealed class GraphicColorAction : PropertyMotionAction<Graphic, Color, NoOptions, ColorMotionAdapter>
    {
        protected override Color GetValue(Graphic target) => target.color;

        protected override void SetValue(Graphic target, in Color value) => target.color = value;

        protected override Color Add(in Color baseValue, in Color delta) => baseValue + delta;

        protected override Color Multiply(in Color baseValue, in Color factor) => new(
            baseValue.r * factor.r, baseValue.g * factor.g, baseValue.b * factor.b, baseValue.a * factor.a);

        protected override Color ScaleReach(in Color from, in Color to, float factor) => from + (to - from) * factor;
    }
}
