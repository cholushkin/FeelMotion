using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Resolves one single-loop SerializableMotionSettings&lt;TValue,TOptions&gt; from an action's
    /// already value-mode-combined Start/End (PropertyMotionActionRuntime) or authored Start/End
    /// (ValueMotionAction.Runtime), applying ActionSettings, the player's DurationMultiplier, and any
    /// RandomizationSet — shared by both the plain single-handle path (native Loops, isFirstIteration
    /// always true) and ManagedLoopPlayer's per-iteration path (Loops forced to 1, isFirstIteration only
    /// for the very first call). Writing this once instead of duplicating it per runtime is what keeps
    /// randomization logic from being duplicated across PropertyMotionAction and ValueMotionAction — see
    /// Documentation/Architecture.md's Randomization section. Public (not internal) — both runtimes
    /// call it, and ValueMotionAction.Runtime lives in the separate FeelMotion.Value.asmdef
    /// (Architecture.md §9), which only references FeelMotion, not the other way around.
    /// </summary>
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

            // Replicates DelayType ourselves across managed-loop iterations: FirstLoop (the default)
            // pays the authored delay once, EveryLoop pays it every iteration — the same distinction
            // LitMotion's own native Loops applies within a single handle (Documentation/Architecture.md
            // §4/§8). The player-level ActionSettings.StartDelay is always a start-of-action delay, so
            // it only ever applies on the first iteration, regardless of DelayType.
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
