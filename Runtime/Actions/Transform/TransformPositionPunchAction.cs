using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// Punch adds a damped vibration on top of the captured position — EndValue is the punch
    /// strength, not a destination. Author with MotionValueMode.FromCurrentTo.
    [Serializable]
    [FeelMotionActionMenu("Transform/Position (Punch)")]
    public sealed class TransformPositionPunchAction : Vector3PropertyMotionAction<Transform, PunchOptions, Vector3PunchMotionAdapter>
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
