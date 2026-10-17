#if FEELMOTION_PHYSICS
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rigidbody/Position")]
    public sealed class RigidbodyPositionAction : Vector3PropertyMotionAction<Rigidbody, NoOptions, Vector3MotionAdapter>
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
