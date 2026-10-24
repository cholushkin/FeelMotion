using System;
using System.Linq;
using System.Text.RegularExpressions;
using LitMotion;
using LitMotion.Adapters;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace FeelMotion.Tests
{
    /// <summary>
    /// Covers the opt-in randomization system (Documentation/Architecture.md's Randomization section):
    /// the value-component codec, the three RandomizationModes, seeded determinism, OncePerPlay vs.
    /// EveryLoop sampling, RequiresManagedLoop, and the Sequential-mode rejection of a managed-loop
    /// action. Every assertion drives completion synchronously via MotionHandle.TryComplete(), matching
    /// PropertyMotionActionTests.cs's established convention — none of these need real time to pass.
    /// Several tests use a Min==Max range so a mode's math is checked against an exact value instead of
    /// a probabilistic one; only the OncePerPlay/EveryLoop and seed tests need genuine randomness, and
    /// those assert by comparison (equal/not-equal) rather than a hardcoded sampled value.
    /// </summary>
    public class RandomizationTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) UnityEngine.Object.DestroyImmediate(_go);
        }

        static void AssertVector3(Vector3 actual, Vector3 expected, string message = null)
        {
            Assert.Less(Vector3.Distance(actual, expected), 0.0001f, message ?? $"expected {expected} but was {actual}");
        }

        // Configure() returns the PropertyMotionAction<,,,> base (for fluent chaining from any concrete
        // action), not TransformPositionAction itself — matching PropertyMotionActionTests.cs's own
        // `var action = new TransformPositionAction().Configure(...)` pattern. Every member this file
        // needs (AddRandomizer, RequiresManagedLoop, CreateRuntime, ...) lives on that base anyway.
        PropertyMotionAction<Transform, Vector3, NoOptions, Vector3MotionAdapter> CreateAction(Vector3 endValue, float duration = 0.01f, int loops = 1)
        {
            _go = new GameObject("test");
            return new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = endValue, Duration = duration, Loops = loops });
        }

        [Test]
        public void ValueComponentCodec_Vector3_RoundTripsExactly()
        {
            var codec = ValueComponentCodec<Vector3>.Instance;
            Span<float> components = stackalloc float[codec.ComponentCount];
            var original = new Vector3(1.5f, -2.25f, 3.75f);

            codec.Decompose(original, components);
            var result = codec.Recompose(components);

            AssertVector3(result, original);
        }

        [Test]
        public void ValueComponentCodec_Int_RoundsToNearest()
        {
            var codec = ValueComponentCodec<int>.Instance;
            Span<float> components = stackalloc float[1];

            components[0] = 7.4f;
            Assert.AreEqual(7, codec.Recompose(components));

            components[0] = 7.6f;
            Assert.AreEqual(8, codec.Recompose(components));
        }

        [Test]
        public void ValueComponentCodec_UnsupportedType_HasNoInstance()
        {
            // FixedString* actions (TMP/UI Text, StringValue) have no component codec — Start/End Value
            // randomization is unavailable for them, Duration/Delay randomization still works. Quaternion
            // stands in here as a real, unmanaged-but-unsupported TValue (no built-in FeelMotion action
            // currently tweens one directly, but the codec genuinely doesn't cover it).
            Assert.IsNull(ValueComponentCodec<Quaternion>.Instance);
        }

        [Test]
        public void Offset_AddsToBase_AndLeavesADisabledComponentUntouched()
        {
            var action = CreateAction(new Vector3(5f, 7f, 9f));
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Mode = RandomizationMode.Offset;
            randomizer.Components[0] = new ComponentRange(true, 10f, 10f); // X: deterministic +10
            randomizer.Components.Add(new ComponentRange(false, 999f, 999f)); // Y: disabled — must stay authored
            randomizer.Components.Add(new ComponentRange(true, -2f, -2f)); // Z: deterministic -2

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(15f, 7f, 7f),
                "Offset must add the sampled delta to the authored EndValue per enabled component, and leave a disabled component exactly as authored.");
        }

        [Test]
        public void Multiplier_ScalesBase()
        {
            var action = CreateAction(new Vector3(5f, 7f, 9f));
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Mode = RandomizationMode.Multiplier;
            randomizer.Components[0] = new ComponentRange(true, 2f, 2f);
            randomizer.Components.Add(new ComponentRange(true, 3f, 3f));
            randomizer.Components.Add(new ComponentRange(true, 0.5f, 0.5f));

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(10f, 21f, 4.5f),
                "Multiplier must multiply the authored EndValue by the sampled factor per enabled component.");
        }

        [Test]
        public void AbsoluteRange_ReplacesTheAuthoredValueOutright()
        {
            var action = CreateAction(new Vector3(5f, 7f, 9f));
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Mode = RandomizationMode.AbsoluteRange;
            randomizer.Components[0] = new ComponentRange(true, 42f, 42f);
            randomizer.Components.Add(new ComponentRange(true, 100f, 100f));
            randomizer.Components.Add(new ComponentRange(true, -8f, -8f));

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(42f, 100f, -8f),
                "AbsoluteRange must replace the authored EndValue outright, ignoring it entirely.");
        }

        [Test]
        public void RandomizationSet_CreateOrNull_IsNullWhenThereAreNoEnabledModules()
        {
            var action = CreateAction(Vector3.one);
            Assert.IsNull(RandomizationSet.CreateOrNull(action.Randomizers),
                "an action with no randomizers must take the exact pre-randomization code path — this is the zero-overhead-when-disabled contract.");

            var randomizer = action.AddRandomizer();
            randomizer.Enabled = false;
            Assert.IsNull(RandomizationSet.CreateOrNull(action.Randomizers),
                "a disabled module must not count as 'has randomization' either.");
        }

        [Test]
        public void Seed_TheSameSeed_ProducesTheSameSampledValue()
        {
            var actionA = CreateAction(Vector3.zero);
            var randomizerA = actionA.AddRandomizer();
            randomizerA.Parameter = RandomizedParameter.EndValue;
            randomizerA.Mode = RandomizationMode.AbsoluteRange;
            randomizerA.UseSeed = true;
            randomizerA.Seed = 12345;
            randomizerA.Components[0] = new ComponentRange(true, -1000f, 1000f);

            var runtimeA = actionA.CreateRuntime(MotionPlaybackContext.Default);
            runtimeA.Play();
            runtimeA.Stop(MotionStopMode.Complete);
            var resultA = _go.transform.localPosition.x;
            UnityEngine.Object.DestroyImmediate(_go); // CreateAction reassigns _go — clean up the first one explicitly

            var actionB = CreateAction(Vector3.zero);
            var randomizerB = actionB.AddRandomizer();
            randomizerB.Parameter = RandomizedParameter.EndValue;
            randomizerB.Mode = RandomizationMode.AbsoluteRange;
            randomizerB.UseSeed = true;
            randomizerB.Seed = 12345;
            randomizerB.Components[0] = new ComponentRange(true, -1000f, 1000f);

            var runtimeB = actionB.CreateRuntime(MotionPlaybackContext.Default);
            runtimeB.Play();
            runtimeB.Stop(MotionStopMode.Complete);
            var resultB = _go.transform.localPosition.x;

            Assert.AreEqual(resultA, resultB, 0.0001f,
                "two independent modules with the same seed must draw the same sample.");
        }

        [Test]
        public void RequiresManagedLoop_IsFalse_WithOnlyOncePerPlayRandomizers()
        {
            var action = CreateAction(Vector3.one, loops: 5);
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Policy = RandomizationSamplingPolicy.OncePerPlay;

            Assert.IsFalse(action.RequiresManagedLoop);
        }

        [Test]
        public void RequiresManagedLoop_IsFalse_WhenLoopCountIsOne_EvenWithAnEveryLoopRandomizer()
        {
            var action = CreateAction(Vector3.one, loops: 1);
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Policy = RandomizationSamplingPolicy.EveryLoop;

            Assert.IsFalse(action.RequiresManagedLoop,
                "a non-looping action has nothing for EveryLoop to repeat — it must take the plain single-handle path.");
        }

        [Test]
        public void RequiresManagedLoop_IsTrue_WithAnEnabledEveryLoopRandomizerAndMoreThanOneLoop()
        {
            var action = CreateAction(Vector3.one, loops: 5);
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Policy = RandomizationSamplingPolicy.EveryLoop;

            Assert.IsTrue(action.RequiresManagedLoop);
        }

        [Test]
        public void EveryLoop_ResamplesEachIteration_WhileOncePerPlay_StaysConstant()
        {
            var action = CreateAction(Vector3.zero, duration: 0.01f, loops: 5);

            var everyLoopOnX = action.AddRandomizer();
            everyLoopOnX.Parameter = RandomizedParameter.EndValue;
            everyLoopOnX.Mode = RandomizationMode.AbsoluteRange;
            everyLoopOnX.Policy = RandomizationSamplingPolicy.EveryLoop;
            everyLoopOnX.Components[0] = new ComponentRange(true, 0f, 1_000_000f);

            var oncePerPlayOnY = action.AddRandomizer();
            oncePerPlayOnY.Parameter = RandomizedParameter.EndValue;
            oncePerPlayOnY.Mode = RandomizationMode.AbsoluteRange;
            oncePerPlayOnY.Policy = RandomizationSamplingPolicy.OncePerPlay;
            oncePerPlayOnY.Components[0] = new ComponentRange(false, 0f, 0f);
            oncePerPlayOnY.Components.Add(new ComponentRange(true, 0f, 1_000_000f));

            Assert.IsTrue(action.RequiresManagedLoop);

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();

            var xSamples = new System.Collections.Generic.List<float>();
            var ySamples = new System.Collections.Generic.List<float>();

            // Each TryComplete() both snaps the current iteration to its own resolved EndValue and
            // (via ManagedLoopPlayer.OnIterationComplete, wired as that handle's WithOnComplete) spawns
            // the next iteration synchronously — same forcing technique PropertyMotionActionTests.cs
            // uses for Stop(Complete), just driven loop-by-loop instead of once.
            for (var i = 0; i < 5; i++)
            {
                runtime.Handle.TryComplete();
                xSamples.Add(_go.transform.localPosition.x);
                ySamples.Add(_go.transform.localPosition.y);
            }

            Assert.AreEqual(MotionActionState.Completed, runtime.State);
            Assert.Greater(xSamples.Distinct().Count(), 1,
                "an EveryLoop module must draw a fresh sample for every managed-loop iteration.");
            Assert.AreEqual(1, ySamples.Distinct().Count(),
                "an OncePerPlay module must reuse its first sample on every later managed-loop iteration.");
        }

        [Test]
        public void Sequential_RejectsAnEveryLoopRandomizedAction_ViaActionError()
        {
            _go = new GameObject("test");
            var player = _go.AddComponent<FeelMotionPlayer>();
            player.PlaybackMode = MotionPlaybackMode.Sequential;

            var action = new TransformPositionAction().Configure(
                _go.transform, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 0.1f, Loops = 3 });
            var randomizer = action.AddRandomizer();
            randomizer.Parameter = RandomizedParameter.EndValue;
            randomizer.Policy = RandomizationSamplingPolicy.EveryLoop;

            player.SetActions(new FeelMotionAction[] { action });

            Exception caughtError = null;
            player.ActionError += (_, ex) => caughtError = ex;

            var completed = false;
            player.Completed += () => completed = true;

            // Same expected error-isolation log as FeelMotionPlayerTests.cs's infinite-loop case —
            // HandleActionError logs the caught exception as part of its normal, by-design path.
            LogAssert.Expect(LogType.Exception, new Regex("InvalidOperationException"));

            player.Play();

            Assert.IsTrue(completed,
                "a session whose only action is rejected must still reach Completed — same zero-sequenced-entries path as the existing infinite-loop rejection.");
            Assert.IsNotNull(caughtError);
            StringAssert.Contains("Sequential playback mode", caughtError.Message);

            player.Drop();
        }
    }
}
