using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Rect")]
    public sealed class CameraRectAction : RectPropertyMotionAction<Camera, NoOptions, RectMotionAdapter>
    {
        protected override Rect GetValue(Camera target) => target.rect;

        protected override void SetValue(Camera target, in Rect value) => target.rect = value;
    }
}
