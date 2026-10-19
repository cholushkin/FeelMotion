using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Transform/Rotation")]
    public sealed class TransformRotationAction : PropertyMotionAction<Transform, Vector3, NoOptions, Vector3MotionAdapter>
    {
        [SerializeField] bool _useWorldSpace;

        protected internal override Vector3 GetValue(Transform target)
            => _useWorldSpace ? target.eulerAngles : target.localEulerAngles;

        protected internal override void SetValue(Transform target, in Vector3 value)
        {
            if (_useWorldSpace) target.eulerAngles = value;
            else target.localEulerAngles = value;
        }

        protected internal override Vector3 Add(in Vector3 baseValue, in Vector3 delta) => baseValue + delta;

        protected internal override Vector3 Multiply(in Vector3 baseValue, in Vector3 factor) => Vector3.Scale(baseValue, factor);

        protected internal override Vector3 ScaleReach(in Vector3 from, in Vector3 to, float factor) => from + (to - from) * factor;
    }
}
