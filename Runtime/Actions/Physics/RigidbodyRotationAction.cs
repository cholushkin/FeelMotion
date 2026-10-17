#if FEELMOTION_PHYSICS
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rigidbody/Rotation")]
    public sealed class RigidbodyRotationAction : Vector3PropertyMotionAction<Rigidbody, NoOptions, Vector3MotionAdapter>
    {
        [SerializeField] bool _useMoveRotation;

        protected override Vector3 GetValue(Rigidbody target) => target.rotation.eulerAngles;

        protected override void SetValue(Rigidbody target, in Vector3 value)
        {
            if (_useMoveRotation) target.MoveRotation(Quaternion.Euler(value));
            else target.rotation = Quaternion.Euler(value);
        }
    }
}
#endif
