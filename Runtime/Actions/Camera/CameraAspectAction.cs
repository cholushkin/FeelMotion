using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Aspect")]
    public sealed class CameraAspectAction : FloatPropertyMotionAction<Camera, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Camera target) => target.aspect;

        protected override void SetValue(Camera target, in float value) => target.aspect = value;
    }
}
