using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// Resolves one single-loop SerializableMotionSettings from an action's combined Start/End,
    /// applying ActionSettings, DurationMultiplier, and RandomizationSet — shared by the plain
    /// single-handle path and ManagedLoopPlayer's per-iteration path, so randomization logic isn't
    /// duplicated across PropertyMotionAction and ValueMotionAction (Architecture.md §12). Public
    /// because ValueMotionAction.Runtime lives in the separate FeelMotion.Value.asmdef.
    public static class MotionSettingsResolver
    {
        const float MinDuration = 0.0001f;

        public static SerializableMotionSettings<TValue, TOptions> Resolve<TValue, TOptions>(
            SerializableMotionSettings<TValue, TOptions> settingsWithCombinedStartEnd,
            ActionSettings actionSettings,
            float durationMultiplier,
            bool isFirstIteration,
            int loops,
            RandomizationSet randomization)
            where TValue : unmanaged
            where TOptions : unmanaged, IMotionOptions
        {
            var start = settingsWithCombinedStartEnd.StartValue;
            var end = settingsWithCombinedStartEnd.EndValue;
            var duration = settingsWithCombinedStartEnd.Duration * durationMultiplier;

            // Replicates DelayType across managed-loop iterations: FirstLoop pays the delay once,
            // EveryLoop pays it every iteration. ActionSettings.StartDelay always applies on the first
            // iteration only, regardless of DelayType.
            var delay = isFirstIteration || settingsWithCombinedStartEnd.DelayType == DelayType.EveryLoop
                ? settingsWithCombinedStartEnd.Delay * durationMultiplier
                : 0f;
            if (isFirstIteration && actionSettings.StartDelay > 0f)
                delay += actionSettings.StartDelay * durationMultiplier;

            if (randomization != null)
            {
                start = randomization.ApplyValue(RandomizedParameter.StartValue, start);
                end = randomization.ApplyValue(RandomizedParameter.EndValue, end);
                duration = Mathf.Max(MinDuration, randomization.ApplyScalar(RandomizedParameter.Duration, duration));
                delay = Mathf.Max(0f, randomization.ApplyScalar(RandomizedParameter.Delay, delay));
            }

            var resolved = settingsWithCombinedStartEnd with
            {
                StartValue = start,
                EndValue = end,
                Duration = duration,
                Delay = delay,
                Loops = loops
            };

            if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled)
                resolved = resolved with { Scheduler = actionSettings.ResolveScheduler() };

            return resolved;
        }
    }
}
