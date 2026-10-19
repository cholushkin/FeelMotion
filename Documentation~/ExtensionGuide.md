# FeelMotion — Extension Guide (custom actions)

How to add a new action type — in `Game/Runtime`, in another `Assets/Libs` package, or in your own
project-local assembly — **without modifying FeelMotion's core**. This is written against the real,
shipped API (see [Architecture.md](Architecture.md)), not a speculative sketch.

## 1. The two things you can implement

### 1a. A property tween — the common case

Most custom actions animate one property on one object type: subclass
`PropertyMotionAction<TObject, TValue, TOptions, TAdapter>` and implement five methods. You get
`MotionValueMode`, restore-independent-of-stop, target validation, `ActionSettings` (start delay,
timescale), the global duration/intensity multipliers, and sequencing for free:

```csharp
using System;
using FeelMotion;
using LitMotion;
using LitMotion.Adapters;
using UnityEngine;

[Serializable]
[FeelMotionActionMenu("MyGame/Shake Camera FOV")]
public sealed class CameraFovShakeAction : PropertyMotionAction<Camera, float, ShakeOptions, FloatShakeMotionAdapter>
{
    // In your own assembly, override with just `protected` — `GetValue`/`SetValue`/`Add`/`Multiply`/
    // `ScaleReach` are declared `protected internal` on PropertyMotionAction, but C# requires a
    // cross-assembly override to drop the `internal` part (it only applies within PropertyMotionAction's
    // own assembly). FeelMotion's own built-in actions keep `protected internal override` only because
    // they happen to live in the same assembly as PropertyMotionAction itself.
    protected override float GetValue(Camera target) => target.fieldOfView;
    protected override void SetValue(Camera target, in float value) => target.fieldOfView = value;

    // Backs MotionValueMode.RelativeToCurrent when CombineMode is Add (the default) — current + delta.
    protected override float Add(in float baseValue, in float delta) => baseValue + delta;

    // Backs RelativeToCurrent when CombineMode is Multiply — current * delta. Mostly relevant for
    // scale-like properties, where Add would distort rather than scale; for a plain FOV shake this is
    // just the arithmetically-consistent counterpart, same mechanical shape as Add.
    protected override float Multiply(in float baseValue, in float factor) => baseValue * factor;

    // Backs the player's global IntensityMultiplier (Architecture.md §8) — same mechanical shape as
    // Add: `from + (to - from) * factor`. factor == 1 means "unchanged", 0 means "no motion".
    protected override float ScaleReach(in float from, in float to, float factor) => from + (to - from) * factor;
}
```

That's the entire action. `[FeelMotionActionMenu("Category/Name")]` is what makes it show up in the
Inspector's Add Action dropdown — see §2.

### 1b. A fully custom bind — your own `LMotion` extension, wrapped

If you already have (or want) a standalone LitMotion bind extension method — usable with or without
FeelMotion, e.g. in a `MotionBuilder` extension library you share across projects:

```csharp
public static class LitMotionCanvasGroupExtensions
{
    public static MotionHandle BindToCanvasGroupAlpha<TOptions, TAdapter>(
        this MotionBuilder<float, TOptions, TAdapter> builder, CanvasGroup target)
        where TOptions : unmanaged, IMotionOptions
        where TAdapter : unmanaged, IMotionAdapter<float, TOptions>
    {
        return builder.Bind(target, (x, t) => { if (t != null) t.alpha = x; });
    }
}
```

— you don't have to choose between "a plain LitMotion extension" and "a FeelMotion action": implement
`FeelMotionAction` directly and have your runtime's `Play()` call straight into your own extension
method. This is also the shape for anything that isn't a simple property tween (a callback, an audio
one-shot, a wait-for-signal) — see `Runtime/Actions/Control/EventAction.cs` and `DelayAction.cs` in
this package for the two simplest real examples:

