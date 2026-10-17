using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>Punch: a damped scale vibration added on top of the captured scale — Settings.EndValue is the punch strength, not a destination. Author with MotionValueMode.FromCurrentTo.</summary>
    [Serializable]
    [FeelMotionActionMenu("Transform/Scale (Punch)")]
    public sealed class TransformScalePunchAction : Vector3PropertyMotionAction<Transform, PunchOptions, Vector3PunchMotionAdapter>
    {
        protected internal override Vector3 GetValue(Transform target) => target.localScale;

        protected internal override void SetValue(Transform target, in Vector3 value) => target.localScale = value;
    }
}
