#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Word Spacing")]
    public sealed class TMPTextWordSpacingAction : FloatPropertyMotionAction<TMP_Text, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(TMP_Text target) => target.wordSpacing;

        protected override void SetValue(TMP_Text target, in float value) => target.wordSpacing = value;
    }
}
#endif
