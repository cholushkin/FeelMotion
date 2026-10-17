#if FEELMOTION_PHYSICS
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Punch: a damped vibration added on top of the captured position — Settings.EndValue is the punch strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Rigidbody/Position (Punch)")]
    public sealed class RigidbodyPositionPunchAction : Vector3PropertyMotionAction<Rigidbody, PunchOptions, Vector3PunchMotionAdapter>
    {
        [SerializeField] bool _useMovePosition = true;

        protected override Vector3 GetValue(Rigidbody target) => target.position;

        protected override void SetValue(Rigidbody target, in Vector3 value)
        {
            if (_useMovePosition) target.MovePosition(value);
            else target.position = value;
        }
    }
}
#endif
