namespace FeelMotion
{
    /// <summary>
    /// How a property action's authored Start/End values relate to the target's value at play time.
    /// </summary>
    public enum MotionValueMode
    {
        /// <summary>Always tween the literal authored StartValue/EndValue, regardless of the target's current value.</summary>
        Absolute,

        /// <summary>Capture the target's current value at play time and tween by the authored EndValue, treated as an offset.</summary>
        RelativeToCurrent,

        /// <summary>Capture the target's current value at play time and tween to the authored EndValue directly — no offset math, no snap.</summary>
        FromCurrentTo
    }
}
