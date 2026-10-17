using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Text/Color")]
    public sealed class TextColorAction : ColorPropertyMotionAction<Text, NoOptions, ColorMotionAdapter>
    {
        protected override Color GetValue(Text target) => target.color;

        protected override void SetValue(Text target, in Color value) => target.color = value;
    }
}
