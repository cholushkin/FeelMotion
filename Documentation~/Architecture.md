# FeelMotion — Architecture

How FeelMotion actually works, as implemented. For what's missing or planned, see
[Roadmap.md](Roadmap.md); for known risks and the zero-allocation question, see
[Concerns.md](Concerns.md); for adding your own action types, see [ExtensionGuide.md](ExtensionGuide.md).

FeelMotion is an Inspector-first animation/feedback library: author sequences of tweens and side
effects on a `FeelMotionPlayer` component, backed by [LitMotion](https://github.com/annulusgames/LitMotion)'s
struct-based handles. It takes concrete ideas from LitMotion.Animation (component shape, Inspector
row composition) and Feel (orthogonal stop/restore, copy/paste shape) but reimplements them as
original code — Feel's license permits building on its *ideas and behavior* but forbids redistributing
its source "in their current state," so nothing here is copied verbatim from either reference.

## 1. Class map

```
FeelMotionPlayer (MonoBehaviour)                   — orchestration + lifecycle only
├── FeelMotionAction[]  (SerializeReference)        — serialized config, no runtime state
│    ├── ActionSettings (_actionSettings)           — common knobs every action has: StartDelay, TimescaleMode
│    └── PropertyMotionAction<TObject,TValue,TOptions,TAdapter>
│         ├── TransformPositionAction, TransformRotationAction, TransformScaleAction   (FeelMotion.asmdef)
│         ├── CanvasGroupAlphaAction, GraphicColorAction, ImageFillAmountAction, ...    (FeelMotion.UI.asmdef)
│         └── (user-authored custom actions, discovered via TypeCache — any assembly)
└── MotionPlayerSession (internal, not serialized, created fresh per Play())
     ├── IMotionActionRuntime[]      — one per enabled, valid action, created by CreateRuntime()
     ├── MotionHandle (Sequential)   — the single LSequence.Run() handle driving the group
     └── MotionPlayerState
```

`FeelMotionPlayer` never holds a `MotionHandle` or a captured value directly — only a reference to the
current `MotionPlayerSession` (nullable, replaced wholesale on each `Play()`). Its job is translating
public API calls (`Play`/`Pause`/`Rewind`/`Drop`) into calls on whichever session is current,
plus Unity lifecycle (`OnEnable`/`OnDisable`/`OnDestroy`/auto-play). It deliberately does **not**
implement `IMotionActionRunner` — that interface's `Resume()`/`Stop(MotionStopMode)` shape is for
individual action runtimes, not the player's own cleaner surface (§5).

## 2. `FeelMotionAction` vs. runtime — the field split

| Lives on `FeelMotionAction` (serialized, authored) | Lives on `IMotionActionRuntime` (fresh per `Play()`) |
|---|---|
| `_displayName`, `_enabled` | the action's own `MotionHandle` |
| `ActionSettings` (`StartDelay`, `TimescaleMode`) | this run's `MotionActionState` |
| target reference, `MotionValueMode`, `SerializableMotionSettings<TValue,TOptions>` (duration/ease/delay/loops) — on `PropertyMotionAction` | captured initial value (read once, at `Play()`, never cached on the action) |

A fresh `IMotionActionRuntime` per `Play()` is what makes replay/overlapping-play safe: there is no
shared mutable field on the serialized action that two plays could stomp on. `CreateRuntime()` must
never return the same instance twice.

## 3. `ActionSettings` — the common block every action carries

`FeelMotionAction` holds a `[SerializeField] ActionSettings _actionSettings`
(`Runtime/Core/ActionSettings.cs`), exposed as `action.ActionSettings`. Two fields, deliberately kept
separate from LitMotion's own per-tween `SerializableMotionSettings`:

- `StartDelay` (float ≥ 0) — extra delay before this action starts, *on top of* whatever delay a
  `PropertyMotionAction`'s own motion settings already authors. Honored by every built-in action
  (`PropertyMotionActionRuntime.Play()`, `DelayAction.Runtime.Play()`, `EventAction.Runtime.Play()`).
- `TimescaleMode` (`Scaled`/`Unscaled`/`Realtime`) — maps to one of LitMotion's own
  `MotionScheduler.Update*` statics via `ActionSettings.ResolveScheduler()`. Left at `Scaled` (the
  default), nothing is overridden — an action's own authored scheduler (if any) is respected as-is.
  This is deliberate: `ActionSettings` only *adds* a delay/timescale override on top of a
  `PropertyMotionAction`'s own `SerializableMotionSettings`, it never silently changes a scheduler the
  designer didn't ask to change.

A custom action that implements `FeelMotionAction` directly (not through `PropertyMotionAction`) gets
`ActionSettings` for free (it's on the base class) but must apply it itself in its own runtime's
`Play()` — see [ExtensionGuide.md](ExtensionGuide.md) §3.

The Inspector (§10) always renders `ActionSettings` under an "Action Settings" heading and, for
`PropertyMotionAction`-derived actions, the LitMotion `SerializableMotionSettings` field under a
"Motion" heading — so duration/ease/delay/loops are in the same place on every action.

## 4. Sessions are built on LitMotion's own `LSequence`

`MotionPlayerSession` (`Runtime/Core/MotionPlayerSession.cs`, `internal`) is created fresh by
`FeelMotionPlayer.Play()` and discarded on completion/replace. This is the single most important
runtime decision: rather than a hand-rolled queue or a web of coroutines, Sequential mode folds every
action's handle into one `LSequence`, giving the whole session **one master `MotionHandle`**:

- **Sequential**: for each enabled, valid action — call `Runtime.Play()` (which starts the action's own
  `LMotion` handle), then `builder.Append(handle)`. `LitMotion.MotionManager.AddToSequence` reparents
  that handle onto the sequence's own virtual clock, so it doesn't actually advance until the sequence
  reaches its scheduled offset — appending an already-"running" handle is correct, not a race. A
  player-level `SequenceDelay` and each action's own `ActionSettings.StartDelay` both become
  `builder.AppendInterval(...)` calls between/before `Append`s.
- **Parallel**: every enabled, valid action's `Runtime.Play()` is called directly (no `LSequence`);
  the session subscribes to each runtime's `Completed` event and counts down.
- Even non-tween actions (`DelayAction`, `EventAction`) produce a `MotionHandle` — a real
  `LMotion.Create(...)` with the duration that actually matters (zero for `EventAction`) — so the
  session never needs a special case for "actions that aren't tweens."
- Pause/Resume/Stop on the whole session is one write to the master handle
  (`PlaybackSpeed`/`TryCancel`/`TryComplete`) in Sequential mode; in Parallel mode it loops over each
  runtime's own `Pause`/`Resume`/`Stop`.

**Known limitation**: once an action's handle is folded into a Sequential `LSequence`, LitMotion drives
it via the sequence's own virtual time, not the handle's own `PlaybackSpeed` — so a true *per-action*
`Pause`/`Resume` mid-sequence isn't expressible this way. `IMotionActionRuntime.Pause()`/`Resume()`
are only fully meaningful for a standalone Parallel-mode action. Session-level pause/resume (the common
case) always works in both modes.

## 5. State machines

```csharp
public enum MotionPlayerState { Idle, Playing, Paused, Completed }
public enum MotionActionState { Idle, Playing, Paused, Completed, Cancelled }
```

Both are explicit fields set only at the point of transition — never inferred from
`MotionHandle.IsActive()`/`IsPlaying()` at read time. `MotionPlayerState` is the per-session state;
each action's own `IMotionActionRuntime` tracks `MotionActionState` independently (one action can
finish before the session does). A session with zero enabled/valid actions still transitions
`Idle → Playing → Completed` within the same frame and fires `OnComplete` — not left to chance.

`FeelMotionPlayer`'s public control surface is `Play`/`Pause`/`Rewind`/`Drop` — deliberately not a 1:1
mirror of `IMotionActionRunner`'s `Play`/`Pause`/`Resume`/`Stop(MotionStopMode)` (that shape stays on
individual action runtimes, where all four `MotionStopMode` values are meaningful):

