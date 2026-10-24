using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// Base for actions that tween a bare value and broadcast it each tick instead of writing it onto a
    /// UnityEngine.Object target. No target means nothing to capture/restore and no IntensityMultiplier/
    /// MotionValueMode to apply (those only make sense relative to a captured property — PropertyMotionAction).
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

        /// Broadcasts one tweened value — typically invokes a serialized UnityEvent&lt;TValue&gt;.
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
            public MotionHandle Handle => _managedLoop?.Handle ?? _handle;
            public event Action Completed;

            MotionHandle _handle;
            ManagedLoopPlayer _managedLoop;

            public bool ValidateTarget() => true;

            public void Play()
            {
                State = MotionActionState.Playing;

                var baseSettings = _action._settings;
                var durationMultiplier = _context.DurationMultiplier;
                var actionSettings = _action.ActionSettings;
                var randomization = RandomizationSet.CreateOrNull(_action.Randomizers);

                // See PropertyMotionActionRuntime.Play()/Documentation/Architecture.md's Randomization
                // section — an EveryLoop module can't reuse one baked LitMotion handle across repeats.
                if (_action.RequiresManagedLoop)
                {
                    _managedLoop = new ManagedLoopPlayer(
                        isFirstIteration => BuildIterationHandle(baseSettings, actionSettings, durationMultiplier, isFirstIteration, randomization),
                        _action.LoopCount,
                        OnManagedLoopComplete);
                    _managedLoop.Play();
                    return;
                }

                var settings = durationMultiplier != 1f
                    ? baseSettings with { Duration = baseSettings.Duration * durationMultiplier, Delay = baseSettings.Delay * durationMultiplier }
                    : baseSettings;

                if (actionSettings.StartDelay > 0f)
                    settings = settings with { Delay = settings.Delay + actionSettings.StartDelay * durationMultiplier };
                if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled)
                    settings = settings with { Scheduler = actionSettings.ResolveScheduler() };

                // Zero-overhead when unused — see PropertyMotionActionRuntime.Play()'s matching comment.
                if (randomization != null)
                {
                    settings = settings with
                    {
                        StartValue = randomization.ApplyValue(RandomizedParameter.StartValue, settings.StartValue),
                        EndValue = randomization.ApplyValue(RandomizedParameter.EndValue, settings.EndValue),
                        Duration = Mathf.Max(0.0001f, randomization.ApplyScalar(RandomizedParameter.Duration, settings.Duration)),
                        Delay = Mathf.Max(0f, randomization.ApplyScalar(RandomizedParameter.Delay, settings.Delay))
                    };
                }

                _handle = LMotion.Create<TValue, TOptions, TAdapter>(settings)
                    .WithOnComplete(OnLitMotionComplete)
                    .Bind(this, static (x, state) => state._action.InvokeChanged(x));
            }

            MotionHandle BuildIterationHandle(
                SerializableMotionSettings<TValue, TOptions> baseSettings,
                ActionSettings actionSettings,
                float durationMultiplier,
                bool isFirstIteration,
                RandomizationSet randomization)
            {
                var resolved = MotionSettingsResolver.Resolve(baseSettings, actionSettings, durationMultiplier, isFirstIteration, loops: 1, randomization);
                return LMotion.Create<TValue, TOptions, TAdapter>(resolved)
                    .WithOnComplete(_managedLoop.OnIterationComplete)
                    .Bind(this, static (x, state) => state._action.InvokeChanged(x));
            }

            void OnManagedLoopComplete()
            {
                State = MotionActionState.Completed;
                Completed?.Invoke();
            }

            void OnLitMotionComplete()
            {
                State = MotionActionState.Completed;
                Completed?.Invoke();
            }

            public void Pause()
            {
                if (_managedLoop != null) _managedLoop.Pause();
                else if (_handle.IsActive()) _handle.PlaybackSpeed = 0f;
                State = MotionActionState.Paused;
            }

            public void Resume()
            {
                if (_managedLoop != null) _managedLoop.Resume();
                else if (_handle.IsActive()) _handle.PlaybackSpeed = 1f;
                State = MotionActionState.Playing;
            }

            public void Stop(MotionStopMode mode)
            {
                var complete = mode is MotionStopMode.Complete or MotionStopMode.CompleteAndRestore;

                if (_managedLoop != null)
                {
                    _managedLoop.Stop(complete);
                    if (!complete) State = MotionActionState.Cancelled;
                }
                else if (!complete)
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
