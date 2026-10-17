using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Rect Transform/Pivot")]
    public sealed class RectTransformPivotAction : Vector2PropertyMotionAction<RectTransform, NoOptions, Vector2MotionAdapter>
    {
        protected override Vector2 GetValue(RectTransform target) => target.pivot;

        protected override void SetValue(RectTransform target, in Vector2 value) => target.pivot = value;
    }
}
