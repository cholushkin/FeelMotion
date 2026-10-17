using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Text/Font Size")]
    public sealed class TextFontSizeAction : IntPropertyMotionAction<Text, IntegerOptions, IntMotionAdapter>
    {
        protected override int GetValue(Text target) => target.fontSize;

        protected override void SetValue(Text target, in int value) => target.fontSize = value;
    }
}
