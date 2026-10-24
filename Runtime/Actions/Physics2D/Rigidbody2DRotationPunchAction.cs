#if FEELMOTION_PHYSICS_2D
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// Punch adds a damped vibration on top of the captured rotation — EndValue is the punch
    /// strength, not a destination. Author with MotionValueMode.FromCurrentTo.
    [Serializable]
    [FeelMotionActionMenu("Rigidbody2D/Rotation (Punch)")]
    public sealed class Rigidbody2DRotationPunchAction : FloatPropertyMotionAction<Rigidbody2D, PunchOptions, FloatPunchMotionAdapter>
    {
        [SerializeField] bool _useMoveRotation;

        protected override float GetValue(Rigidbody2D target) => target.rotation;

        protected override void SetValue(Rigidbody2D target, in float value)
        {
            if (_useMoveRotation) target.MoveRotation(value);
            else target.rotation = value;
        }
    }
}
#endif
