#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Color (Alpha)")]
    public sealed class TMPTextColorAlphaAction : FloatPropertyMotionAction<TMP_Text, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(TMP_Text target) => target.color.a;

        protected override void SetValue(TMP_Text target, in float value)
        {
            var c = target.color;
            c.a = value;
            target.color = c;
        }
    }
}
#endif
