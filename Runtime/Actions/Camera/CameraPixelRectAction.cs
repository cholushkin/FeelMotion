using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Camera/Pixel Rect")]
    public sealed class CameraPixelRectAction : RectPropertyMotionAction<Camera, NoOptions, RectMotionAdapter>
    {
        protected override Rect GetValue(Camera target) => target.pixelRect;

        protected override void SetValue(Camera target, in Rect value) => target.pixelRect = value;
    }
}
