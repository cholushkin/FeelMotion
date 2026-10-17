using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Shake: a randomized scale vibration added on top of the captured scale — Settings.EndValue is the shake strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Transform/Scale (Shake)")]
    public sealed class TransformScaleShakeAction : Vector3PropertyMotionAction<Transform, ShakeOptions, Vector3ShakeMotionAdapter>
    {
        protected internal override Vector3 GetValue(Transform target) => target.localScale;

        protected internal override void SetValue(Transform target, in Vector3 value) => target.localScale = value;
    }
}
