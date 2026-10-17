#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Font Size")]
    public sealed class TMPTextFontSizeAction : FloatPropertyMotionAction<TMP_Text, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(TMP_Text target) => target.fontSize;

        protected override void SetValue(TMP_Text target, in float value) => target.fontSize = value;
    }
}
#endif
