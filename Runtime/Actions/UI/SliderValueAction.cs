using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Slider/Value")]
    public sealed class SliderValueAction : FloatPropertyMotionAction<Slider, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Slider target) => target.value;

        protected override void SetValue(Slider target, in float value) => target.value = value;
    }
}
