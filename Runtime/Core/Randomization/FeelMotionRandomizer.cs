using System;
using System.Collections.Generic;
using UnityEngine;

namespace FeelMotion
{
    /// One opt-in randomization module on a FeelMotionAction — targets one authored parameter
    /// (Start/End Value, Duration, Delay) and perturbs it per RandomizationMode, independently of every
    /// other module on the same action (own RandomState, see CreateState). Applied to the resolved
    /// runtime settings right before a LitMotion handle is built, never to the authored
    /// SerializableMotionSettings itself.
    [Serializable]
    public sealed class FeelMotionRandomizer
    {
        [SerializeField] bool _enabled = true;
        [SerializeField] RandomizedParameter _parameter = RandomizedParameter.EndValue;
        [SerializeField] RandomizationMode _mode = RandomizationMode.Offset;
        [SerializeField] RandomizationSamplingPolicy _policy = RandomizationSamplingPolicy.OncePerPlay;
        [SerializeField] bool _useSeed;
        [SerializeField] int _seed;
        [SerializeReference] IRandomDistribution _distribution = new UniformDistribution();

        // One entry per value component (index 0 used directly for Duration/Delay, which are always
        // scalar). Defaults to a single enabled [-1, 1] range so a freshly-added module does something
        // visible immediately.
        [SerializeField] List<ComponentRange> _components = new() { new ComponentRange(true, -1f, 1f) };

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public RandomizedParameter Parameter
        {
            get => _parameter;
            set => _parameter = value;
        }

        public RandomizationMode Mode
        {
            get => _mode;
            set => _mode = value;
        }

        public RandomizationSamplingPolicy Policy
        {
            get => _policy;
            set => _policy = value;
        }

        public bool UseSeed
        {
            get => _useSeed;
            set => _useSeed = value;
        }

        public int Seed
        {
            get => _seed;
            set => _seed = value;
        }

        public IRandomDistribution Distribution => _distribution ??= new UniformDistribution();

        public List<ComponentRange> Components => _components ??= new List<ComponentRange>();

        /// Creates this module's own RNG stream for one Play() — deterministic when UseSeed is set,
        /// non-deterministic otherwise. Never shared with any other module.
        public RandomState CreateState() => _useSeed ? RandomState.FromSeed(_seed) : RandomState.NonDeterministic();
    }
}
