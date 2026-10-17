using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a randomized rotational vibration added on top of the captured rotation — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Transform/Rotation (Shake)")]
    public sealed class TransformRotationShakeAction : Vector3PropertyMotionAction<Transform, ShakeOptions, Vector3ShakeMotionAdapter>
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
