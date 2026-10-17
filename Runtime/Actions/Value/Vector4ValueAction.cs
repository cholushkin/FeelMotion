using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class Vector4ChangedEvent : UnityEvent<Vector4> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Vector4")]
    public sealed class Vector4ValueAction : ValueMotionAction<Vector4, NoOptions, Vector4MotionAdapter>
    {
        [SerializeField] Vector4ChangedEvent _onValueChanged = new();

        public Vector4ChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(Vector4 value) => _onValueChanged?.Invoke(value);
    }
}
