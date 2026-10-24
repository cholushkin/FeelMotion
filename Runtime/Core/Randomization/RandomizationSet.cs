using System;
using System.Collections.Generic;

namespace FeelMotion
{
    /// <summary>
    /// Per-Play() aggregation of one action's enabled FeelMotionRandomizer modules — owns one
    /// RandomState per module (seeded or not, created once here) and, for OncePerPlay modules, the
    /// first-drawn sample, so every subsequent managed-loop iteration reuses those same numbers instead
    /// of re-rolling. Never shared across Play() calls: created fresh by whichever
    /// IMotionActionRuntime.Play() owns it, mirroring every other runtime-only state in this package
    /// (Documentation/Architecture.md §2). Internal — callers only need CreateOrNull/ApplyValue/
    /// ApplyScalar/HasPerLoopRandomization. Public (not internal) because it's shared across assembly
    /// boundaries: PropertyMotionAction lives in FeelMotion.asmdef, but ValueMotionAction — which also
    /// needs this — lives in the separate FeelMotion.Value.asmdef (Architecture.md §9), which only
    /// references FeelMotion, not the other way around.
    /// </summary>
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

        /// <summary>True when any enabled module uses EveryLoop — drives FeelMotionAction.
        /// RequiresManagedLoop, checked by the action itself before any RandomizationSet exists; kept
        /// here too for the runtime's own convenience once it has built one.</summary>
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

        /// <summary>Applies every enabled module targeting <paramref name="parameter"/> to
        /// <paramref name="baseValue"/>, in authoring order (a later module composes on top of an
        /// earlier one's result). Returns <paramref name="baseValue"/> unchanged for a TValue the
        /// component codec doesn't support (e.g. FixedString*).</summary>
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

        /// <summary>Same as ApplyValue, for the always-scalar Duration/Delay parameters.</summary>
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

        /// <summary>OncePerPlay: draws once, caches, and reuses forever after on this RandomizationSet.
        /// EveryLoop: draws a fresh sample on every call. Only the enabled components actually consume
        /// a draw from the module's RandomState — a disabled component's slot stays 0 and is never read
        /// (Apply skips it above), so toggling components on/off doesn't shift other components' draws.</summary>
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
                sample[i] = module.Distribution.Sample(range.Min, range.Max, ref state.Rng);
            }

            if (module.Policy == RandomizationSamplingPolicy.OncePerPlay) state.CachedSample = sample;
            return sample;
        }
    }
}
