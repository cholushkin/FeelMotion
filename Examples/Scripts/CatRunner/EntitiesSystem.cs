using System.Collections.Generic;
using FeelMotion;
using LitMotion;
using UnityEngine.InputSystem;
using UnityEngine;
#if FEELMOTION_RENDER_PIPELINES
using UnityEngine.Rendering;
#endif

namespace FeelMotion.Samples
{
    public sealed class EntitiesSystem : MonoBehaviour
    {
        static readonly int JumpTriggerHash = Animator.StringToHash("Jump");

        [SerializeField, Min(0f)] float _cooldownDuration = 1f;

        [SerializeField] Transform _cameraTransform;
        [SerializeField] Transform _catContainerTransform;
        [SerializeField] ParticleSystem _dustParticles;

        [SerializeField] AudioClip _quackClip;
        [SerializeField] AudioClip _wooshClip;
        [SerializeField] AudioClip _boomClip;
        [SerializeField] AudioClip _deniedClip;

#if FEELMOTION_RENDER_PIPELINES
        [SerializeField] Volume _postVolume;
#endif

        Animator _animator;
        AudioSource _audioSource;

        FeelMotionPlayer _jumpPlayer;
        FeelMotionPlayer _landPlayer;
        FeelMotionPlayer _deniedPlayer;

        float _lastJumpStartedAt = -100f;

        void Awake()
        {
            _animator = GetComponent<Animator>();
            _audioSource = GetComponent<AudioSource>();

            _jumpPlayer = BuildJumpPlayer();
            _landPlayer = BuildLandPlayer();
            _deniedPlayer = BuildDeniedPlayer();
        }

        void Update()
        {
            if (WasJumpPressedThisFrame()) Jump();
        }

        static bool WasJumpPressedThisFrame()
        {
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.spaceKey.wasPressedThisFrame) return true;

            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.wasPressedThisFrame) return true;

            return false;
        }

        void Jump()
        {
            if (Time.time - _lastJumpStartedAt < _cooldownDuration)
            {
                _deniedPlayer.Rewind();
                _deniedPlayer.Play();
                return;
            }

            _lastJumpStartedAt = Time.time;
            _animator.SetTrigger(JumpTriggerHash);

            _jumpPlayer.Rewind();
            _jumpPlayer.Play();
        }

        /// Called by an AnimationEvent on the jump clip at its contact frame.
        public void Land()
        {
            _landPlayer.Rewind();
            _landPlayer.Play();
        }

        FeelMotionPlayer BuildJumpPlayer()
        {
            var player = gameObject.AddComponent<FeelMotionPlayer>();
            player.PlaybackMode = MotionPlaybackMode.Parallel;

            var actions = new List<FeelMotionAction>
            {
                // Tiny, snappy camera kick.
                new TransformPositionShakeAction().Configure(
                    _cameraTransform, MotionValueMode.FromCurrentTo,
                    new SerializableMotionSettings<Vector3, ShakeOptions>
                    {
                        EndValue = new Vector3(0.03f, 0.03f, 0f),
                        Duration = 0.15f,
                        Options = new ShakeOptions { Frequency = 40, DampingRatio = 1f },
                    }),

                // Anticipation squash settling back to normal scale.
                new TransformScaleAction().Configure(
                    transform, MotionValueMode.Absolute,
                    new SerializableMotionSettings<Vector3, NoOptions>
                    {
                        StartValue = new Vector3(1.15f, 0.85f, 1f),
                        EndValue = Vector3.one,
                        Duration = 0.3f,
                        Ease = Ease.OutBack,
                    }),

                // Quack — plays immediately.
                new AudioPlayOneShotAction().Configure(_audioSource, _quackClip, 1f, true, 0.8f, 1.2f),
            };

            // Woosh — trails the quack by 0.1s, quiet.
            var woosh = new AudioPlayOneShotAction().Configure(_audioSource, _wooshClip, 0.05f, true, 0.9f, 1.2f);
            woosh.ActionSettings.Configure(0.1f);
            actions.Add(woosh);

#if FEELMOTION_RENDER_PIPELINES
            if (_postVolume != null)
            {
                actions.Add(new VolumeWeightAction().Configure(
                    _postVolume, MotionValueMode.Absolute,
                    new SerializableMotionSettings<float, NoOptions>
                    {
                        StartValue = 0f,
                        EndValue = 1f,
                        Duration = 0.1f,
                        Loops = 2,
                        LoopType = LoopType.Yoyo,
                    }));
            }
#endif

            player.SetActions(actions);
            return player;
        }

        FeelMotionPlayer BuildLandPlayer()
        {
            var player = gameObject.AddComponent<FeelMotionPlayer>();
            player.PlaybackMode = MotionPlaybackMode.Parallel;

            var actions = new List<FeelMotionAction>();

            if (_dustParticles != null) actions.Add(new ParticleEmitAction().Configure(_dustParticles, 100));

            actions.Add(new AudioPlayOneShotAction().Configure(_audioSource, _boomClip, 0.9f, true, 0.8f, 1f));

            actions.Add(new TransformPositionShakeAction().Configure(
                _cameraTransform, MotionValueMode.FromCurrentTo,
                new SerializableMotionSettings<Vector3, ShakeOptions>
                {
                    EndValue = new Vector3(0.02f, 0.02f, 0f),
                    Duration = 0.12f,
                    Options = new ShakeOptions { Frequency = 35, DampingRatio = 1f },
                }));

            // Horizontal landing wobble — stands in for Feel's MMSpringComponent bump.
            if (_catContainerTransform != null)
            {
                actions.Add(new TransformPositionPunchAction().Configure(
                    _catContainerTransform, MotionValueMode.FromCurrentTo,
                    new SerializableMotionSettings<Vector3, PunchOptions>
                    {
                        EndValue = new Vector3(0.08f, 0f, 0f),
                        Duration = 0.4f,
                        Options = new PunchOptions { Frequency = 8, DampingRatio = 0.5f },
                    }));
            }

#if FEELMOTION_RENDER_PIPELINES
            if (_postVolume != null)
            {
                actions.Add(new VolumeWeightAction().Configure(
                    _postVolume, MotionValueMode.Absolute,
                    new SerializableMotionSettings<float, NoOptions>
                    {
                        StartValue = 0f,
                        EndValue = 1f,
                        Duration = 0.2f,
                        Loops = 2,
                        LoopType = LoopType.Yoyo,
                    }));
            }
#endif

            player.SetActions(actions);
            return player;
        }

        FeelMotionPlayer BuildDeniedPlayer()
        {
            var player = gameObject.AddComponent<FeelMotionPlayer>();
            player.PlaybackMode = MotionPlaybackMode.Parallel;

            var actions = new List<FeelMotionAction>
            {
                new AudioPlayOneShotAction().Configure(_audioSource, _deniedClip, 0.3f, true, 1f, 1.3f),

                new TransformPositionShakeAction().Configure(
                    transform, MotionValueMode.FromCurrentTo,
                    new SerializableMotionSettings<Vector3, ShakeOptions>
                    {
                        EndValue = new Vector3(0.015f, 0f, 0f),
                        Duration = 0.15f,
                        Options = new ShakeOptions { Frequency = 25, DampingRatio = 1f },
                    }),
            };

            player.SetActions(actions);
            return player;
        }
    }
}
