#if FEELMOTION_TMP
using System;
using LitMotion;
using LitMotion.Adapters;
using TMPro;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Color")]
    public sealed class TMPTextColorAction : ColorPropertyMotionAction<TMP_Text, NoOptions, ColorMotionAdapter>
    {
        protected override Color GetValue(TMP_Text target) => target.color;

        protected override void SetValue(TMP_Text target, in Color value) => target.color = value;
    }
}
#endif
