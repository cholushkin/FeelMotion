using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace FeelMotion
{
    /// Main entry point: configure and play a sequence of FeelMotionActions from the Inspector. Every
    /// Play() call creates a fresh MotionPlayerSession — nothing here holds a MotionHandle or a
    /// captured value directly. See Documentation/Architecture.md §1, §4.
    ///
    /// Deliberately does not implement IMotionActionRunner: that interface's generic Resume()/
    /// Stop(MotionStopMode) shape is for individual action runtimes (PropertyMotionActionRuntime,
    /// DelayAction, EventAction) — the player's own surface is the Play/Pause/Rewind/Drop set below. A
    /// forced restart while already playing is just Rewind() + Play(), no separate method needed.
    [AddComponentMenu("FeelMotion/FeelMotion Player")]
    public sealed class FeelMotionPlayer : MonoBehaviour
    {
        [SerializeField] string _label;
        [SerializeField] MotionPlaybackMode _playbackMode = MotionPlaybackMode.Sequential;
        [SerializeField] MotionAutoPlayMode _autoPlay = MotionAutoPlayMode.None;
        [SerializeField, Min(0f)] float _sequenceDelay;
        [SerializeField] bool _stopOnDisable = true;
        [SerializeField] bool _restoreOnDisable;

        [SerializeField, Min(0f)] float _durationMultiplier = 1f;
        [SerializeField, Min(0f)] float _intensityMultiplier = 1f;

        [SerializeReference] FeelMotionAction[] _actions = Array.Empty<FeelMotionAction>();

        [SerializeField] UnityEvent _onPlay = new();
        [SerializeField] UnityEvent _onComplete = new();
        [SerializeField] UnityEvent _onStop = new();

        MotionPlayerSession _session;

        public IReadOnlyList<FeelMotionAction> Actions => _actions;

        /// User-settable name for this whole animation effect — e.g. "Hit Flash", shown in the
        /// Inspector. Mirrors the per-action label (FeelMotionAction.DisplayName); purely for authoring
        /// clarity, never read by playback logic.
        public string Label
        {
            get => _label;
            set => _label = value;
        }

        /// Replaces the action list from code — e.g. a procedurally built sequence, or a test.
        public void SetActions(IReadOnlyList<FeelMotionAction> actions)
        {
            if (actions == null)
            {
                _actions = Array.Empty<FeelMotionAction>();
                return;
            }

            var copy = new FeelMotionAction[actions.Count];
            for (var i = 0; i < actions.Count; i++) copy[i] = actions[i];
            _actions = copy;
        }

        public MotionPlaybackMode PlaybackMode
        {
            get => _playbackMode;
            set => _playbackMode = value;
        }

        public float SequenceDelay
        {
            get => _sequenceDelay;
            set => _sequenceDelay = Mathf.Max(0f, value);
        }

        /// Global multiplier applied to every action's duration/delay this player plays — mirrors
        /// Feel's MMF_Player.DurationMultiplier. 1 (default) changes nothing.
        public float DurationMultiplier
        {
            get => _durationMultiplier;
            set => _durationMultiplier = Mathf.Max(0f, value);
        }

        /// Global multiplier applied to how far every action's value travels — mirrors Feel's
        /// MMF_Player.FeedbacksIntensity. 1 (default) changes nothing, 0 means no motion.
        public float IntensityMultiplier
        {
            get => _intensityMultiplier;
            set => _intensityMultiplier = Mathf.Max(0f, value);
        }

        public MotionPlaybackContext PlaybackContext => new(_durationMultiplier, _intensityMultiplier);

        public MotionPlayerState State => _session?.State ?? MotionPlayerState.Idle;

        public float Progress => _session?.Progress ?? 0f;

        /// 0-1 progress of one action in the currently playing/paused session — lets the Inspector fill
        /// each action row's own progress bar while the whole player is playing, not just during a
        /// standalone per-action preview. 0 if idle or the action isn't part of the current session.
        public float GetActionProgress(FeelMotionAction action) => _session?.GetActionProgress(action) ?? 0f;

        public event Action Played;
        public event Action Paused;
        public event Action Resumed;
        public event Action Stopped;
        public event Action Completed;

        /// Fired when a single action throws during Play()/validation. The session skips the offending
        /// action and keeps going — see Documentation/Architecture.md §7.
        public event Action<FeelMotionAction, Exception> ActionError;

        void OnEnable()
        {
            if (_autoPlay == MotionAutoPlayMode.OnEnable) Play();
        }

        void Start()
        {
            if (_autoPlay == MotionAutoPlayMode.OnStart) Play();
        }

        void OnDisable()
        {
            if (_session == null) return;
            if (_restoreOnDisable) _session.RestoreInitialValues();
            if (_stopOnDisable) Drop();
        }

        void OnDestroy()
        {
            Drop();
        }

        /// Starts from the beginning if idle/completed, resumes if paused, or does nothing if already
        /// playing — merges the old separate Play()/Resume() into one entry point.
        public void Play()
        {
            if (_session != null)
            {
                switch (_session.State)
                {
                    case MotionPlayerState.Playing:
                        return;
                    case MotionPlayerState.Paused:
                        _session.Resume();
                        Resumed?.Invoke();
                        return;
                }
            }

            _session = new MotionPlayerSession(_actions, _playbackMode, _sequenceDelay, PlaybackContext, HandleComplete, HandleActionError);
            _session.Play();

            Played?.Invoke();
            _onPlay?.Invoke();
        }

        /// Freezes time in place — the running session can be resumed later with Play().
        public void Pause()
        {
            _session?.Pause();
            Paused?.Invoke();
        }

        /// Stops playing and writes each action's captured initial value back — the opposite of Drop().
        /// Named Rewind rather than Reset to avoid colliding with Unity's own MonoBehaviour.Reset() Editor message.
        public void Rewind()
        {
            if (_session == null) return;
            _session.Stop(MotionStopMode.CancelAndRestore);
            Stopped?.Invoke();
            _onStop?.Invoke();
        }

        /// Stops animating and releases the property holds — leaves every value exactly where it was,
        /// unlike Rewind().
        public void Drop()
        {
            if (_session == null) return;
            _session.Stop(MotionStopMode.Cancel);
            Stopped?.Invoke();
            _onStop?.Invoke();
        }

        /// Awaitable sugar over Play() for callers that want async/await ergonomics.
        public UniTask PlayAsync()
        {
            var tcs = new UniTaskCompletionSource();

            void OnDone()
            {
                Completed -= OnDone;
                tcs.TrySetResult();
            }

            Completed += OnDone;
            Play();
            return tcs.Task;
        }

        void HandleComplete()
        {
            Completed?.Invoke();
            _onComplete?.Invoke();
        }

        void HandleActionError(FeelMotionAction action, Exception ex)
        {
            ActionError?.Invoke(action, ex);
            Debug.LogException(ex, this);
        }
    }
}