- **`Play()`** starts a fresh session from `Idle`/`Completed`/no session, resumes if `Paused`, and is a
  **no-op** if already `Playing`. To force a fresh run regardless of state, call `Rewind()` then
  `Play()` — there is no separate `Restart()`.
- **`Pause()`** freezes time in place via the master handle's (Sequential) or each runtime's (Parallel)
  `PlaybackSpeed`.
- **`Rewind()`** — `MotionStopMode.CancelAndRestore`: stops and writes every action's captured initial
  value back.
- **`Drop()`** — `MotionStopMode.Cancel`: stops and releases the LitMotion property holds, leaving each
  value exactly where it was. The opposite of `Rewind()`.

(Named `Rewind()` rather than `Reset()` to avoid colliding with Unity's own `MonoBehaviour.Reset()`
Editor message.)

## 6. Value modes and restore (independent of Stop)

```csharp
public enum MotionValueMode { Absolute, RelativeToCurrent, FromCurrentTo }
public enum MotionStopMode { Cancel, CancelAndRestore, Complete, CompleteAndRestore }
```

- `Absolute`: tween the authored `From → To` every time, regardless of the target's current value.
- `RelativeToCurrent`: capture current value at `Play()`, tween `current → current + delta`.
- `FromCurrentTo`: capture current value at `Play()`, tween `current → To` with no offset math.

