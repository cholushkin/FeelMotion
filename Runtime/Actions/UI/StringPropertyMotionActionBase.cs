using System;
using LitMotion;
using LitMotion.Adapters;
using Unity.Collections;

namespace FeelMotion
{
    /// String property base — interpolation/intensity-scaling are meaningless for text, so
    /// Add/ScaleReach just return the target value unchanged.
    [Serializable]
    public abstract class StringPropertyMotionActionBase<TObject> : PropertyMotionAction<TObject, FixedString512Bytes, StringOptions, FixedString512BytesMotionAdapter>
        where TObject : UnityEngine.Object
    {
        protected override FixedString512Bytes Add(in FixedString512Bytes baseValue, in FixedString512Bytes delta) => delta;

        protected override FixedString512Bytes Multiply(in FixedString512Bytes baseValue, in FixedString512Bytes factor) => factor;

        protected override FixedString512Bytes ScaleReach(in FixedString512Bytes from, in FixedString512Bytes to, float factor) => to;
    }
}
