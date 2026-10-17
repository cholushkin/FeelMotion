# FeelMotion — Roadmap

What's actually implemented vs. what's left. For how the implemented part works, see
[Architecture.md](Architecture.md); for known risks in what's implemented, see
[Concerns.md](Concerns.md).

## Done

- **Runtime core**: `FeelMotionPlayer`, `FeelMotionAction`, `IMotionActionRunner`/`IMotionActionRuntime`,
  `MotionPlayerSession`, explicit `MotionPlayerState`/`MotionActionState` state machines.
- **Playback controls**: `Play` (starts fresh if idle/completed, resumes if paused, no-ops if already
  playing)/`Pause`/`Rewind`/`Drop` (force a restart with `Rewind()` + `Play()`), `Sequential`/`Parallel`
  built on `LSequence`, `MotionStopMode` (incl. `CompleteAndRestore`, used internally by
  `IMotionActionRuntime.Stop`/`MotionPlayerSession.Stop`), target validation + per-action error
  isolation.
- **`ActionSettings`**: common `StartDelay`/`TimescaleMode` block on every action, honored by
  `PropertyMotionAction`, `DelayAction`, `EventAction`.
- **Global multipliers**: player-level `DurationMultiplier`/`IntensityMultiplier`, threaded through
  `MotionPlaybackContext` into `CreateRuntime(in context)` — mirrors Feel's `MMF_Player`.
- **Built-in actions** (ported from LitMotion.Animation's component set, replicated without
  referencing that package — see [ExtensionGuide.md](ExtensionGuide.md) §3):
  - Core (`FeelMotion.asmdef`): Transform (Position/Rotation/Scale, each with Punch/Shake variants),
    Control (Delay, Event, Play FeelMotion Player), Particles (Emit — not in LitMotion.Animation, added
    because juice sequences need it).
  - `FeelMotion.UI`: CanvasGroup alpha, Graphic color, Image (fill amount, color, color alpha), Text
    (text/color/font size), Slider value, RectTransform (anchored position, size delta, pivot).
  - `FeelMotion.Camera`: aspect, clip planes, field of view, orthographic size, rect, pixel rect,
    background color.
  - `FeelMotion.Rendering`: Material property by name (float/int/vector/color), SpriteRenderer color,
    Volume weight (URP, guarded by `FEELMOTION_RENDER_PIPELINES`).
  - `FeelMotion.Audio`: AudioSource volume/pitch, Play One Shot (not in LitMotion.Animation — added for
    timed SFX triggers; see Concerns.md on why its `ActionSettings.StartDelay` is a real playback delay,
    unlike `EventAction`'s).
  - `FeelMotion.Physics` / `FeelMotion.Physics2D`: Rigidbody/Rigidbody2D position/rotation, each with
    Punch/Shake variants.
  - `FeelMotion.TextMeshPro`: TMP_Text text, character/word/line/paragraph spacing, font size, color,
    color alpha.
  - `FeelMotion.Value`: no-target actions that broadcast a tweened value via `UnityEvent<T>`
    (Float/Double/Int/Long/Vector2/Vector3/Vector4/Color/String) — mirrors LitMotion.Animation's
    `ValueAnimationComponent` family.
  - A full worked example of these in combination: `Scenes/ExampleCatRunner.unity` +
    `Scenes/Demo/CatRunner.cs` (`FeelMotion.Samples.asmdef`) — a FeelMotion rebuild of Feel's FeelDuck
    demo, built entirely in code (no SerializeReference authoring) via `FeelMotionPlayer.SetActions`.
- **Assembly split**: `FeelMotion` (Core/Player/Transform/Control/Particles) + `FeelMotion.UI` +
  `FeelMotion.Camera` + `FeelMotion.Rendering` + `FeelMotion.Audio` + `FeelMotion.Physics` +
  `FeelMotion.Physics2D` + `FeelMotion.TextMeshPro` + `FeelMotion.Value` (each an optional, dependency-
  isolated action category — see ExtensionGuide.md §3) + `FeelMotion.Editor` + `FeelMotion.Tests` +
  `FeelMotion.Samples` (demo-only, not part of the library proper).
- **Inspector**: UIElements editor (`CreateInspectorGUI`) — searchable Add Action dropdown
  (`TypeCache`-driven, works across assemblies with zero wiring), reorderable list, enable/disable/
  rename/remove/move/duplicate, per-action preview with a live progress bar that also tracks each
  action row while the whole player is playing, consistently-labeled "Action Settings"/"Motion"
  sections, player-level Play/Pause/Rewind/Drop + progress.
- **Lifecycle**: auto-play modes, `OnDisable`/`OnDestroy` stop/restore, Editor play-mode-exit safety.
- **Automated tests** (`FeelMotion.Tests.asmdef`, edit-mode, references `FeelMotion` only — not
  `FeelMotion.UI`): value-mode math, restore-independent-of-stop, target validation,
  `ActionSettings`/scheduler mapping, zero/invalid-action completion, Play/Pause/Rewind state
  transitions, duration/intensity multiplier math.

## Not yet done

- **Inspector parity items**: copy/paste between players (Feel's `MMF_PlayerCopy.cs` shape,
  reimplemented — static clipboard + reflection field-copy with an ignore-list for Unity's own
  bookkeeping fields), validation-warning icons for `ValidateTarget()` failures, "keep Play Mode
  changes." (The live per-row progress bar — Architecture.md §10 — already covers "currently executing"
  row highlighting.)
- **Queued replay**: deferring a new `Play()` call until the current session finishes, instead of
  resuming/no-oping, would need a dedicated opt-in mechanism — not designed yet.
- **Player-level repeat/looping**, distinct from a single action's own `SerializableMotionSettings.Loops`.
- **VitalRouter integration**: an optional external adapter (subscribe `Completed`, publish a command)
  rather than a core dependency — not built yet because nothing in `Assets/Game` consumes FeelMotion.
- **Conflict policy**: what happens when two enabled actions target the same property on the same
  object in the same session. Undefined today (last-write-wins per LitMotion's own update order,
  implicitly) — no design-time warning yet.
- **Allocation hardening**: pooling `MotionPlayerSession`/`IMotionActionRuntime` across `Play()` calls
  instead of allocating fresh each time — see [Concerns.md](Concerns.md) for why this isn't done and
  what it would cost to add.
- **Migration tooling** from `LitMotionAnimation` components: FeelMotion now has an equivalent action for
  every `LitMotionAnimation` component (see "Built-in actions" above), but there's no automated
  converter for existing `LitMotionAnimation` component data on scenes/prefabs — not built because
  nothing in `Assets/Game` uses `LitMotionAnimation` today; would matter if a future removal of that
  package needs to carry over authored data instead of re-authoring by hand.