The runtime always captures the pre-play value (needed for restore regardless of mode).
`RestoreInitialValues()` is its own method — never implicit inside `Stop()`. `Cancel` leaves the value
exactly where the tween stopped; `CancelAndRestore` composes `Cancel()` + `RestoreInitialValues()`
explicitly; `Complete` snaps to the end value; `CompleteAndRestore` completes then restores.

## 7. Target validation and error isolation

`IMotionActionRuntime.ValidateTarget()` runs once per `Play()`, before any handle is built. A failing
action is skipped (never reaches the sequence); any exception thrown while creating a runtime,
validating it, or playing it is caught **per-action**, reported via `FeelMotionPlayer.ActionError`, and
the session keeps going with the remaining actions — one broken action can't stall or abort the whole
sequence.

## 8. Global duration/intensity multipliers

`FeelMotionPlayer` carries two player-level knobs — `DurationMultiplier` and `IntensityMultiplier`
(default `1`, both `Min(0)`) — mirroring Feel's `MMF_Player.DurationMultiplier`/`FeedbacksIntensity`.
They're packaged into a `MotionPlaybackContext` (`Runtime/Core/MotionPlaybackContext.cs`, a readonly
struct) and passed into `FeelMotionAction.CreateRuntime(in MotionPlaybackContext context)`; each
runtime captures the context once, at `Play()`.

- **`DurationMultiplier`** scales every time quantity a runtime hands to LitMotion: a
  `PropertyMotionAction`'s `Duration`/`Delay` and `ActionSettings.StartDelay`, `DelayAction.Duration`,
  `EventAction`'s `StartDelay`, and the player's own `SequenceDelay` between Sequential entries. Clamped
  to a small positive floor (`0.0001f`), not exactly `0`, to avoid a degenerate zero-time tween.
- **`IntensityMultiplier`** scales how far a `PropertyMotionAction`'s value travels. Each concrete
  action implements `ScaleReach(in TValue from, in TValue to, float factor)` (same shape as `Add`), and
  `PropertyMotionActionRuntime.Play()` applies it uniformly across all three `MotionValueMode`s:
  `end = ScaleReach(start, end, intensity)`, i.e. `start + (end - start) * intensity`. `1` leaves `end`
  unchanged; `0` means no motion. Control actions (`DelayAction`, `EventAction`) have no value to scale
  and ignore `IntensityMultiplier`.

Standalone per-action preview (§10) builds its throwaway runtime with the owning player's
`PlaybackContext` too, so a preview matches what a real `Play()` would do.

## 9. Assembly layout

