using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// Emits particles (or restarts playback) on a ParticleSystem — not a tween. LitMotion.Animation has
    /// no particle support; implemented here so juice sequences can fire a burst at a specific point.
    [Serializable]
    [FeelMotionActionMenu("Particles/Emit")]
    public sealed class ParticleEmitAction : FeelMotionAction
    {
        enum Mode { Emit, Play }

        [SerializeField] ParticleSystem _target;
        [SerializeField] Mode _mode = Mode.Emit;
        [SerializeField, Min(0)] int _emitCount = 1;

        public ParticleEmitAction Configure(ParticleSystem target, int emitCount)
        {
            _target = target;
            _mode = Mode.Emit;
            _emitCount = emitCount;
            return this;
        }

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

        sealed class Runtime : IMotionActionRuntime
        {
            readonly ParticleEmitAction _action;
            readonly MotionPlaybackContext _context;

            public Runtime(ParticleEmitAction action, in MotionPlaybackContext context)
            {
                _action = action;
                _context = context;
            }

            public MotionActionState State { get; private set; } = MotionActionState.Idle;
            public MotionHandle Handle => _handle;
            public event Action Completed;

            MotionHandle _handle;

            public bool ValidateTarget() => _action._target != null;

            public void Play()
            {
                State = MotionActionState.Playing;

                var actionSettings = _action.ActionSettings;
                var delay = actionSettings.StartDelay * _context.DurationMultiplier;

                var builder = LMotion.Create(0f, 1f, delay).WithOnComplete(EmitAndComplete);
                if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled) builder = builder.WithScheduler(actionSettings.ResolveScheduler());

                _handle = builder.RunWithoutBinding();
            }

            void EmitAndComplete()
            {
                var target = _action._target;
                if (target != null)
                {
                    if (_action._mode == Mode.Emit) target.Emit(_action._emitCount);
                    else target.Play();
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

            public void RestoreInitialValues() { } // nothing to restore — emission is fire-and-forget
        }
    }
}
