using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class Vector3ChangedEvent : UnityEvent<Vector3> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Vector3")]
    public sealed class Vector3ValueAction : ValueMotionAction<Vector3, NoOptions, Vector3MotionAdapter>
    {
        [SerializeField] Vector3ChangedEvent _onValueChanged = new();

        public Vector3ChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(Vector3 value) => _onValueChanged?.Invoke(value);
    }
}
