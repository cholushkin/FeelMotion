using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Image/Color")]
    public sealed class ImageColorAction : ColorPropertyMotionAction<Image, NoOptions, ColorMotionAdapter>
    {
        protected override Color GetValue(Image target) => target.color;

        protected override void SetValue(Image target, in Color value) => target.color = value;
    }
}