```
Assets/Libs/FeelMotion/
├── Runtime/
│   ├── FeelMotion.asmdef              — Core, Player, Control/Transform/Particles actions
│   │   references: LitMotion, UniTask
│   ├── Core/                          — FeelMotionAction, ActionSettings, IMotionActionRuntime/Runner,
│   │                                     MotionPlayerSession, state/mode enums
│   ├── Actions/
│   │   ├── PropertyMotionActionBases.cs — typed Add/ScaleReach bases (Float/Vector2/3/4/Color/Int/
│   │   │                                   Long/Double/Rect), shared by every category below
│   │   ├── Control/                   — Delay, Event, Play FeelMotion Player  (in FeelMotion.asmdef)
│   │   ├── Transform/                 — Position/Rotation/Scale + Punch/Shake (in FeelMotion.asmdef)
│   │   ├── Particles/                 — ParticleEmitAction                   (in FeelMotion.asmdef)
│   │   ├── UI/
│   │   │   └── FeelMotion.UI.asmdef   — CanvasGroup/Graphic/Image/Text/Slider/RectTransform actions
│   │   │       references: FeelMotion, LitMotion, UnityEngine.UI, Unity.Collections
│   │   ├── Camera/
│   │   │   └── FeelMotion.Camera.asmdef      — references: FeelMotion, LitMotion
│   │   ├── Rendering/
│   │   │   └── FeelMotion.Rendering.asmdef   — Material/SpriteRenderer/Volume(URP) actions
│   │   │       references: FeelMotion, LitMotion, Unity.RenderPipelines.Core.Runtime
│   │   │       versionDefines: com.unity.render-pipelines.core → FEELMOTION_RENDER_PIPELINES
│   │   ├── Audio/
│   │   │   └── FeelMotion.Audio.asmdef       — AudioSource volume/pitch, Play One Shot
│   │   │       references: FeelMotion, LitMotion
│   │   │       versionDefines: com.unity.modules.audio → FEELMOTION_UNITY_AUDIO
│   │   ├── Physics/
│   │   │   └── FeelMotion.Physics.asmdef     — Rigidbody position/rotation + Punch/Shake
│   │   │       references: FeelMotion, LitMotion
│   │   │       versionDefines: com.unity.modules.physics → FEELMOTION_PHYSICS
│   │   ├── Physics2D/
│   │   │   └── FeelMotion.Physics2D.asmdef   — Rigidbody2D position/rotation + Punch/Shake
│   │   │       references: FeelMotion, LitMotion
│   │   │       versionDefines: com.unity.modules.physics2d → FEELMOTION_PHYSICS_2D
│   │   ├── TextMeshPro/
│   │   │   └── FeelMotion.TextMeshPro.asmdef — TMP_Text actions
│   │   │       references: FeelMotion, LitMotion, Unity.TextMeshPro, Unity.Collections
│   │   │       versionDefines: com.unity.ugui ≥2.0.0 → FEELMOTION_TMP
│   │   └── Value/
│   │       └── FeelMotion.Value.asmdef       — ValueMotionAction<TValue,TOptions,TAdapter> (no
│   │           references: FeelMotion, LitMotion, Unity.Collections   target — broadcasts via UnityEvent<T>)
│   └── Player/                        — FeelMotionPlayer
├── Editor/
│   └── FeelMotion.Editor.asmdef       — FeelMotionPlayerEditor, ActionRowView, AddActionDropdown
│       references: FeelMotion, LitMotion, UniTask (Editor-only)
├── Tests/
│   └── FeelMotion.Tests.asmdef        — edit-mode NUnit, references FeelMotion (not FeelMotion.UI)
├── Scenes/
│   ├── ExampleCatRunner.unity         — FeelMotion rebuild of Feel's FeelDuck demo
│   └── Demo/
│       └── FeelMotion.Samples.asmdef  — CatRunner.cs (demo-only, not part of the library proper)
│           references: FeelMotion, FeelMotion.Audio, FeelMotion.Rendering, LitMotion,
│                        Unity.InputSystem, Unity.RenderPipelines.Core.Runtime
└── Documentation/
```

