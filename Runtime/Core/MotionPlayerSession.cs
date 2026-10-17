using System;
using System.Collections.Generic;
using LitMotion;
using UnityEngine;

namespace FeelMotion
{
    /// <summary>
    /// Everything one FeelMotionPlayer.Play() call needs: the per-action runtimes created for this
    /// play, the Sequential/Parallel wiring, and this session's own explicit MotionPlayerState.
    /// Created fresh by FeelMotionPlayer.Play() and discarded on completion/replace — see
    /// Documentation/Architecture.md §4.
    ///
    /// Sequential mode folds every action's handle into one LitMotion LSequence, giving the whole
    /// session a single master MotionHandle (no recursive queue advancement). Parallel mode keeps
    /// each action's handle independent so per-action Pause/Resume keeps working — see
    /// Documentation/Architecture.md §4's known limitation for why Sequential mode can't offer the
    /// same guarantee.
    /// </summary>
    internal sealed class MotionPlayerSession
    {
        readonly struct Entry
        {
            public readonly FeelMotionAction Action;
            public readonly IMotionActionRuntime Runtime;

            public Entry(FeelMotionAction action, IMotionActionRuntime runtime)
            {
                Action = action;
                Runtime = runtime;
            }
        }

        readonly List<Entry> _entries = new();
        readonly MotionPlaybackMode _mode;
        readonly float _sequenceDelay;
        readonly Action _onComplete;
        readonly Action<FeelMotionAction, Exception> _onActionError;

        MotionHandle _masterHandle;
        int _pendingParallelCount;
        bool _completedFired;

        public MotionPlayerState State { get; private set; } = MotionPlayerState.Idle;

        public MotionPlayerSession(
            IReadOnlyList<FeelMotionAction> actions,
            MotionPlaybackMode mode,
            float sequenceDelay,
            in MotionPlaybackContext context,
            Action onComplete,
            Action<FeelMotionAction, Exception> onActionError)
        {
            _mode = mode;
            _sequenceDelay = Mathf.Max(0f, sequenceDelay) * context.DurationMultiplier;
            _onComplete = onComplete;
            _onActionError = onActionError;

            if (actions == null) return;

            foreach (var action in actions)
            {
                if (action == null || !action.Enabled) continue;

                IMotionActionRuntime runtime;
                try
                {
                    runtime = action.CreateRuntime(context);
                }
                catch (Exception ex)
                {
                    _onActionError?.Invoke(action, ex);
                    continue;
                }

                if (runtime == null) continue;

                bool valid;
                try
                {
                    valid = runtime.ValidateTarget();
                }
                catch (Exception ex)
                {
                    _onActionError?.Invoke(action, ex);
                    continue;
                }

                if (!valid) continue;

                _entries.Add(new Entry(action, runtime));
            }
        }

        /// <summary>0-1 progress estimate. Sequential reads it straight off the master handle;
        /// Parallel averages each still-tracked handle's own progress.</summary>
        public float Progress
        {
            get
            {
                if (_mode == MotionPlaybackMode.Sequential)
                {
                    if (_masterHandle.IsActive() && !double.IsInfinity(_masterHandle.TotalDuration) && _masterHandle.TotalDuration > 0d)
                        return Mathf.Clamp01((float)(_masterHandle.Time / _masterHandle.TotalDuration));
                    return State == MotionPlayerState.Completed ? 1f : 0f;
                }

                if (_entries.Count == 0) return State == MotionPlayerState.Completed ? 1f : 0f;

                var sum = 0f;
                foreach (var entry in _entries)
                    sum += RuntimeProgress(entry.Action, entry.Runtime);
                return sum / _entries.Count;
            }
        }

        /// <summary>0-1 progress for one action currently tracked by this session — used by the
        /// Inspector to fill each action row's own progress bar while the whole player is playing (not
        /// just during a standalone per-action preview). Returns 0 for an action that isn't part of this
        /// session (disabled or failed validation).</summary>
        public float GetActionProgress(FeelMotionAction action)
        {
            foreach (var entry in _entries)
                if (ReferenceEquals(entry.Action, action))
                    return RuntimeProgress(entry.Action, entry.Runtime);
            return 0f;
        }

        static float RuntimeProgress(FeelMotionAction action, IMotionActionRuntime runtime)
        {
            var handle = runtime.Handle;
            return handle.IsActive()
                ? action.GetProgress(handle)
                : runtime.State == MotionActionState.Completed ? 1f : 0f;
        }

