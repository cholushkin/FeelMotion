using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// Shake adds a randomized vibration on top of the captured scale — EndValue is the shake strength,
    /// not a destination. Author with MotionValueMode.FromCurrentTo.
    [Serializable]
    [FeelMotionActionMenu("Transform/Scale (Shake)")]
    public sealed class TransformScaleShakeAction : Vector3PropertyMotionAction<Transform, ShakeOptions, Vector3ShakeMotionAdapter>
    {
        protected internal override Vector3 GetValue(Transform target) => target.localScale;

        protected internal override void SetValue(Transform target, in Vector3 value) => target.localScale = value;
    }
}
