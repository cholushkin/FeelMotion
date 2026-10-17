using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    [Serializable]
    public sealed class ColorChangedEvent : UnityEvent<Color> { }

    [Serializable]
    [FeelMotionActionMenu("Value/Color")]
    public sealed class ColorValueAction : ValueMotionAction<Color, NoOptions, ColorMotionAdapter>
    {
        [SerializeField] ColorChangedEvent _onValueChanged = new();

        public ColorChangedEvent OnValueChanged => _onValueChanged;

        protected override void InvokeChanged(Color value) => _onValueChanged?.Invoke(value);
    }
}
