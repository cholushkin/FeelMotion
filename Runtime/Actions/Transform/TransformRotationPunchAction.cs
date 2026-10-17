using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Punch: a damped rotational vibration added on top of the captured rotation — Settings.EndValue is the punch strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Transform/Rotation (Punch)")]
    public sealed class TransformRotationPunchAction : Vector3PropertyMotionAction<Transform, PunchOptions, Vector3PunchMotionAdapter>
    {
        [SerializeField] bool _useWorldSpace;

        protected internal override Vector3 GetValue(Transform target)
            => _useWorldSpace ? target.eulerAngles : target.localEulerAngles;

        protected internal override void SetValue(Transform target, in Vector3 value)
        {
            if (_useWorldSpace) target.eulerAngles = value;
            else target.localEulerAngles = value;
        }
    }
}
