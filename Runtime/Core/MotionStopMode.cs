namespace FeelMotion
{
    /// <summary>
    /// Stopping playback and restoring an action's initial value are independent choices — never
    /// coupled the way LitMotion.Animation's OnStop() always restores.
    /// </summary>
    public enum MotionStopMode
    {
        /// <summary>Cancel. The value is left exactly where the tween stopped.</summary>
        Cancel,
        /// <summary>Cancel, then write each action's captured initial value back.</summary>
        CancelAndRestore,
        /// <summary>Jump to the end value and fire completion as if it finished naturally.</summary>
        Complete,
        /// <summary>Complete, then write each action's captured initial value back.</summary>
        CompleteAndRestore
    }
}
