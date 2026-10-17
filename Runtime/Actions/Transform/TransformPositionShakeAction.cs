using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a randomized vibration added on top of the captured position — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Transform/Position (Shake)")]
    public sealed class TransformPositionShakeAction : Vector3PropertyMotionAction<Transform, ShakeOptions, Vector3ShakeMotionAdapter>
    {
        [SerializeField] bool _useWorldSpace;

        protected internal override Vector3 GetValue(Transform target)
            => _useWorldSpace ? target.position : target.localPosition;

        protected internal override void SetValue(Transform target, in Vector3 value)
        {
            if (_useWorldSpace) target.position = value;
            else target.localPosition = value;
        }
    }
}
