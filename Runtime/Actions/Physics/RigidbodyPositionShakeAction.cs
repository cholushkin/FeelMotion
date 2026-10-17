#if FEELMOTION_PHYSICS
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a damped random vibration added on top of the captured position — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Rigidbody/Position (Shake)")]
    public sealed class RigidbodyPositionShakeAction : Vector3PropertyMotionAction<Rigidbody, ShakeOptions, Vector3ShakeMotionAdapter>
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
