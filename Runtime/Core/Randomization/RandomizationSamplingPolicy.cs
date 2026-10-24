namespace FeelMotion;

/// When a FeelMotionRandomizer draws a new sample.
public enum RandomizationSamplingPolicy
{
    /// Sample exactly once, at Play() — every loop of a looping action reuses that same sample.
    OncePerPlay,

    /// Sample fresh for every loop pass. Forces FeelMotion-managed loop iteration
    /// (FeelMotionAction.RequiresManagedLoop) whenever the owning action's LoopCount != 1.
    EveryLoop
}
