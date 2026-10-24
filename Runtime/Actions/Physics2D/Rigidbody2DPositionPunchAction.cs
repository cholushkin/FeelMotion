#if FEELMOTION_PHYSICS_2D
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// Punch adds a damped vibration on top of the captured position — EndValue is the punch
    /// strength, not a destination. Author with MotionValueMode.FromCurrentTo.
    [Serializable]
    [FeelMotionActionMenu("Rigidbody2D/Position (Punch)")]
    public sealed class Rigidbody2DPositionPunchAction : Vector2PropertyMotionAction<Rigidbody2D, PunchOptions, Vector2PunchMotionAdapter>
    {
        [SerializeField] bool _useMovePosition = true;

        protected override Vector2 GetValue(Rigidbody2D target) => target.position;

        protected override void SetValue(Rigidbody2D target, in Vector2 value)
        {
            if (_useMovePosition) target.MovePosition(value);
            else target.position = value;
        }
    }
}
#endif
