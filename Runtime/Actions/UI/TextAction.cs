using System;
using LitMotion;
using LitMotion.Adapters;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("UI/Text/Text")]
    public sealed class TextAction : StringPropertyMotionActionBase<Text>
    {
        protected override FixedString512Bytes GetValue(Text target) => target.text;

        protected override void SetValue(Text target, in FixedString512Bytes value)
        {
            var mutableValue = value;
            target.text = mutableValue.ConvertToString();
        }
    }
}
