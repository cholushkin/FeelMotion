using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// Serialized, authored configuration for one action in a FeelMotionPlayer's sequence. Holds no
    /// runtime state (handles, captured values) — that lives on the IMotionActionRuntime that
    /// CreateRuntime() returns fresh for every Play() call. See Documentation/Architecture.md.
    [Serializable]
    public abstract class FeelMotionAction
    {
        [SerializeField] string _displayName;
        [SerializeField] bool _enabled = true;
        [SerializeField] ActionSettings _actionSettings = new();
        [SerializeField] List<FeelMotionRandomizer> _randomizers = new();

        public bool Enabled => _enabled;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? GetType().Name : _displayName;

        /// Common playback knobs (start delay, timescale mode) shared by every action type — see
        /// Documentation/Architecture.md.
        public ActionSettings ActionSettings => _actionSettings;

        /// Opt-in, independent randomization modules for Start/End Value, Duration, and Delay — empty
        /// by default, applied to the *resolved* runtime settings, never to these authored values (see
        /// Documentation/Architecture.md §12). Only PropertyMotionAction/ValueMotionAction resolve
        /// these; a control action can still hold modules, it just has nothing to apply them to.
        public IReadOnlyList<FeelMotionRandomizer> Randomizers => _randomizers;

        /// Adds a randomizer module from code (editor tooling, tests). Returns it for chaining.
        public FeelMotionRandomizer AddRandomizer()
        {
            var randomizer = new FeelMotionRandomizer();
            _randomizers.Add(randomizer);
            return randomizer;
        }

        public void RemoveRandomizer(FeelMotionRandomizer randomizer) => _randomizers.Remove(randomizer);

        /// True when this action has at least one enabled EveryLoop randomizer and actually loops
        /// (LoopCount != 1) — forces FeelMotion-managed loop iteration (fresh single-loop LitMotion
        /// handles per pass, via ManagedLoopPlayer) instead of LitMotion's native Loops, since a single
        /// native multi-loop handle bakes one sample for every repeat. A managed-loop action can't be
        /// used in Sequential mode; MotionPlayerSession rejects it the same way it rejects an infinite
        /// native LoopCount.
        public bool RequiresManagedLoop
        {
            get
            {
                if (LoopCount == 1 || _randomizers == null) return false;
                foreach (var randomizer in _randomizers)
                    if (randomizer != null && randomizer.Enabled && randomizer.Policy == RandomizationSamplingPolicy.EveryLoop)
                        return true;
                return false;
            }
        }

        /// Authored duration of one loop pass, in seconds, for the Inspector caption — NOT scaled by the
        /// owning player's DurationMultiplier (playback-time concern; this is "what was authored"), and
        /// NOT multiplied by loop count, since GetProgress resets every loop and this stays consistent
        /// with that bar. Defaults to the common start delay; overridden by actions with their own
        /// notion of duration.
        public virtual double GetDuration() => ActionSettings.StartDelay;

        /// How many times this action repeats: 1 (default, no loop concept), >1 for a finite repeat
        /// count, <0 for an infinite loop — mirrors LitMotion's SerializableMotionSettings.Loops.
        /// Drives IsLooping and the "x3"/"x∞" Inspector caption suffix.
        public virtual int LoopCount => 1;

        /// True when this action is configured to repeat more than once — drives the looping
        /// progress-bar color in the Inspector.
        public bool IsLooping => LoopCount < 0 || LoopCount > 1;

        /// Checked by the Inspector (not the runtime) to surface authoring problems — e.g. a shader
        /// property name that doesn't exist on the assigned material — in the Debug panel. Null when
        /// there's nothing to report. Must be cheap and side-effect-free; runs even when nothing is
        /// playing, unlike IMotionActionRuntime.ValidateTarget().
        public virtual string Validate() => null;

        /// 0-1 progress of a currently-active handle, for the Inspector's per-action progress bar.
        /// Default: overall progress across the handle's whole (finite) timeline. Overridden by actions
        /// whose Settings has its own Delay/Duration — those use ComputeLoopProgress instead, so the
        /// bar resets every loop and stays meaningful for an infinitely-looping action. Only called
        /// while handle.IsActive().
        public virtual float GetProgress(in MotionHandle handle)
        {
            return !double.IsInfinity(handle.TotalDuration) && handle.TotalDuration > 0d
                ? Mathf.Clamp01((float)(handle.Time / handle.TotalDuration))
                : 0f;
        }

        /// Creates a fresh runtime instance for one Play() call. Must never return the same instance
        /// twice. context carries the owning FeelMotionPlayer's global duration/intensity multipliers.
        public abstract IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context);

        /// One loop pass's duration: the common start delay plus one Delay+Duration cycle — shared by
        /// every action whose duration comes from a LitMotion SerializableMotionSettings.
        protected static double ComputeMotionDuration(float delay, float duration, float startDelay)
            => startDelay + delay + duration;

        /// 0-1 progress within the current loop pass, from a handle's own per-loop Delay/Duration/Time
        /// — unlike Handle.Time/TotalDuration, these stay meaningful for an infinite loop count, since
        /// LitMotion still reports a single loop's own Delay/Duration regardless of repeat count.
        /// Mirrors LitMotion's own MotionData time-folding per delayType: EveryLoop repeats a
        /// Delay-then-Duration period every loop; FirstLoop pays the delay once, then every subsequent
        /// loop is Duration alone — this matters because FirstLoop is the default.
        protected static float ComputeLoopProgress(in MotionHandle handle, DelayType delayType)
        {
            double delay = handle.Delay;
            double duration = handle.Duration;
            if (duration <= 0d) return 0f;

            double currentLoopTime;
            if (delayType == DelayType.EveryLoop)
            {
                var period = delay + duration;
                currentLoopTime = period > 0d ? handle.Time % period - delay : handle.Time - delay;
            }
            else
            {
                currentLoopTime = (handle.Time - delay) % duration;
            }

            return currentLoopTime < 0d ? 0f : Mathf.Clamp01((float)(currentLoopTime / duration));
        }
    }
}
