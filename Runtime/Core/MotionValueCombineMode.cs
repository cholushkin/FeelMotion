namespace FeelMotion
{
    /// <summary>
    /// How a RelativeToCurrent action combines the captured initial value with the authored EndValue.
    /// </summary>
    public enum MotionValueCombineMode
    {
        /// <summary>current + EndValue — EndValue is an offset. The long-standing default.</summary>
        Add,

        /// <summary>current * EndValue (component-wise for vectors/color) — EndValue is a factor.
        /// Needed for scale-like properties, where adding an offset distorts rather than scales.</summary>
        Multiply
    }
}