```csharp
[Serializable]
[FeelMotionActionMenu("MyGame/Fade Canvas Group (custom bind)")]
public sealed class FadeCanvasGroupAction : FeelMotionAction
{
    [SerializeField] CanvasGroup _target;
    [SerializeField] float _to = 1f;
    [SerializeField] float _duration = 0.3f;

    public override IMotionActionRuntime CreateRuntime(in MotionPlaybackContext context) => new Runtime(this, context);

    sealed class Runtime : IMotionActionRuntime
    {
        readonly FadeCanvasGroupAction _action;
        readonly MotionPlaybackContext _context;
        public Runtime(FadeCanvasGroupAction action, in MotionPlaybackContext context)
        {
            _action = action;
            _context = context;
        }

        public MotionActionState State { get; private set; }
        public MotionHandle Handle => _handle;
        public event Action Completed;
        MotionHandle _handle;

        public bool ValidateTarget() => _action._target != null;

        public void Play()
        {
            State = MotionActionState.Playing;

            // Your own extension method, used exactly as you would outside FeelMotion. Apply the
            // player's global DurationMultiplier yourself — a hand-written runtime owns its own
            // LMotion builder, so it's responsible for every time quantity it hands to LitMotion.
            var durationMultiplier = _context.DurationMultiplier;
            var builder = LMotion.Create(_action._target.alpha, _action._to, _action._duration * durationMultiplier)
                .WithOnComplete(() => { State = MotionActionState.Completed; Completed?.Invoke(); });

            // Apply the common ActionSettings block yourself — PropertyMotionAction does this for
            // you, but a hand-written runtime owns its own LMotion builder, so it applies it too.
            var settings = _action.ActionSettings;
            if (settings.StartDelay > 0f) builder = builder.WithDelay(settings.StartDelay * durationMultiplier);
            if (settings.TimescaleMode != MotionTimescaleMode.Scaled) builder = builder.WithScheduler(settings.ResolveScheduler());

            _handle = builder.BindToCanvasGroupAlpha(_action._target);
        }

        public void Pause() { if (_handle.IsActive()) _handle.PlaybackSpeed = 0f; State = MotionActionState.Paused; }
        public void Resume() { if (_handle.IsActive()) _handle.PlaybackSpeed = 1f; State = MotionActionState.Playing; }
        public void Stop(MotionStopMode mode)
        {
            if (mode is MotionStopMode.Cancel or MotionStopMode.CancelAndRestore) _handle.TryCancel();
            else _handle.TryComplete();
        }
        public void RestoreInitialValues() { } // own this if you captured a value to restore
    }
}
```

The pattern that matters, taken from `EventAction`/`DelayAction`: **every action produces a
`MotionHandle`, even if it's a zero-duration placeholder**, so the session's sequencing code never
needs a special case for "actions that aren't real tweens."

## 2. Making it discoverable in the Inspector

No registration file, no central enum:

```csharp
[AttributeUsage(AttributeTargets.Class)]
public sealed class FeelMotionActionMenuAttribute : Attribute
{
    public string MenuName { get; }
    public FeelMotionActionMenuAttribute(string menuName) => MenuName = menuName;
}
```

`FeelMotion.Editor`'s Add Action dropdown (`Editor/AddActionDropdown.cs`) builds its tree from
`TypeCache.GetTypesDerivedFrom<FeelMotionAction>()`, filtered to non-abstract/non-generic types,
grouped by splitting `MenuName` on `/`. `TypeCache` scans the **whole project**, not just assemblies
that reference `FeelMotion.Editor` — as long as your type is `[Serializable]`, derives from
`FeelMotionAction`, and sits anywhere in the project, it appears in the Add Action menu with zero
other steps.

## 3. Putting custom actions in your own assembly

This is already how FeelMotion ships its own optional actions — `Runtime/Actions/UI/` is its own
`FeelMotion.UI.asmdef`, referencing only `FeelMotion` + `LitMotion` + `UnityEngine.UI`, and needed no
change anywhere else (not in `FeelMotion.Editor`, not in `FeelMotion.Tests`) to keep working. Follow
the same shape for your own actions:

```jsonc
// MyGame.FeelMotionActions.asmdef
{
    "name": "MyGame.FeelMotionActions",
    "references": ["FeelMotion", "LitMotion"],
    "autoReferenced": true
}
```

