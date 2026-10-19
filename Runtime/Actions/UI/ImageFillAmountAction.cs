using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Image/Fill Amount")]
    public sealed class ImageFillAmountAction : PropertyMotionAction<Image, float, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Image target) => target.fillAmount;

        protected override void SetValue(Image target, in float value) => target.fillAmount = value;

        protected override float Add(in float baseValue, in float delta) => baseValue + delta;

        protected override float Multiply(in float baseValue, in float factor) => baseValue * factor;

        protected override float ScaleReach(in float from, in float to, float factor) => from + (to - from) * factor;
    }
}
