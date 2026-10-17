#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Line Spacing")]
    public sealed class TMPTextLineSpacingAction : FloatPropertyMotionAction<TMP_Text, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(TMP_Text target) => target.lineSpacing;

        protected override void SetValue(TMP_Text target, in float value) => target.lineSpacing = value;
    }
}
#endif
