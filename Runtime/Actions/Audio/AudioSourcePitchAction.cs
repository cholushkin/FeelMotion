#if FEELMOTION_UNITY_AUDIO
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Audio/Audio Source/Pitch")]
    public sealed class AudioSourcePitchAction : FloatPropertyMotionAction<AudioSource, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(AudioSource target) => target.pitch;
        protected override void SetValue(AudioSource target, in float value) => target.pitch = value;
    }
}
#endif
