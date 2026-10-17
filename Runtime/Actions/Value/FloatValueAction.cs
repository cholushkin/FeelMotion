using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class FloatChangedEvent : UnityEvent<float> { }

    /// <summary>No target — broadcasts the tweened float via OnValueChanged. Wire it in the Inspector to
    /// whatever should receive it (a shader property setter, a UI script, etc).</summary>
    [Serializable]
    [FeelMotionActionMenu("Value/Float")]
    public sealed class FloatValueAction : ValueMotionAction<float, NoOptions, FloatMotionAdapter>
    {
        [SerializeField] FloatChangedEvent _onValueChanged = new();

        public FloatChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(float value) => _onValueChanged?.Invoke(value);
    }
}
