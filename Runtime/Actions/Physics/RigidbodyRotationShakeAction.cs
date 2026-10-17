#if FEELMOTION_PHYSICS
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a damped random vibration added on top of the captured rotation — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Rigidbody/Rotation (Shake)")]
    public sealed class RigidbodyRotationShakeAction : Vector3PropertyMotionAction<Rigidbody, ShakeOptions, Vector3ShakeMotionAdapter>
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
