using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Near Clip Plane")]
    public sealed class CameraNearClipPlaneAction : FloatPropertyMotionAction<Camera, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Camera target) => target.nearClipPlane;

        protected override void SetValue(Camera target, in float value) => target.nearClipPlane = value;
    }
}
