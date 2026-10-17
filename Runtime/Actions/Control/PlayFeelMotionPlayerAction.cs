using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Plays another FeelMotionPlayer and completes when that player's own playback finishes — lets one
    /// player kick off another instead of duplicating its action list. Mirrors LitMotion.Animation's
    /// "Play LitMotion Animation" control component.
    /// </summary>
    [Serializable]
    [FeelMotionActionMenu("Control/Play FeelMotion Player")]
    public sealed class PlayFeelMotionPlayerAction : FeelMotionAction
    {
        [SerializeField] FeelMotionPlayer _target;

        public PlayFeelMotionPlayerAction Configure(FeelMotionPlayer target)
        {
            _target = target;
            return this;
        }

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

        sealed class Runtime : IMotionActionRuntime
        {
            readonly PlayFeelMotionPlayerAction _action;
            readonly MotionPlaybackContext _context;

            public Runtime(PlayFeelMotionPlayerAction action, in MotionPlaybackContext context)
            {
                _action = action;
                _context = context;
            }

            public MotionActionState State { get; private set; } = MotionActionState.Idle;
            public MotionHandle Handle => _handle;
            public event Action Completed;

            MotionHandle _handle;
            bool _subscribed;

            public bool ValidateTarget() => _action._target != null;

            public void Play()
            {
                State = MotionActionState.Playing;

                var target = _action._target;
                if (target != null)
                {
                    target.Completed += OnTargetCompleted;
                    _subscribed = true;
                    target.Play();
                }

                // A zero-duration handle just carries this action's own completion signal through the
                // normal MotionHandle machinery (sequencing, Pause/Resume/Stop) — see EventAction.
                _handle = LMotion.Create(0f, 1f, 0f).RunWithoutBinding();

                if (target == null) CompleteNow();
            }

            void OnTargetCompleted()
            {
                Unsubscribe();
                CompleteNow();
            }

            void CompleteNow()
            {
                State = MotionActionState.Completed;
                Completed?.Invoke();
            }

            void Unsubscribe()
            {
                if (!_subscribed) return;
                _subscribed = false;
                var target = _action._target;
                if (target != null) target.Completed -= OnTargetCompleted;
            }

            public void Pause()
            {
                _action._target?.Pause();
                if (_handle.IsActive()) _handle.PlaybackSpeed = 0f;
                State = MotionActionState.Paused;
            }

            public void Resume()
            {
                _action._target?.Play();
                if (_handle.IsActive()) _handle.PlaybackSpeed = 1f;
                State = MotionActionState.Playing;
            }

            public void Stop(MotionStopMode mode)
            {
                Unsubscribe();

                if (mode is MotionStopMode.Cancel or MotionStopMode.CancelAndRestore)
                {
                    if (mode == MotionStopMode.CancelAndRestore) _action._target?.Rewind();
                    else _action._target?.Drop();
                    _handle.TryCancel();
                    State = MotionActionState.Cancelled;
                }
                else
                {
                    // Complete modes don't force the target player to jump to its end state
                    // (FeelMotionPlayer exposes no such API) — they just stop waiting on it.
                    _handle.TryComplete();
                }
            }

            public void RestoreInitialValues() { } // the target player restores its own actions
        }
    }
}
