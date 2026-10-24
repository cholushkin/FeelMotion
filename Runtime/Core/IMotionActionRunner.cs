namespace FeelMotion
{
    /// The minimal control surface shared by a single action's runtime and by FeelMotionPlayer itself,
    /// so calling code treats "one action" and "the whole player" the same way.
    public interface IMotionActionRunner
    {
        void Play();
        void Pause();
        void Resume();
        void Stop(MotionStopMode mode);
    }
}
