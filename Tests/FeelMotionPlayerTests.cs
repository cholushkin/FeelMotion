using System;
using LitMotion;
using NUnit.Framework;
using UnityEngine;

namespace FeelMotion.Tests
{
    /// <summary>
    /// Covers MotionPlayerSession orchestration through the public FeelMotionPlayer API: zero-action
    /// completion, per-action error isolation (Documentation/Architecture.md §7), and the merged
    /// Play()/Resume() control-API semantics. Deliberately tests through FeelMotionPlayer rather than
    /// the internal MotionPlayerSession/runtime types, matching this project's convention of testing
    /// the public surface.
    /// </summary>
    public class FeelMotionPlayerTests
    {
        GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                if (_go.TryGetComponent<FeelMotionPlayer>(out var player))
                    player.Drop();
                UnityEngine.Object.DestroyImmediate(_go);
            }
        }

        FeelMotionPlayer CreatePlayer()
        {
            _go = new GameObject("FeelMotionPlayerTest");
            return _go.AddComponent<FeelMotionPlayer>();
        }

        [Test]
        public void Play_WithNoActions_CompletesImmediatelyAndFiresOnComplete()
        {
            var player = CreatePlayer();
            var completed = false;
            player.Completed += () => completed = true;

            player.SetActions(Array.Empty<FeelMotionAction>());
            player.Play();

            Assert.IsTrue(completed);
            Assert.AreEqual(MotionPlayerState.Completed, player.State);
        }

        [Test]
        public void Play_WhenTheOnlyActionThrows_SessionStillCompletes()
        {
            var player = CreatePlayer();
            player.PlaybackMode = MotionPlaybackMode.Sequential;

            Exception caughtError = null;
            FeelMotionAction erroredAction = null;
            player.ActionError += (action, ex) =>
            {
                erroredAction = action;
                caughtError = ex;
            };

            var completed = false;
            player.Completed += () => completed = true;

            var throwing = new ThrowingAction();
            player.SetActions(new FeelMotionAction[] { throwing });

            player.Play();

            Assert.IsTrue(completed,
                "A session whose only action throws during Play() must still reach Completed — " +
                "per-action error isolation (Documentation/Architecture.md §7) must never let one " +
                "broken action stall the whole sequence.");
            Assert.IsNotNull(caughtError);
            Assert.AreSame(throwing, erroredAction);
            Assert.AreEqual(MotionPlayerState.Completed, player.State);
        }

        [Test]
        public void Play_WhenOneOfSeveralSequentialActionsThrows_RemainingActionsStillRun()
        {
            var player = CreatePlayer();
            player.PlaybackMode = MotionPlaybackMode.Sequential;

            var throwing = new ThrowingAction();
            var flag = new FlagAction();

            player.SetActions(new FeelMotionAction[] { throwing, flag });
            player.Play();

            Assert.IsTrue(flag.WasPlayed,
                "the action after a throwing one must still have been scheduled and run — a single " +
                "broken action must never abort the rest of a Sequential session (FR-014).");

            player.Drop();
        }

        [Test]
        public void Play_WhileAlreadyPlaying_DoesNothing()
        {
            var player = CreatePlayer();

            var action = new CountingAction();
            player.SetActions(new FeelMotionAction[] { action });

            player.Play();
            player.Play(); // must be a no-op — the first session is still Playing

            Assert.AreEqual(1, action.PlayCount);

            player.Drop();
        }

        [Test]
        public void RewindThenPlay_CancelsThePreviousSessionAndStartsAFreshOne()
        {
            var player = CreatePlayer();

            var action = new CountingAction();
            player.SetActions(new FeelMotionAction[] { action });

            player.Play();
            player.Rewind(); // Rewind() + Play() is the forced-restart idiom — no separate Restart() method
            player.Play();

            Assert.AreEqual(2, action.PlayCount);

            player.Drop();
        }

        [Test]
        public void Play_WhilePaused_ResumesRatherThanRestarting()
        {
            var player = CreatePlayer();

            var action = new CountingAction();
            player.SetActions(new FeelMotionAction[] { action });

            player.Play();
            player.Pause();
            player.Play(); // must resume the existing session, not start a new one

            Assert.AreEqual(1, action.PlayCount);
            Assert.AreEqual(MotionPlayerState.Playing, player.State);

            player.Drop();
        }

        /// <summary>Fails every time Play() is called — exercises the per-action error isolation path.</summary>
        sealed class ThrowingAction : FeelMotionAction
        {
            public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime();

            sealed class Runtime : IMotionActionRuntime
            {
                public MotionActionState State => MotionActionState.Idle;
                public MotionHandle Handle => default;
                public event Action Completed { add { } remove { } }
                public bool ValidateTarget() => true;
                public void Play() => throw new InvalidOperationException("intentional test failure");
                public void Pause() { }
                public void Resume() { }
                public void Stop(MotionStopMode mode) { }
                public void RestoreInitialValues() { }
            }
        }

        /// <summary>Sets a flag as soon as its runtime's Play() runs, then completes instantly.</summary>
        sealed class FlagAction : FeelMotionAction
        {
            public bool WasPlayed;

            public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this);

            sealed class Runtime : IMotionActionRuntime
            {
                readonly FlagAction _action;
                public Runtime(FlagAction action) => _action = action;

                public MotionActionState State { get; private set; } = MotionActionState.Idle;
                public MotionHandle Handle { get; private set; }
                public event Action Completed;

                public bool ValidateTarget() => true;

                public void Play()
                {
                    _action.WasPlayed = true;
                    State = MotionActionState.Playing;
                    Handle = LMotion.Create(0f, 1f, 0f)
                        .WithOnComplete(OnLitMotionComplete)
                        .RunWithoutBinding();
                }

                void OnLitMotionComplete()
                {
                    State = MotionActionState.Completed;
                    Completed?.Invoke();
                }

                public void Pause() { }
                public void Resume() { }
                public void Stop(MotionStopMode mode) { }
                public void RestoreInitialValues() { }
            }
        }

        /// <summary>Counts how many times its runtime's Play() actually ran, and plays for long enough
        /// that a test can safely call FeelMotionPlayer.Play() a second time before it finishes.</summary>
        sealed class CountingAction : FeelMotionAction
        {
            public int PlayCount;

            public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this);

            sealed class Runtime : IMotionActionRuntime
            {
                readonly CountingAction _action;
                public Runtime(CountingAction action) => _action = action;

                public MotionActionState State { get; private set; } = MotionActionState.Idle;
                public MotionHandle Handle => _handle;
                public event Action Completed;

                MotionHandle _handle;

                public bool ValidateTarget() => true;

                public void Play()
                {
                    _action.PlayCount++;
                    State = MotionActionState.Playing;
                    _handle = LMotion.Create(0f, 1f, 60f)
                        .WithOnComplete(OnLitMotionComplete)
                        .RunWithoutBinding();
                }

                void OnLitMotionComplete()
                {
                    State = MotionActionState.Completed;
                    Completed?.Invoke();
                }

                public void Pause()
                {
                    if (_handle.IsActive()) _handle.PlaybackSpeed = 0f;
                }

                public void Resume()
                {
                    if (_handle.IsActive()) _handle.PlaybackSpeed = 1f;
                }

                public void Stop(MotionStopMode mode)
                {
                    _handle.TryCancel();
                }

                public void RestoreInitialValues() { }
            }
        }
    }
}
