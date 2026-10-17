#if FEELMOTION_PHYSICS_2D
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a damped random vibration added on top of the captured position — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Rigidbody2D/Position (Shake)")]
    public sealed class Rigidbody2DPositionShakeAction : Vector2PropertyMotionAction<Rigidbody2D, ShakeOptions, Vector2ShakeMotionAdapter>
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
