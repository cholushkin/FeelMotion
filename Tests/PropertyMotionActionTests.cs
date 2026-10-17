using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace FeelMotion.Tests
{
    /// <summary>
    /// Covers MotionValueMode semantics, the restore-independent-of-stop contract, and ActionSettings
    /// (Documentation/Architecture.md §3, §6). Every assertion here is synchronous: Stop(Complete)
    /// forces the handle to its end value immediately, so none of these need to wait for real time to
    /// pass.
    /// </summary>
    public class PropertyMotionActionTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        static void AssertVector3(Vector3 actual, Vector3 expected, string message = null)
        {
            Assert.Less(Vector3.Distance(actual, expected), 0.0001f,
                message ?? $"expected {expected} but was {actual}");
        }

        [Test]
        public void RelativeToCurrent_OffsetsFromCapturedValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = new Vector3(5f, 0f, 0f);

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.RelativeToCurrent,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = new Vector3(1f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(6f, 0f, 0f));
        }

        [Test]
        public void FromCurrentTo_EndsExactlyAtAuthoredValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = new Vector3(5f, 0f, 0f);

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.FromCurrentTo,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = new Vector3(2f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(2f, 0f, 0f));
        }

        [Test]
        public void Absolute_UsesAuthoredStartAndEndRegardlessOfCurrentValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = new Vector3(5f, 0f, 0f);

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(9f, 0f, 0f));
        }

        [Test]
        public void Stop_CancelAndRestore_WritesBackCapturedInitialValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = new Vector3(3f, 1f, 0f);

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.CancelAndRestore);

            AssertVector3(_go.transform.localPosition, new Vector3(3f, 1f, 0f),
                "CancelAndRestore must write back the value captured at Play() — restore is independent of how playback stopped.");
        }

        [Test]
        public void Stop_Cancel_LeavesValueWhereItStopped_DoesNotRestore()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = new Vector3(3f, 1f, 0f);

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            // Plain Cancel must NOT restore — that's the whole point of separating the two (problem 4).
            runtime.Stop(MotionStopMode.Cancel);

            Assert.AreNotEqual(new Vector3(3f, 1f, 0f), _go.transform.localPosition,
                "Cancel alone must not restore the initial value — only *AndRestore modes do.");
        }

        [Test]
        public void ValidateTarget_ReturnsFalseWhenTargetIsNull()
        {
            var action = new TransformPositionAction().Configure(
                null,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);

            Assert.IsFalse(runtime.ValidateTarget());
        }

        [Test]
        public void ActionSettings_ResolveScheduler_MapsEachTimescaleMode()
        {
            Assert.AreSame(LitMotion.MotionScheduler.UpdateIgnoreTimeScale,
                new ActionSettings().Configure(0f, MotionTimescaleMode.Unscaled).ResolveScheduler());

            Assert.AreSame(LitMotion.MotionScheduler.UpdateRealtime,
                new ActionSettings().Configure(0f, MotionTimescaleMode.Realtime).ResolveScheduler());

            Assert.AreSame(LitMotion.MotionScheduler.Update,
                new ActionSettings().Configure(0f, MotionTimescaleMode.Scaled).ResolveScheduler());
        }

        [Test]
        public void ActionSettings_StartDelay_StillCompletesToAuthoredEndValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = Vector3.zero;

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });
            action.ActionSettings.Configure(startDelay: 0.5f);

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(9f, 0f, 0f),
                "a StartDelay must not prevent Stop(Complete) from snapping to the authored end value.");
        }

        [Test]
        public void ActionSettings_TimescaleModeUnscaled_StillCompletesToAuthoredEndValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = Vector3.zero;

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });
            action.ActionSettings.Configure(startDelay: 0f, MotionTimescaleMode.Unscaled);

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(9f, 0f, 0f),
                "an explicit Unscaled TimescaleMode must not break normal playback/completion.");
        }

        [Test]
        public void IntensityMultiplier_ScalesHowFarTheValueTravels()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = Vector3.zero;

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(new MotionPlaybackContext(durationMultiplier: 1f, intensityMultiplier: 0.5f));
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(4.5f, 0f, 0f),
                "an intensity multiplier of 0.5 must halve the reach from the authored start toward the authored end.");
        }

        [Test]
        public void DurationMultiplier_StillCompletesToTheAuthoredEndValue()
        {
            _go = new GameObject("test");
            _go.transform.localPosition = Vector3.zero;

            var action = new TransformPositionAction().Configure(
                _go.transform,
                MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { StartValue = Vector3.zero, EndValue = new Vector3(9f, 0f, 0f), Duration = 1f });

            var runtime = action.CreateRuntime(new MotionPlaybackContext(durationMultiplier: 2f, intensityMultiplier: 1f));
            runtime.Play();
            runtime.Stop(MotionStopMode.Complete);

            AssertVector3(_go.transform.localPosition, new Vector3(9f, 0f, 0f),
                "Stop(Complete) must still snap to the authored end value regardless of the duration multiplier.");
        }

        [Test]
        public void GetDuration_IsStartDelayPlusDelayPlusDuration_OfASingleLoop()
        {
            var action = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Delay = 0.2f, Loops = 1 });
            action.ActionSettings.Configure(startDelay: 0.1f);

            Assert.AreEqual(1.3d, action.GetDuration(), 0.0001d);
        }

        [Test]
        public void GetDuration_IsNotMultipliedByLoopCount()
        {
            var oneLoop = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Delay = 0.2f, Loops = 1 });
            var fiveLoops = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Delay = 0.2f, Loops = 5 });

            Assert.AreEqual(oneLoop.GetDuration(), fiveLoops.GetDuration(), 0.0001d,
                "the caption's duration must match a single loop pass, consistent with GetProgress resetting every loop — not a total across all loops.");
        }

        [Test]
        public void GetDuration_NegativeLoops_IsStillAFiniteSingleLoopDuration()
        {
            var action = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Delay = 0.2f, Loops = -1 });

            Assert.AreEqual(1.2d, action.GetDuration(), 0.0001d,
                "an infinitely-looping action still has a finite one-loop duration to show next to its progress bar.");
        }

        [Test]
        public void GetProgress_EveryLoopDelayType_RestartsTheDelayEveryLoop()
        {
            _go = new GameObject("test");
            var action = new TransformPositionAction().Configure(
                _go.transform, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions>
                    { EndValue = Vector3.one, Duration = 1f, Delay = 0.2f, Loops = -1, DelayType = DelayType.EveryLoop });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            var handle = runtime.Handle;

            handle.Time = 0.2d + 0.5d; // loop 0: halfway through the duration phase
            Assert.AreEqual(0.5f, action.GetProgress(handle), 0.01f);

            handle.Time = 1.2d + 0.1d; // loop 1 (period 1.2s): still inside its own delay phase
            Assert.AreEqual(0f, action.GetProgress(handle), 0.01f);

            runtime.Stop(MotionStopMode.Cancel);
        }

        [Test]
        public void GetProgress_FirstLoopDelayType_DoesNotReapplyTheDelayOnLaterLoops()
        {
            _go = new GameObject("test");
            var action = new TransformPositionAction().Configure(
                _go.transform, MotionValueMode.Absolute,
                new SerializableMotionSettings<Vector3, NoOptions>
                    { EndValue = Vector3.one, Duration = 1f, Delay = 0.2f, Loops = -1, DelayType = DelayType.FirstLoop });

            var runtime = action.CreateRuntime(MotionPlaybackContext.Default);
            runtime.Play();
            var handle = runtime.Handle;

            // Past the one-time initial delay (0.2s) and loop 0's duration (1s), halfway through loop 1 —
            // which, unlike EveryLoop, has no delay of its own.
            handle.Time = 0.2d + 1.0d + 0.5d;
            Assert.AreEqual(0.5f, action.GetProgress(handle), 0.01f,
                "FirstLoop must not re-apply the delay on loops after the first.");

            runtime.Stop(MotionStopMode.Cancel);
        }

        [Test]
        public void IsLooping_LoopsGreaterThanOne_IsTrue()
        {
            var action = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute, new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Loops = 2 });

            Assert.IsTrue(action.IsLooping);
        }

        [Test]
        public void IsLooping_NegativeLoops_IsTrue()
        {
            var action = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute, new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Loops = -1 });

            Assert.IsTrue(action.IsLooping);
        }

        [Test]
        public void IsLooping_LoopsEqualOne_IsFalse()
        {
            var action = new TransformPositionAction().Configure(
                null, MotionValueMode.Absolute, new SerializableMotionSettings<Vector3, NoOptions> { EndValue = Vector3.one, Duration = 1f, Loops = 1 });

            Assert.IsFalse(action.IsLooping);
        }

        [Test]
        public void DelayAction_GetDuration_IsStartDelayPlusDuration()
        {
            var action = new DelayAction();
            action.ActionSettings.Configure(startDelay: 0.5f);

            Assert.AreEqual(0.5d + action.Duration, action.GetDuration(), 0.0001d);
        }
    }
}
