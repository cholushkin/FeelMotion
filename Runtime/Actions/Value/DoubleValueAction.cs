using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class DoubleChangedEvent : UnityEvent<double> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Double")]
    public sealed class DoubleValueAction : ValueMotionAction<double, NoOptions, DoubleMotionAdapter>
    {
        [SerializeField] DoubleChangedEvent _onValueChanged = new();

        public DoubleChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(double value) => _onValueChanged?.Invoke(value);
    }
}
