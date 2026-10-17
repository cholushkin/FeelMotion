using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Global playback knobs set on FeelMotionPlayer and threaded into every action's runtime at
    /// CreateRuntime() time — mirrors Feel's MMF_Player.DurationMultiplier/FeedbacksIntensity. See
    /// Documentation/Architecture.md.
    /// </summary>
    public readonly struct MotionPlaybackContext
    {
        const float MinDurationMultiplier = 0.0001f;

        public readonly float DurationMultiplier;
        public readonly float IntensityMultiplier;

        public static MotionPlaybackContext Default => new(1f, 1f);

        public MotionPlaybackContext(float durationMultiplier, float intensityMultiplier)
        {
            DurationMultiplier = Mathf.Max(MinDurationMultiplier, durationMultiplier);
            IntensityMultiplier = Mathf.Max(0f, intensityMultiplier);
        }
    }
}
