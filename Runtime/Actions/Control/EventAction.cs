using System;
using LitMotion;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    /// <summary>
    /// Fires UnityEvents instead of tweening anything. Completes immediately but still produces a
    /// MotionHandle, so it sequences alongside real tweens with no special-casing — see
    /// Documentation/Architecture.md §4.
    /// </summary>
    [Serializable]
    [FeelMotionActionMenu("Control/Event")]
    public sealed class EventAction : FeelMotionAction
    {
        [SerializeField] UnityEvent _onPlay;
        [SerializeField] UnityEvent _onStop;

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

        sealed class Runtime : IMotionActionRuntime
        {
            readonly EventAction _action;
            readonly MotionPlaybackContext _context;

            public Runtime(EventAction action, in MotionPlaybackContext context)
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
                _action._onPlay?.Invoke();
                State = MotionActionState.Playing;

                var builder = LMotion.Create(0f, 1f, 0f)
                    .WithOnComplete(OnLitMotionComplete);

                var actionSettings = _action.ActionSettings;
                if (actionSettings.StartDelay > 0f) builder = builder.WithDelay(actionSettings.StartDelay * _context.DurationMultiplier);
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
                _action._onStop?.Invoke();

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
