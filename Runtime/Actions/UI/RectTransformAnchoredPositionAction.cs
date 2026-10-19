using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Rect Transform/Anchored Position")]
    public sealed class RectTransformAnchoredPositionAction : PropertyMotionAction<RectTransform, Vector2, NoOptions, Vector2MotionAdapter>
    {
        protected override Vector2 GetValue(RectTransform target) => target.anchoredPosition;

        protected override void SetValue(RectTransform target, in Vector2 value) => target.anchoredPosition = value;

        protected override Vector2 Add(in Vector2 baseValue, in Vector2 delta) => baseValue + delta;

        protected override Vector2 Multiply(in Vector2 baseValue, in Vector2 factor) => Vector2.Scale(baseValue, factor);

        protected override Vector2 ScaleReach(in Vector2 from, in Vector2 to, float factor) => from + (to - from) * factor;
    }
}
