#if FEELMOTION_PHYSICS_2D
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a damped random vibration added on top of the captured rotation — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Rigidbody2D/Rotation (Shake)")]
    public sealed class Rigidbody2DRotationShakeAction : FloatPropertyMotionAction<Rigidbody2D, ShakeOptions, FloatShakeMotionAdapter>
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
