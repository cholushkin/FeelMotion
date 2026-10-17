using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class IntChangedEvent : UnityEvent<int> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Int")]
    public sealed class IntValueAction : ValueMotionAction<int, IntegerOptions, IntMotionAdapter>
    {
        [SerializeField] IntChangedEvent _onValueChanged = new();

        public IntChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(int value) => _onValueChanged?.Invoke(value);
    }
}
