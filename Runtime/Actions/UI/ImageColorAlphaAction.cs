using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Image/Color (Alpha)")]
    public sealed class ImageColorAlphaAction : FloatPropertyMotionAction<Image, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Image target) => target.color.a;

        protected override void SetValue(Image target, in float value)
        {
            var c = target.color;
            c.a = value;
            target.color = c;
        }
    }
}
