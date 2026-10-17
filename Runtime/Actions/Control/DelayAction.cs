using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// A pure wait, with no target. Lets a Sequential sequence have a gap at a specific point
    /// without needing a separate per-sequence delay setting on the player.
    /// </summary>
    [Serializable]
    [FeelMotionActionMenu("Control/Delay")]
    public sealed class DelayAction : FeelMotionAction
    {
        [SerializeField, Min(0f)] float _duration = 1f;

        public float Duration => _duration;

        public override double GetDuration() => ActionSettings.StartDelay + _duration;

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

        sealed class Runtime : IMotionActionRuntime
        {
            readonly DelayAction _action;
            readonly MotionPlaybackContext _context;

            public Runtime(DelayAction action, in MotionPlaybackContext context)
            {
                _action = action;
                _context = context;
            }

            public MotionActionState State { get; private set; } = MotionActionState.Idle;
            public MotionHandle Handle => _handle;
            public event Action Completed;

            MotionHandle _handle;

            public bool ValidateTarget() => true;

            public void Play()
            {
                State = MotionActionState.Playing;

                var durationMultiplier = _context.DurationMultiplier;
                var builder = LMotion.Create(0f, 1f, _action.Duration * durationMultiplier)
                    .WithOnComplete(OnLitMotionComplete);

                var actionSettings = _action.ActionSettings;
                if (actionSettings.StartDelay > 0f) builder = builder.WithDelay(actionSettings.StartDelay * durationMultiplier);
                if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled) builder = builder.WithScheduler(actionSettings.ResolveScheduler());

                _handle = builder.RunWithoutBinding();
            }

            void OnLitMotionComplete()
            {
                State = MotionActionState.Completed;
                Completed?.Invoke();
            }

            public void Pause()
            {
                if (_handle.IsActive()) _handle.PlaybackSpeed = 0f;
                State = MotionActionState.Paused;
            }

            public void Resume()
            {
                if (_handle.IsActive()) _handle.PlaybackSpeed = 1f;
                State = MotionActionState.Playing;
            }

            public void Stop(MotionStopMode mode)
            {
                if (mode is MotionStopMode.Cancel or MotionStopMode.CancelAndRestore)
                {
                    _handle.TryCancel();
                    State = MotionActionState.Cancelled;
                }
                else
                {
                    _handle.TryComplete();
                }
            }

            public void RestoreInitialValues() { } // nothing to restore — there's no target
        }
    }
}
