#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Character Spacing")]
    public sealed class TMPTextCharacterSpacingAction : FloatPropertyMotionAction<TMP_Text, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(TMP_Text target) => target.characterSpacing;

        protected override void SetValue(TMP_Text target, in float value) => target.characterSpacing = value;
    }
}
#endif
