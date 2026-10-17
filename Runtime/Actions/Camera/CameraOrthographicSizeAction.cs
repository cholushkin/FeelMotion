using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Orthographic Size")]
    public sealed class CameraOrthographicSizeAction : FloatPropertyMotionAction<Camera, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Camera target) => target.orthographicSize;

        protected override void SetValue(Camera target, in float value) => target.orthographicSize = value;
    }
}
