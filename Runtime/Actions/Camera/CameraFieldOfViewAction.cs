using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Field Of View")]
    public sealed class CameraFieldOfViewAction : FloatPropertyMotionAction<Camera, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Camera target) => target.fieldOfView;

        protected override void SetValue(Camera target, in float value) => target.fieldOfView = value;
    }
}
