#if FEELMOTION_RENDER_PIPELINES
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;
using UnityEngine.Rendering;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Rendering/Volume/Weight")]
    public sealed class VolumeWeightAction : FloatPropertyMotionAction<Volume, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(Volume target) => target.weight;

        protected override void SetValue(Volume target, in float value) => target.weight = value;
    }
}
#endif
