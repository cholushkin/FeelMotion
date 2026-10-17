#if FEELMOTION_PHYSICS_2D
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rigidbody2D/Position")]
    public sealed class Rigidbody2DPositionAction : Vector2PropertyMotionAction<Rigidbody2D, NoOptions, Vector2MotionAdapter>
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
