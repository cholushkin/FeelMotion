#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Paragraph Spacing")]
    public sealed class TMPTextParagraphSpacingAction : FloatPropertyMotionAction<TMP_Text, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(TMP_Text target) => target.paragraphSpacing;

        protected override void SetValue(TMP_Text target, in float value) => target.paragraphSpacing = value;
    }
}
#endif
