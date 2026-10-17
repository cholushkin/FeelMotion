using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class LongChangedEvent : UnityEvent<long> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Long")]
    public sealed class LongValueAction : ValueMotionAction<long, IntegerOptions, LongMotionAdapter>
    {
        [SerializeField] LongChangedEvent _onValueChanged = new();

        public LongChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(long value) => _onValueChanged?.Invoke(value);
    }
}
