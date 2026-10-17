#if FEELMOTION_TMP
using System;
using TMPro;
using Unity.Collections;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/TextMesh Pro/Text")]
    public sealed class TMPTextAction : StringPropertyMotionActionBase<TMP_Text>
    {
        protected override FixedString512Bytes GetValue(TMP_Text target) => target.text;

        protected override void SetValue(TMP_Text target, in FixedString512Bytes value)
        {
            var mutableValue = value;
            target.text = mutableValue.ConvertToString();
        }
    }
}
#endif