`FeelMotion.UI` (and every other non-core category above) exists so a project that never uses that
category's actions doesn't need its dependency compiled in alongside Core/Transform/Control — see
[Concerns.md](Concerns.md) for what this split does and doesn't guarantee about final build size.
Dependency direction: `FeelMotion` → `LitMotion`, `UniTask` only; every other `FeelMotion.*` runtime
assembly → `FeelMotion`, `LitMotion`, plus its own specific dependency. None references
`GameLib`/`Game.Runtime` — matches this project's `CLAUDE.md` rule that libraries under `Assets/Libs`
must not depend on `Game/Runtime`. `FeelMotion.Samples` is the one exception to "no new deps without a
reason": it exists purely to host the demo scene's driver script and isn't referenced by anything else.

Every one of these categories was ported from the equivalent `LitMotion.Animation` component file
(`AudioComponents.cs`, `CameraComponents.cs`, etc.) — same behavior, replicated without referencing
`LitMotion.Animation` at all (only base `LitMotion`). See [ExtensionGuide.md](ExtensionGuide.md) §3 for
the `versionDefines` pattern used to gate each optional dependency, and [Roadmap.md](Roadmap.md) for the
full list of ported actions.

Discovery of action types (built-in or custom, in any assembly) needs no asmdef wiring at all: the
Editor's Add-Action dropdown (`Editor/AddActionDropdown.cs`) uses
`TypeCache.GetTypesDerivedFrom<FeelMotionAction>()`, which scans the whole project regardless of which
assemblies reference `FeelMotion.Editor`.

## 10. Inspector

`Editor/FeelMotionPlayerEditor.cs` is a UIElements (`CreateInspectorGUI`) editor, not IMGUI. Each action
row is an `Editor/ActionRowView.cs`: a `Foldout` (the triangle) with an icon and a bold "enabled"
`Toggle` inserted into its header, a progress bar, and a per-action preview button, plus a context menu
(Remove/Move Up/Move Down/Duplicate). The row composition — and specifically making only the enabled
Toggle's checkmark glyph pickable while everything else in the toggle ignores pointer events — is what
keeps the Foldout's own triangle independently clickable; see `ActionRowView`'s doc comment for the
exact mechanism. `AddActionDropdown` (an `AdvancedDropdown`, IMGUI) is unchanged and opens from the
UIElements "Add Action..." button via `button.worldBound`.

Per-action preview builds a throwaway `IMotionActionRuntime` directly from the action
(`action.CreateRuntime(player.PlaybackContext)`) and plays it in isolation — no `MotionPlayerSession`
involved, since that type is `internal` to the runtime assembly and a single action doesn't need
sequencing.

Each row's progress bar also tracks the **live session** while the whole player is `Playing`/`Paused`:
`FeelMotionPlayerEditor` keeps a `(row, action)` list built in `CreateActionRow`, and the Debug-panel
`.Every(50)` scheduler calls `FeelMotionPlayer.GetActionProgress(action)` →
`MotionPlayerSession.GetActionProgress` (a reference-equality scan of its entries) for each tracked row.
A row whose action isn't part of the current session (disabled, or failed `ValidateTarget()`) stays at
`0`. `ActionRowView.IsPreviewing` keeps the two update paths from colliding: `TogglePreview` sets it
while a standalone preview drives that row, and the session poll skips any row with it set.

**Known limitation**: in Sequential mode this depends on each folded entry's `MotionHandle.Time`/
`TotalDuration` staying individually queryable once appended into the master `LSequence` (§4). If a
given LitMotion version doesn't expose that, `GetActionProgress`'s fallback (`MotionActionState`-based:
`0` until that action's slot completes, then `1`) still makes the row visibly "catch up" step-wise.
Parallel mode is unaffected either way.

## 11. Events

Plain C# events + `UnityEvent`s on `FeelMotionPlayer` (`Played`/`Paused`/`Resumed`/`Stopped`/
`Completed`/`ActionError`, `_onPlay`/`_onComplete`/`_onStop`). `FeelMotion.asmdef` stays free of
`VitalRouter`/`R3` — if a consumer wants lifecycle events on `VitalRouter.Router.Default`, that's a
thin external adapter (subscribe `Completed`, publish a command), not something baked into the core
package, matching this project's convention that `R3` is for `rMVVM`/UI state and `VitalRouter` is for
discrete gameplay events (`CLAUDE.md` §3).
