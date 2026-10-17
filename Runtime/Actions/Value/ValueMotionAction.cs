using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Base for actions that tween a bare value and broadcast it each tick instead of writing it onto a
    /// UnityEngine.Object target — e.g. driving a shader/UI property via UnityEvent wiring in the
    /// Inspector. There is no target, so there's nothing to capture/restore and no global
    /// IntensityMultiplier/MotionValueMode to apply — those only make sense relative to a captured
    /// property value (see PropertyMotionAction). A subclass only has to broadcast the tweened value.
    /// </summary>
    [Serializable]
    public abstract class ValueMotionAction<TValue, TOptions, TAdapter> : FeelMotionAction
        where TValue : unmanaged
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
    {
        [SerializeField] SerializableMotionSettings<TValue, TOptions> _settings;

        public SerializableMotionSettings<TValue, TOptions> Settings => _settings;

        public override double GetDuration() => _settings == null
            ? ActionSettings.StartDelay
            : ComputeMotionDuration(_settings.Delay, _settings.Duration, ActionSettings.StartDelay);

        public override int LoopCount => _settings?.Loops ?? 1;

        public override float GetProgress(in MotionHandle handle)
            => ComputeLoopProgress(handle, _settings?.DelayType ?? DelayType.FirstLoop);

        /// <summary>Broadcasts one tweened value — typically invokes a serialized UnityEvent&lt;TValue&gt;.</summary>
        protected abstract void InvokeChanged(TValue value);

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

        sealed class Runtime : IMotionActionRuntime
        {
            readonly ValueMotionAction<TValue, TOptions, TAdapter> _action;
            readonly MotionPlaybackContext _context;

            public Runtime(ValueMotionAction<TValue, TOptions, TAdapter> action, in MotionPlaybackContext context)
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

                var settings = _action._settings;
                var durationMultiplier = _context.DurationMultiplier;
                if (durationMultiplier != 1f)
                    settings = settings with { Duration = settings.Duration * durationMultiplier, Delay = settings.Delay * durationMultiplier };

                var actionSettings = _action.ActionSettings;
                if (actionSettings.StartDelay > 0f)
                    settings = settings with { Delay = settings.Delay + actionSettings.StartDelay * durationMultiplier };
                if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled)
                    settings = settings with { Scheduler = actionSettings.ResolveScheduler() };

                _handle = LMotion.Create<TValue, TOptions, TAdapter>(settings)
                    .WithOnComplete(OnLitMotionComplete)
                    .Bind(this, static (x, state) => state._action.InvokeChanged(x));
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
