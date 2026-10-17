#if FEELMOTION_TMP
using System;
using LitMotion;
using Unity.Collections;

namespace FeelMotion
{
    /// <summary>String property base — interpolation/intensity-scaling are meaningless for text, so
    /// Add/ScaleReach just return the target value unchanged.</summary>
    [Serializable]
    public abstract class StringPropertyMotionActionBase<TObject> : PropertyMotionAction<TObject, FixedString512Bytes, StringOptions, LitMotion.Adapters.FixedString512BytesMotionAdapter>
        where TObject : UnityEngine.Object
    {
        protected override FixedString512Bytes Add(in FixedString512Bytes baseValue, in FixedString512Bytes delta) => delta;

        protected override FixedString512Bytes ScaleReach(in FixedString512Bytes from, in FixedString512Bytes to, float factor) => to;
    }
}
#endif
