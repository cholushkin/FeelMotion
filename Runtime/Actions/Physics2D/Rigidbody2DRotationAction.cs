#if FEELMOTION_PHYSICS_2D
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rigidbody2D/Rotation")]
    public sealed class Rigidbody2DRotationAction : FloatPropertyMotionAction<Rigidbody2D, NoOptions, FloatMotionAdapter>
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
