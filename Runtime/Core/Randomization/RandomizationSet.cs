using System;
using System.Collections.Generic;

namespace FeelMotion
{
    /// Per-Play() aggregation of one action's enabled FeelMotionRandomizer modules — owns one
    /// RandomState per module and, for OncePerPlay modules, the first-drawn sample, so later
    /// managed-loop iterations reuse the same numbers instead of re-rolling. Created fresh by whichever
    /// IMotionActionRuntime.Play() owns it, never shared across Play() calls. Public because both
    /// PropertyMotionAction and ValueMotionAction (separate asmdefs) need it.
    public sealed class RandomizationSet
    {
        sealed class ModuleState
        {
            public FeelMotionRandomizer Module;
            public RandomState Rng;

            // Raw per-component samples (pre-combination), cached only for OncePerPlay so later calls
            // (further managed-loop iterations, or a second ApplyValue call for the same parameter)
            // reuse the exact same draw instead of re-rolling.
            public float[] CachedSample;
        }

        readonly List<ModuleState> _modules;

        /// True when any enabled module uses EveryLoop. Mirrors FeelMotionAction.RequiresManagedLoop,
        /// kept here too for convenience once a RandomizationSet already exists.
        public bool HasPerLoopRandomization { get; }

        public static RandomizationSet CreateOrNull(IReadOnlyList<FeelMotionRandomizer> randomizers)
        {
            if (randomizers == null || randomizers.Count == 0) return null;

            List<ModuleState> modules = null;
            foreach (var randomizer in randomizers)
            {
                if (randomizer == null || !randomizer.Enabled) continue;
                (modules ??= new List<ModuleState>()).Add(new ModuleState { Module = randomizer, Rng = randomizer.CreateState() });
            }

            return modules == null ? null : new RandomizationSet(modules);
        }

        RandomizationSet(List<ModuleState> modules)
        {
            _modules = modules;
            foreach (var state in modules)
            {
                if (state.Module.Policy != RandomizationSamplingPolicy.EveryLoop) continue;
                HasPerLoopRandomization = true;
                break;
            }
        }

        /// Applies every enabled module targeting `parameter` to `baseValue`, in authoring order (a
        /// later module composes on top of an earlier one's result). Unchanged for a TValue the
        /// component codec doesn't support (e.g. FixedString*).
        public TValue ApplyValue<TValue>(RandomizedParameter parameter, in TValue baseValue) where TValue : unmanaged
        {
            var codec = ValueComponentCodec<TValue>.Instance;
            if (codec == null) return baseValue;

            Span<float> components = stackalloc float[codec.ComponentCount];
            codec.Decompose(baseValue, components);

            foreach (var state in _modules)
            {
                if (state.Module.Parameter != parameter) continue;
                Apply(state, components);
            }

            return codec.Recompose(components);
        }

        /// Same as ApplyValue, for the always-scalar Duration/Delay parameters.
        public float ApplyScalar(RandomizedParameter parameter, float baseValue)
        {
            Span<float> components = stackalloc float[1];
            components[0] = baseValue;

            foreach (var state in _modules)
            {
                if (state.Module.Parameter != parameter) continue;
                Apply(state, components);
            }

            return components[0];
        }

        static void Apply(ModuleState state, Span<float> components)
        {
            var module = state.Module;
            var ranges = module.Components;
            var sample = GetOrDrawSample(state, components.Length);

            for (var i = 0; i < components.Length; i++)
            {
                if (i >= ranges.Count || !ranges[i].Enabled) continue; // left exactly as-is

                components[i] = module.Mode switch
                {
                    RandomizationMode.AbsoluteRange => sample[i],
                    RandomizationMode.Offset => components[i] + sample[i],
                    RandomizationMode.Multiplier => components[i] * sample[i],
                    _ => components[i]
                };
            }
        }

        /// OncePerPlay: draws once, caches, reuses forever on this RandomizationSet. EveryLoop: fresh
        /// sample every call. Only enabled components consume a draw, so toggling one on/off doesn't
        /// shift other components' draws.
        static float[] GetOrDrawSample(ModuleState state, int componentCount)
        {
            if (state.Module.Policy == RandomizationSamplingPolicy.OncePerPlay && state.CachedSample != null)
                return state.CachedSample;

            var module = state.Module;
            var ranges = module.Components;
            var sample = new float[componentCount];

            for (var i = 0; i < componentCount; i++)
            {
                if (i >= ranges.Count || !ranges[i].Enabled) continue;
                var range = ranges[i];
                sample[i] = range.Source == RandomizationValueSource.List && range.Values is { Count: > 0 }
                    ? range.Values[state.Rng.NextInt(range.Values.Count)]
                    : module.Distribution.Sample(range.Min, range.Max, ref state.Rng);
            }

            if (module.Policy == RandomizationSamplingPolicy.OncePerPlay) state.CachedSample = sample;
            return sample;
        }
    }
}
