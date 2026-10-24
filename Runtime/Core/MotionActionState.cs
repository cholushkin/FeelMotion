namespace FeelMotion
{
    /// Explicit playback state of a single action's runtime instance. Set directly by the runtime at
    /// each transition — never inferred from MotionHandle.IsActive()/IsPlaying() at read time.
    public enum MotionActionState
    {
        Idle,
        Playing,
        Paused,
        Completed,
        Cancelled
    }
}