        public void Play()
        {
            State = MotionPlayerState.Playing;

            if (_entries.Count == 0)
            {
                CompleteNow();
                return;
            }

            if (_mode == MotionPlaybackMode.Sequential) PlaySequential();
            else PlayParallel();
        }

        void PlaySequential()
        {
            var builder = LSequence.Create();
            var processedCount = 0;
            var sequencedCount = 0;

            foreach (var entry in _entries)
            {
                try
                {
                    entry.Runtime.Play();
                }
                catch (Exception ex)
                {
                    _onActionError?.Invoke(entry.Action, ex);
                    processedCount++;
                    continue;
                }

                if (processedCount > 0 && _sequenceDelay > 0f) builder.AppendInterval(_sequenceDelay);
                processedCount++;

                var handle = entry.Runtime.Handle;
                if (handle.IsValid() && handle.IsActive())
                {
                    builder.Append(handle);
                    sequencedCount++;
                }
                // else: finished synchronously (e.g. an instant Control action) — the interval
                // above already accounts for its place in the timeline, nothing left to append.
            }

            if (sequencedCount == 0)
            {
                builder.Dispose();
                CompleteNow();
                return;
            }

            _masterHandle = builder.Run(b => b.WithOnComplete(OnMasterComplete));
        }

        void PlayParallel()
        {
            // Subscribe before anything plays so a synchronous completion inside the loop below
            // always counts against the full, already-known entry count.
            foreach (var entry in _entries)
                entry.Runtime.Completed += OnParallelEntryComplete;

            _pendingParallelCount = _entries.Count;

            foreach (var entry in _entries)
            {
                try
                {
                    entry.Runtime.Play();
                }
                catch (Exception ex)
                {
                    _onActionError?.Invoke(entry.Action, ex);
                    OnParallelEntryComplete(); // count it as done so the session can't hang on it
                }
            }
        }

        void OnParallelEntryComplete()
        {
            _pendingParallelCount--;
            if (_pendingParallelCount <= 0) CompleteNow();
        }

        void OnMasterComplete() => CompleteNow();

        void CompleteNow()
        {
            if (_completedFired) return;
            _completedFired = true;
            State = MotionPlayerState.Completed;
            _onComplete?.Invoke();
        }

        public void Pause()
        {
            if (State != MotionPlayerState.Playing) return;

            if (_mode == MotionPlaybackMode.Sequential)
            {
                if (_masterHandle.IsActive()) _masterHandle.PlaybackSpeed = 0f;
            }
            else
            {
                foreach (var entry in _entries)
                {
                    try { entry.Runtime.Pause(); }
                    catch (Exception ex) { _onActionError?.Invoke(entry.Action, ex); }
                }
            }

            State = MotionPlayerState.Paused;
        }

        public void Resume()
        {
            if (State != MotionPlayerState.Paused) return;

            if (_mode == MotionPlaybackMode.Sequential)
            {
                if (_masterHandle.IsActive()) _masterHandle.PlaybackSpeed = 1f;
            }
            else
            {
                foreach (var entry in _entries)
                {
                    try { entry.Runtime.Resume(); }
                    catch (Exception ex) { _onActionError?.Invoke(entry.Action, ex); }
                }
            }

            State = MotionPlayerState.Playing;
        }

        public void Stop(MotionStopMode mode)
        {
            if (State == MotionPlayerState.Idle) return;

            if (_mode == MotionPlaybackMode.Sequential)
            {
                if (mode is MotionStopMode.Cancel or MotionStopMode.CancelAndRestore)
                    _masterHandle.TryCancel();
                else
                    _masterHandle.TryComplete();
            }
            else
            {
                foreach (var entry in _entries)
                {
                    try { entry.Runtime.Stop(mode); }
                    catch (Exception ex) { _onActionError?.Invoke(entry.Action, ex); }
                }
            }

            if (mode is MotionStopMode.CancelAndRestore or MotionStopMode.CompleteAndRestore)
                RestoreInitialValues();

            if (mode is MotionStopMode.Complete or MotionStopMode.CompleteAndRestore)
            {
                CompleteNow(); // idempotent — a no-op if the natural completion callback already fired above
            }
            else
            {
                _completedFired = true; // a cancelled session must never later fire OnComplete
                State = MotionPlayerState.Idle;
            }
        }

        public void RestoreInitialValues()
        {
            foreach (var entry in _entries)
            {
                try { entry.Runtime.RestoreInitialValues(); }
                catch (Exception ex) { _onActionError?.Invoke(entry.Action, ex); }
            }
        }
    }
}
