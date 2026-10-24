namespace FeelMotion
{
    /// Stopping playback and restoring an action's initial value are independent choices — never
    /// coupled the way LitMotion.Animation's OnStop() always restores.
    public enum MotionStopMode
    {
        /// Cancel. The value is left exactly where the tween stopped.
        Cancel,
        /// Cancel, then write each action's captured initial value back.
        CancelAndRestore,
        /// Jump to the end value and fire completion as if it finished naturally.
        Complete,
        /// Complete, then write each action's captured initial value back.
        CompleteAndRestore
    }
}
