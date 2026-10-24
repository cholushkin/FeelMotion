using System;
using LitMotion;
using LitMotion.Adapters;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class StringChangedEvent : UnityEvent<string> { }

    /// Tweens text character-by-character (LitMotion's FixedString512Bytes string motion) and
    /// broadcasts the result as a plain string.
    [Serializable]
    [FeelMotionActionMenu("Value/String")]
    public sealed class StringValueAction : ValueMotionAction<FixedString512Bytes, StringOptions, FixedString512BytesMotionAdapter>
    {
        [SerializeField] StringChangedEvent _onValueChanged = new();

        public StringChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(FixedString512Bytes value) => _onValueChanged?.Invoke(value.ConvertToString());
    }
}
