using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Serialized, authored configuration for one action in a FeelMotionPlayer's sequence. Holds no
    /// runtime state (handles, captured values) — that lives on the IMotionActionRuntime that
    /// CreateRuntime() returns fresh for every Play() call. See Documentation/Architecture.md.
    /// </summary>
    [Serializable]
    public abstract class FeelMotionAction
    {
        [SerializeField] string _displayName;
        [SerializeField] bool _enabled = true;
        [SerializeField] ActionSettings _actionSettings = new();

        public bool Enabled => _enabled;

        public string DisplayName => string.IsNullOrEmpty(_displayName) ? GetType().Name : _displayName;

        /// <summary>Common playback knobs (start delay, timescale mode) shared by every action type —
        /// see Documentation/Architecture.md.</summary>
        public ActionSettings ActionSettings => _actionSettings;

        /// <summary>Authored duration of one loop pass, in seconds, for the Inspector caption — NOT
        /// scaled by the owning player's DurationMultiplier (that's a playback-time concern, this is
        /// "what was authored"), and NOT multiplied by loop count: a looping action's progress bar
        /// (GetProgress) resets every loop, so the duration shown next to it is the length of a single
        /// cycle, consistent with that bar — not a total that would be infinite for an infinitely
        /// looping action. Defaults to just the common start delay; overridden by actions that have
        /// their own notion of duration (a tween's own settings, a plain Delay action, ...).</summary>
        public virtual double GetDuration() => ActionSettings.StartDelay;

        /// <summary>How many times this action repeats: 1 (default — no loop concept) for a one-shot
        /// action, &gt;1 for a finite repeat count, &lt;0 for an infinite loop — mirrors LitMotion's own
        /// SerializableMotionSettings.Loops convention. Drives both IsLooping and the "x3"/"x∞" suffix
        /// on the Inspector caption. Overridden by actions with a loop/repeat concept.</summary>
        public virtual int LoopCount => 1;

        /// <summary>True when this action is configured to repeat more than once — drives the looping
        /// progress-bar color in the Inspector.</summary>
        public bool IsLooping => LoopCount < 0 || LoopCount > 1;

        /// <summary>Checked by the Inspector (not the runtime) to surface authoring problems — e.g. a
        /// shader property name that doesn't exist on the assigned material — as an error in the
        /// Debug panel. Returns null when there's nothing to report. Must be cheap and
        /// side-effect-free; unlike IMotionActionRuntime.ValidateTarget() this runs even when nothing
        /// is playing.</summary>
        public virtual string Validate() => null;

        /// <summary>0-1 progress of a currently-active handle, for the Inspector's per-action progress
        /// bar. Default: overall progress across the handle's whole (necessarily finite) timeline.
        /// Overridden by actions whose Settings has its own Delay/Duration — those use the per-loop
        /// progress (ComputeLoopProgress) instead, so the bar resets every loop and stays meaningful
        /// even for an infinitely-looping action, where an overall "whole timeline" progress is
        /// impossible to express. Only called while <paramref name="handle"/>.IsActive().</summary>
        public virtual float GetProgress(in MotionHandle handle)
        {
            return !double.IsInfinity(handle.TotalDuration) && handle.TotalDuration > 0d
                ? Mathf.Clamp01((float)(handle.Time / handle.TotalDuration))
                : 0f;
        }

        /// <summary>Creates a fresh runtime instance for one Play() call. Must never return the same instance twice.
        /// <paramref name="context"/> carries the owning FeelMotionPlayer's global duration/intensity multipliers.</summary>
        public abstract IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context);

        /// <summary>One loop pass's duration: the common start delay plus one Delay+Duration cycle —
        /// shared by every action whose duration comes from a LitMotion SerializableMotionSettings.</summary>
        protected static double ComputeMotionDuration(float delay, float duration, float startDelay)
            => startDelay + delay + duration;

        /// <summary>0-1 progress within the current loop pass, from a handle's own per-loop Delay/
        /// Duration/Time — unlike Handle.Time/TotalDuration, these stay meaningful for an infinite loop
        /// count, since LitMotion still reports a single loop's own Delay/Duration regardless of how
        /// many times it repeats. Mirrors LitMotion's own MotionData time-folding per
        /// <paramref name="delayType"/>: EveryLoop repeats a Delay-then-Duration period every loop;
        /// FirstLoop pays the delay once, then every subsequent loop is Duration alone — getting this
        /// right matters because FirstLoop is the default, so most looping actions use it.</summary>
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
