using System;
using LitMotion;

namespace FeelMotion
{
    /// Drives "every loop" re-randomization by creating one fresh single-loop LitMotion handle per
    /// iteration, instead of relying on native Loops (which bakes one set of values for every repeat).
    /// Each iteration's handle is created lazily as the previous one completes, not up front at Play(),
    /// so it can't be folded into a Sequential LSequence — MotionPlayerSession rejects that combination.
    /// Public because both PropertyMotionAction and ValueMotionAction (separate asmdefs) construct one.
    public sealed class ManagedLoopPlayer
    {
        readonly Func<bool, MotionHandle> _playOneLoop;
        readonly int _loopCount; // mirrors FeelMotionAction.LoopCount: >=1 is a finite count, <0 is infinite
        readonly Action _onCompleted;

        int _completedLoops;
        bool _stopped;

        public MotionHandle Handle { get; private set; }

        /// `playOneLoop` builds and binds one single-loop handle, wiring completion back to
        /// OnIterationComplete; its bool argument is true only for the first iteration (the one paying
        /// ActionSettings.StartDelay and a FirstLoop delay).
        public ManagedLoopPlayer(Func<bool, MotionHandle> playOneLoop, int loopCount, Action onCompleted)
        {
            _playOneLoop = playOneLoop;
            _loopCount = loopCount;
            _onCompleted = onCompleted;
        }

        public void Play() => Handle = _playOneLoop(true);

        /// Wired as each iteration's WithOnComplete callback. Never call this directly.
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
            // getter's return value (CS1612). PlaybackSpeed's setter writes through the handle's own id
            // regardless of which copy calls it, so reading into a local first is enough.
            var handle = Handle;
            if (handle.IsActive()) handle.PlaybackSpeed = 0f;
        }

        public void Resume()
        {
            var handle = Handle;
            if (handle.IsActive()) handle.PlaybackSpeed = 1f;
        }

        /// Stops spawning further iterations and resolves the current one — `complete` true completes
        /// it in place (MotionStopMode.Complete*), false cancels it (MotionStopMode.Cancel*).
        public void Stop(bool complete)
        {
            _stopped = true;
            if (complete) Handle.TryComplete();
            else Handle.TryCancel();
        }
    }
}
