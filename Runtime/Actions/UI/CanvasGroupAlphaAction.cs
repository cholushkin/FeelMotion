using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Canvas Group/Alpha")]
    public sealed class CanvasGroupAlphaAction : PropertyMotionAction<CanvasGroup, float, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(CanvasGroup target) => target.alpha;

        protected override void SetValue(CanvasGroup target, in float value) => target.alpha = value;

        protected override float Add(in float baseValue, in float delta) => baseValue + delta;

        protected override float Multiply(in float baseValue, in float factor) => baseValue * factor;

        protected override float ScaleReach(in float from, in float to, float factor) => from + (to - from) * factor;
    }
}
