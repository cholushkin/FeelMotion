namespace FeelMotion;

/// <summary>When a FeelMotionRandomizer draws a new sample — see Documentation/Architecture.md's
/// Randomization section for how EveryLoop is implemented (FeelMotion-managed loop iteration, since a
/// single LitMotion handle with native Loops bakes one sample for every repeat).</summary>
public enum RandomizationSamplingPolicy
{
    /// <summary>Sample exactly once, at Play() — every loop of a looping action reuses that same sample.</summary>
    OncePerPlay,

    /// <summary>Sample fresh for every loop pass. Forces FeelMotion-managed loop iteration
    /// (FeelMotionAction.RequiresManagedLoop) whenever the owning action's LoopCount != 1.</summary>
    EveryLoop
}
