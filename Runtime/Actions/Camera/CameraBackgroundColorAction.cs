using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Background Color")]
    public sealed class CameraBackgroundColorAction : ColorPropertyMotionAction<Camera, NoOptions, ColorMotionAdapter>
    {
        protected override Color GetValue(Camera target) => target.backgroundColor;

        protected override void SetValue(Camera target, in Color value) => target.backgroundColor = value;
    }
}
