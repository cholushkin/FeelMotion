namespace FeelMotion
{
    /// How a RelativeToCurrent action combines the captured initial value with the authored EndValue.
    public enum MotionValueCombineMode
    {
        /// current + EndValue — EndValue is an offset. The long-standing default.
        Add,

        /// current * EndValue (component-wise for vectors/color) — EndValue is a factor. Needed for
        /// scale-like properties, where adding an offset distorts rather than scales.
        Multiply
    }
}
