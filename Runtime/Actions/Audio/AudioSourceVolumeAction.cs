#if FEELMOTION_UNITY_AUDIO
using System;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

namespace FeelMotion
{
    [Serializable]
    [FeelMotionActionMenu("Audio/Audio Source/Volume")]
    public sealed class AudioSourceVolumeAction : FloatPropertyMotionAction<AudioSource, NoOptions, FloatMotionAdapter>
    {
        protected override float GetValue(AudioSource target) => target.volume;
        protected override void SetValue(AudioSource target, in float value) => target.volume = value;
    }
}
#endif
