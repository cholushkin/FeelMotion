using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class Vector2ChangedEvent : UnityEvent<Vector2> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Vector2")]
    public sealed class Vector2ValueAction : ValueMotionAction<Vector2, NoOptions, Vector2MotionAdapter>
    {
        [SerializeField] Vector2ChangedEvent _onValueChanged = new();

        public Vector2ChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(Vector2 value) => _onValueChanged?.Invoke(value);
    }
}
