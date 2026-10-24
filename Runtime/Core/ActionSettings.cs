using System;
using LitMotion;
using UnityEngine;

namespace FeelMotion;

/// Common, value-independent playback knobs every FeelMotionAction carries, regardless of whether it's
/// a property tween, a Control action, or a fully custom one. Separate from the per-value LitMotion
/// SerializableMotionSettings (duration/ease/loops) that PropertyMotionAction already exposes — see
/// Documentation/Architecture.md's ActionSettings section.
[Serializable]
public sealed class ActionSettings
{
    [SerializeField, Min(0f)] float _startDelay;
    [SerializeField] MotionTimescaleMode _timescaleMode = MotionTimescaleMode.Scaled;

    /// Extra delay, in seconds, before this action starts — added on top of whatever delay the action's
    /// own motion settings (if any) already authors.
    public float StartDelay => _startDelay;

    public MotionTimescaleMode TimescaleMode => _timescaleMode;

    /// Configures this instance from code instead of the Inspector — for runtime-built sequences and
    /// tests, matching PropertyMotionAction.Configure's pattern.
    public ActionSettings Configure(float startDelay, MotionTimescaleMode timescaleMode = MotionTimescaleMode.Scaled)
    {
        _startDelay = Mathf.Max(0f, startDelay);
        _timescaleMode = timescaleMode;
        return this;
    }

    /// Resolves TimescaleMode to a concrete LitMotion scheduler. Only call when TimescaleMode != Scaled
    /// — Scaled means "don't override whatever scheduler the action already has", not "use MotionScheduler.Update".
    public IMotionScheduler ResolveScheduler() => _timescaleMode switch
    {
        MotionTimescaleMode.Unscaled => MotionScheduler.UpdateIgnoreTimeScale,
        MotionTimescaleMode.Realtime => MotionScheduler.UpdateRealtime,
        _ => MotionScheduler.Update
    };
}
