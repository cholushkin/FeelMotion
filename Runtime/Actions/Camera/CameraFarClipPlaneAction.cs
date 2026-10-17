using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Far Clip Plane")]
    public sealed class CameraFarClipPlaneAction : FloatPropertyMotionAction<Camera, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Camera target) => target.farClipPlane;

        protected override void SetValue(Camera target, in float value) => target.farClipPlane = value;
    }
}
