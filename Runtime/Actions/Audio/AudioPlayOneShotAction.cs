#if FEELMOTION_UNITY_AUDIO
using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Plays a one-shot clip on an AudioSource — not a tween. LitMotion.Animation's audio components
    /// only animate continuous Volume/Pitch; one-shot SFX playback is genuinely missing there, so this
    /// is implemented from scratch for FeelMotion.
    ///
    /// Unlike EventAction/DelayAction (which fire their effect immediately in Play() and only use
    /// ActionSettings.StartDelay to time when they report Completed, for sequencing), this action's
    /// ActionSettings.StartDelay is a *real* playback delay — PlayOneShot itself is deferred, because a
    /// sound effect's whole purpose here is to land at a specific offset (e.g. a "woosh" that trails a
    /// "quack" by 0.1s inside the same Parallel-mode player).
    /// </summary>
    [Serializable]
    [FeelMotionActionMenu("Audio/Play One Shot")]
    public sealed class AudioPlayOneShotAction : FeelMotionAction
    {
        [SerializeField] AudioSource _target;
        [SerializeField] AudioClip _clip;
        [SerializeField, Range(0f, 1f)] float _volume = 1f;
        [SerializeField] bool _randomizePitch;
        [SerializeField] float _minPitch = 0.9f;
        [SerializeField] float _maxPitch = 1.1f;

        public AudioPlayOneShotAction Configure(AudioSource target, AudioClip clip, float volume = 1f, bool randomizePitch = false, float minPitch = 0.9f, float maxPitch = 1.1f)
        {
            _target = target;
            _clip = clip;
            _volume = volume;
            _randomizePitch = randomizePitch;
            _minPitch = minPitch;
            _maxPitch = maxPitch;
            return this;
        }

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

        sealed class Runtime : IMotionActionRuntime
        {
            readonly AudioPlayOneShotAction _action;
            readonly MotionPlaybackContext _context;

            public Runtime(AudioPlayOneShotAction action, in MotionPlaybackContext context)
            {
                _action = action;
                _context = context;
            }

            public MotionActionState State { get; private set; } = MotionActionState.Idle;
            public MotionHandle Handle => _handle;
            public event Action Completed;

            MotionHandle _handle;

            public bool ValidateTarget() => _action._target != null && _action._clip != null;

            public void Play()
            {
                State = MotionActionState.Playing;

                var actionSettings = _action.ActionSettings;
                var delay = actionSettings.StartDelay * _context.DurationMultiplier;

                var builder = LMotion.Create(0f, 1f, delay).WithOnComplete(PlayClipAndComplete);
                if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled) builder = builder.WithScheduler(actionSettings.ResolveScheduler());

                _handle = builder.RunWithoutBinding();
            }

            void PlayClipAndComplete()
            {
                var target = _action._target;
                var clip = _action._clip;
                if (target != null && clip != null)
                {
                    if (_action._randomizePitch) target.pitch = UnityEngine.Random.Range(_action._minPitch, _action._maxPitch);
                    target.PlayOneShot(clip, _action._volume);
                }

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

            public void RestoreInitialValues() { } // nothing to restore — playback is fire-and-forget
        }
    }
}
#endif
