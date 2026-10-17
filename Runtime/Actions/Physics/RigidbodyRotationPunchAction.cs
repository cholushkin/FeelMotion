#if FEELMOTION_PHYSICS
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Punch: a damped vibration added on top of the captured rotation — Settings.EndValue is the punch strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Rigidbody/Rotation (Punch)")]
    public sealed class RigidbodyRotationPunchAction : Vector3PropertyMotionAction<Rigidbody, PunchOptions, Vector3PunchMotionAdapter>
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