Drop your `[FeelMotionActionMenu]`-attributed classes anywhere under that asmdef's folder. Nothing in
`FeelMotion` or `FeelMotion.Editor` needs to reference your assembly back — discovery is one-directional
via `TypeCache` (§2). If your category pulls in a dependency not every consumer wants (TextMeshPro,
Physics2D, a render-pipeline package), this is also how you keep that dependency out of projects that
don't use your actions, the same way `FeelMotion.UI`'s `UnityEngine.UI` reference doesn't force itself
on a project with no UI actions (see [Concerns.md](Concerns.md) for what this does and doesn't
guarantee about final build size).

For an optional dependency that might not be installed at all (not just "installed but unused"), gate
the `#if` compilation with a `versionDefines` entry instead of an unconditional reference — this is how
`FeelMotion.Camera`/`Rendering`/`Audio`/`Physics`/`Physics2D`/`TextMeshPro` are built (each ported from
one of LitMotion.Animation's component files, replicating its behavior without referencing that
package):

```jsonc
// FeelMotion.Physics2D.asmdef
{
    "name": "FeelMotion.Physics2D",
    "references": ["FeelMotion", "LitMotion"],
    "versionDefines": [
        { "name": "com.unity.modules.physics2d", "expression": "", "define": "FEELMOTION_PHYSICS_2D" }
    ]
}
```
then wrap the whole file (or just the dependency-specific class) in `#if FEELMOTION_PHYSICS_2D` /
`#endif`. `FeelMotion.Value` is the odd one out — its actions have no target object at all (they
broadcast a tweened value via `UnityEvent<T>` for the Inspector to wire up), which is why they derive
from a different base, `ValueMotionAction<TValue,TOptions,TAdapter>`, instead of
`PropertyMotionAction<TObject,TValue,TOptions,TAdapter>`.

## 4. Rules for a well-behaved custom action

1. **No mutable runtime state on the serialized class.** Everything that changes during playback
   (captured values, handles, flags) belongs on the object `CreateRuntime()` returns, never on the
   `[Serializable]` action itself.
2. **`ValidateTarget()` must be cheap and side-effect-free.** It's called once per `Play()` before any
   handle is built — don't do expensive work in it.
3. **Honor `MotionStopMode`.** If your action can be stopped mid-flight, implement all four modes
   meaningfully — at minimum, `Cancel` must not silently restore a value the caller didn't ask to
   restore.
4. **Return a handle from `Play()`, always** — even a zero-duration placeholder (§1b). The session
   needs it to know when your action is "done" for sequencing purposes.
5. **Apply `ActionSettings` yourself if you're not going through `PropertyMotionAction`.** It's
   `_action.ActionSettings` — `StartDelay`/`TimescaleMode` — see the §1b example.
6. **Log failures once, not per-frame.** A target that's missing every frame should not spam the
   console every frame.
7. **Apply the `MotionPlaybackContext` your `CreateRuntime(in context)` receives, if you're not going
   through `PropertyMotionAction`.** `context.DurationMultiplier` scales every time quantity you hand to
   LitMotion (see the §1b example). `context.IntensityMultiplier` only matters if your action has a
   value to scale — a pure side-effect action (a callback, an audio one-shot) can ignore it, like
   `EventAction` does.

## 5. What you get for free by following this contract

- Reorder, enable/disable, duplicate, and preview in the Inspector — your action is just another
  `[SerializeReference]` element with generically-drawn fields; no custom editor code needed unless you
  want a bespoke field layout.
- Correct behavior under `Sequential`/`Parallel` playback and session-level pause/resume — all owned by
  `MotionPlayerSession`, not by your action.
- Automatic inclusion in `FeelMotionPlayer`'s progress reporting — both the player-level progress bar
  and each action's own live row in the Inspector while the whole player is playing (Architecture.md
  §10) — as long as your handle's `TotalDuration`/`Time` are meaningful (a zero-duration placeholder
  reports as instantly complete, which is correct).
- If you subclass `PropertyMotionAction` and implement `ScaleReach` the same mechanical way as `Add`,
  the player's global `IntensityMultiplier` works on your action for free, in all three `MotionValueMode`s.

## 6. What you don't get for free (own these yourself)

- Per-field validation beyond "is the target assigned" (e.g. "this clip must be mono") — add it to
  your own fields.
- Per-action `Pause`/`Resume` once your action is folded into a Sequential sequence —
  [Architecture.md](Architecture.md) §4's known limitation applies to custom actions exactly as it does
  to built-in ones.
