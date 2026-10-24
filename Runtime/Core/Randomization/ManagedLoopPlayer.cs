using System;
using LitMotion;

namespace FeelMotion
{
    /// <summary>
    /// Drives "every loop" re-randomization by creating one fresh single-loop LitMotion handle per
    /// iteration instead of relying on LitMotion's own native Loops (which bakes one set of values for
    /// every repeat). Owned by a PropertyMotionActionRuntime/ValueMotionAction.Runtime whose action has
    /// FeelMotionAction.RequiresManagedLoop. Can't be folded into a Sequential LSequence — each
    /// iteration's handle is created lazily, as the previous one completes, not all up front at Play()
    /// time — so MotionPlayerSession rejects this combination the same way it already rejects an
    /// infinite native LoopCount in Sequential mode. See Documentation/Architecture.md's Randomization
    /// section. Public (not internal) — PropertyMotionAction (FeelMotion.asmdef) and ValueMotionAction
    /// (the separate FeelMotion.Value.asmdef, Architecture.md §9) both construct one.
    /// </summary>
    public sealed class ManagedLoopPlayer
    {
        readonly Func<bool, MotionHandle> _playOneLoop;
        readonly int _loopCount; // mirrors FeelMotionAction.LoopCount: >=1 is a finite count, <0 is infinite
        readonly Action _onCompleted;

        int _completedLoops;
        bool _stopped;

        public MotionHandle Handle { get; private set; }

        /// <summary><paramref name="playOneLoop"/> builds and binds exactly one single-loop handle,
        /// wiring its own completion back to OnIterationComplete; its bool argument is true only for
        /// the very first iteration (the one that pays ActionSettings.StartDelay and a FirstLoop
        /// delay).</summary>
        public ManagedLoopPlayer(Func<bool, MotionHandle> playOneLoop, int loopCount, Action onCompleted)
        {
            _playOneLoop = playOneLoop;
            _loopCount = loopCount;
            _onCompleted = onCompleted;
        }

        public void Play() => Handle = _playOneLoop(true);

        /// <summary>Wired as each iteration's own WithOnComplete callback — called by LitMotion when
        /// one single-loop pass finishes. Never call this directly.</summary>
        public void OnIterationComplete()
        {
            if (_stopped) return;
            _completedLoops++;

            if (_loopCount >= 0 && _completedLoops >= _loopCount)
            {
                _onCompleted?.Invoke();
                return;
            }

            Handle = _playOneLoop(false);
        }

        public void Pause()
        {
            // Handle is a property, not a field — `Handle.PlaybackSpeed = x` would try to mutate the
            // getter's return value (CS1612). PlaybackSpeed's setter writes through the handle's own
            // internal id regardless of which copy calls it, so reading into a local first is enough;
            // there's nothing to write back since the id/identity itself never changes.
            var handle = Handle;
            if (handle.IsActive()) handle.PlaybackSpeed = 0f;
        }

        public void Resume()
        {
            var handle = Handle;
            if (handle.IsActive()) handle.PlaybackSpeed = 1f;
        }

        /// <summary>Stops spawning further iterations and resolves the current one —
        /// <paramref name="complete"/> true completes it in place (MotionStopMode.Complete*), false
        /// cancels it (MotionStopMode.Cancel*).</summary>
        public void Stop(bool complete)
        {
            _stopped = true;
            if (complete) Handle.TryComplete();
            else Handle.TryCancel();
        }
    }
}
