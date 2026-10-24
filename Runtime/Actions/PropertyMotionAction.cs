using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Base for actions that tween one property on one UnityEngine.Object target. Config lives here
    /// (serialized, authored); captured values/handle/state live on PropertyMotionActionRuntime,
    /// created fresh per Play() — see Documentation/Architecture.md.
    /// </summary>
    [Serializable]
    public abstract class PropertyMotionAction<TObject, TValue, TOptions, TAdapter> : FeelMotionAction
        where TObject : UnityEngine.Object
        where TValue : unmanaged
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
    {
        [SerializeField] TObject _target;
        [SerializeField] MotionValueMode _valueMode = MotionValueMode.Absolute;
        [SerializeField] MotionValueCombineMode _combineMode = MotionValueCombineMode.Add;
        [SerializeField] SerializableMotionSettings<TValue, TOptions> _settings;

        public TObject Target => _target;
        public MotionValueMode ValueMode => _valueMode;

        /// <summary>How RelativeToCurrent combines the captured initial value with the authored
        /// EndValue — Add (offset) or Multiply (factor, component-wise for vectors/color). Ignored by
        /// Absolute/FromCurrentTo, which never combine the initial value with EndValue.</summary>
        public MotionValueCombineMode CombineMode => _combineMode;

        public SerializableMotionSettings<TValue, TOptions> Settings => _settings;

        public override double GetDuration() => _settings == null
            ? ActionSettings.StartDelay
            : ComputeMotionDuration(_settings.Delay, _settings.Duration, ActionSettings.StartDelay);

        public override int LoopCount => _settings?.Loops ?? 1;

        public override float GetProgress(in MotionHandle handle)
            => ComputeLoopProgress(handle, _settings?.DelayType ?? DelayType.FirstLoop);

        /// <summary>Configures this action from code instead of the Inspector — for runtime-built
        /// sequences and tests. Returns this instance for fluent chaining.</summary>
        public PropertyMotionAction<TObject, TValue, TOptions, TAdapter> Configure(
            TObject target, MotionValueMode valueMode, SerializableMotionSettings<TValue, TOptions> settings,
            MotionValueCombineMode combineMode = MotionValueCombineMode.Add)
        {
            _target = target;
            _valueMode = valueMode;
            _combineMode = combineMode;
            _settings = settings;
            return this;
        }

        /// <summary>Reads the property's current value from the target.</summary>
        protected internal abstract TValue GetValue(TObject target);

        /// <summary>Writes a value to the property on the target.</summary>
        protected internal abstract void SetValue(TObject target, in TValue value);

        /// <summary>Combines a captured base value with an authored delta as an offset — used by
        /// MotionValueMode.RelativeToCurrent when CombineMode is Add.</summary>
        protected internal abstract TValue Add(in TValue baseValue, in TValue delta);

        /// <summary>Combines a captured base value with an authored delta as a factor (component-wise
        /// for vectors/color) — used by MotionValueMode.RelativeToCurrent when CombineMode is Multiply.
        /// Needed for scale-like properties, where Add distorts rather than scales.</summary>
        protected internal abstract TValue Multiply(in TValue baseValue, in TValue factor);

        /// <summary>Returns the point reached when traveling from <paramref name="from"/> toward
        /// <paramref name="to"/> scaled by <paramref name="factor"/> — <c>factor == 1</c> returns
        /// <paramref name="to"/> unchanged, <c>0</c> returns <paramref name="from"/>. Backs the global
        /// intensity multiplier (Documentation/Architecture.md).</summary>
        protected internal abstract TValue ScaleReach(in TValue from, in TValue to, float factor);

        public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context)
            => new PropertyMotionActionRuntime<TObject, TValue, TOptions, TAdapter>(this, context);
    }

    /// <summary>
    /// Per-Play() runtime for a PropertyMotionAction: owns the captured initial value and the
    /// MotionHandle for exactly one play. Never reused across replays, so overlapping/repeated
    /// Play() calls can't corrupt each other's state.
    /// </summary>
    internal sealed class PropertyMotionActionRuntime<TObject, TValue, TOptions, TAdapter> : IMotionActionRuntime
        where TObject : UnityEngine.Object
        where TValue : unmanaged
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<TValue, TOptions>
    {
        readonly PropertyMotionAction<TObject, TValue, TOptions, TAdapter> _action;
        readonly MotionPlaybackContext _context;
        TValue _initialValue;
        bool _hasInitialValue;

        public PropertyMotionActionRuntime(PropertyMotionAction<TObject, TValue, TOptions, TAdapter> action, in MotionPlaybackContext context)
        {
            _action = action;
            _context = context;
        }

        public MotionActionState State { get; private set; } = MotionActionState.Idle;
        public MotionHandle Handle => _managedLoop?.Handle ?? _handle;
        public event Action Completed;

        MotionHandle _handle;
        ManagedLoopPlayer _managedLoop;

        public bool ValidateTarget()
        {
            if (_action.Target == null) return false;

            // A non-null target with a reported authoring problem (e.g. a missing shader property)
            // is a real error, not "nothing to do" — throwing routes it through the session's existing
            // per-action error isolation (MotionPlayerSession -> FeelMotionPlayer.ActionError ->
            // Debug.LogException), the same path a thrown CreateRuntime()/ValidateTarget() already uses.
            var error = _action.Validate();
            if (error != null) throw new InvalidOperationException(error);

            return true;
        }

        public void Play()
        {
            var target = _action.Target;
            _initialValue = _action.GetValue(target);
            _hasInitialValue = true;

            var settings = _action.Settings;
            TValue start;
            TValue end;

            switch (_action.ValueMode)
            {
                case MotionValueMode.RelativeToCurrent:
                    start = _initialValue;
                    end = _action.CombineMode == MotionValueCombineMode.Multiply
                        ? _action.Multiply(_initialValue, settings.EndValue)
                        : _action.Add(_initialValue, settings.EndValue);
                    break;
                case MotionValueMode.FromCurrentTo:
                    start = _initialValue;
                    end = settings.EndValue;
                    break;
                default: // Absolute
                    start = settings.StartValue;
                    end = settings.EndValue;
                    break;
            }

            // Global intensity multiplier scales how far the value travels from `start` toward `end` —
            // 1 (default) leaves `end` untouched, so this never perturbs existing behavior.
            if (_context.IntensityMultiplier != 1f)
                end = _action.ScaleReach(start, end, _context.IntensityMultiplier);

            State = MotionActionState.Playing;

            var baseSettings = settings with { StartValue = start, EndValue = end };
            var durationMultiplier = _context.DurationMultiplier;
            var actionSettings = _action.ActionSettings;
            var randomization = RandomizationSet.CreateOrNull(_action.Randomizers);

            // A module sampling EveryLoop can't reuse one baked LitMotion handle across repeats — see
            // Documentation/Architecture.md's Randomization section. This action's own LoopCount==1
            // check is folded into RequiresManagedLoop, so a non-looping randomized action still takes
            // the plain path below.
            if (_action.RequiresManagedLoop)
            {
                _managedLoop = new ManagedLoopPlayer(
                    isFirstIteration => BuildIterationHandle(baseSettings, actionSettings, durationMultiplier, isFirstIteration, randomization),
                    _action.LoopCount,
                    OnManagedLoopComplete);
                _managedLoop.Play();
                return;
            }

            var motionSettings = durationMultiplier != 1f
                ? baseSettings with { Duration = baseSettings.Duration * durationMultiplier, Delay = baseSettings.Delay * durationMultiplier }
                : baseSettings;

            if (actionSettings.StartDelay > 0f)
                motionSettings = motionSettings with { Delay = motionSettings.Delay + actionSettings.StartDelay * durationMultiplier };
            if (actionSettings.TimescaleMode != MotionTimescaleMode.Scaled)
                motionSettings = motionSettings with { Scheduler = actionSettings.ResolveScheduler() };

            // Zero-overhead when unused: randomization is null whenever the action has no enabled
            // modules, and everything above this point is unchanged from before randomization existed.
            if (randomization != null)
            {
                motionSettings = motionSettings with
                {
                    StartValue = randomization.ApplyValue(RandomizedParameter.StartValue, motionSettings.StartValue),
                    EndValue = randomization.ApplyValue(RandomizedParameter.EndValue, motionSettings.EndValue),
                    Duration = Mathf.Max(0.0001f, randomization.ApplyScalar(RandomizedParameter.Duration, motionSettings.Duration)),
                    Delay = Mathf.Max(0f, randomization.ApplyScalar(RandomizedParameter.Delay, motionSettings.Delay))
                };
            }

            _handle = LMotion.Create<TValue, TOptions, TAdapter>(motionSettings)
                .WithOnComplete(OnLitMotionComplete)
                .Bind(this, static (x, state) => state.ApplyValue(x));
        }

        /// <summary>Builds and binds exactly one single-loop handle for ManagedLoopPlayer — used only
        /// when _action.RequiresManagedLoop. isFirstIteration mirrors ManagedLoopPlayer's own contract:
        /// true only for the very first pass, so ActionSettings.StartDelay and a FirstLoop delay are
        /// paid once, not every iteration.</summary>
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
                .Bind(this, static (x, state) => state.ApplyValue(x));
        }

        void OnManagedLoopComplete()
        {
            State = MotionActionState.Completed;
            Completed?.Invoke();
        }

        void ApplyValue(TValue value)
        {
            var target = _action.Target;
            if (target == null) return;
            _action.SetValue(target, value);
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
                _managedLoop.Stop(complete);
            else if (!complete)
                _handle.TryCancel();
            else
                _handle.TryComplete();

            if (mode is MotionStopMode.CancelAndRestore or MotionStopMode.CompleteAndRestore)
                RestoreInitialValues();

            if (mode is MotionStopMode.Cancel or MotionStopMode.CancelAndRestore)
                State = MotionActionState.Cancelled;
        }

        public void RestoreInitialValues()
        {
            if (!_hasInitialValue) return;
            var target = _action.Target;
            if (target == null) return;
            _action.SetValue(target, _initialValue);
        }
    }
}
