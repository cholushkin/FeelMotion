# FeelMotion — Concerns

Honest list of known risks and open questions in the current implementation. Not a backlog (see
[Roadmap.md](Roadmap.md) for that) — this is "things to know before you rely on this," written for
whoever picks this package up next.

## Is FeelMotion zero-allocation?

**Partially — not fully, and that's a deliberate trade-off, not an oversight.**

**What is allocation-free**: the actual per-frame tween update. `PropertyMotionActionRuntime.Play()`
binds with a `static` lambda (`.Bind(this, static (x, state) => state.ApplyValue(x))`,
`Runtime/Actions/PropertyMotionAction.cs`) — no closure capture, so LitMotion's own per-frame struct
update stays allocation-free, matching LitMotion's own design goal. Action-list iteration in
`MotionPlayerSession` is a plain loop over a `List<Entry>`, no LINQ.

**What is not allocation-free**: every `FeelMotionPlayer.Play()` call —

- allocates a new `MotionPlayerSession`, which allocates its own `List<Entry>`;
- calls `CreateRuntime(in context)` once per enabled action, each a new heap object
  (`PropertyMotionActionRuntime<...>`, `DelayAction.Runtime`, etc.) — the `MotionPlaybackContext` struct
  itself is passed by `in` and costs nothing extra;
- in Parallel mode, subscribes a `Completed +=` delegate per entry (`OnParallelEntryComplete`);
- `FeelMotionPlayer.PlayAsync()` additionally allocates a `UniTaskCompletionSource` and a closure
  (`OnDone`) per call.

**Why**: this is the direct cost of the safety guarantee in §2 of [Architecture.md](Architecture.md) — a
fresh runtime per `Play()` means two overlapping/repeated plays can never corrupt each other's state,
since nothing mutable is shared between them. LitMotion's own handles are zero-alloc *once playing*;
FeelMotion's session wrapper allocates once *per `Play()` call*, not per frame. Fine for a one-button
game where actions play on discrete triggers — but anyone calling `Play()` at high frequency (e.g. from
a per-frame system) should know this isn't the same claim as LitMotion's own zero-allocation tweens.

**If this needs to change**: pool `MotionPlayerSession` and its runtimes per `FeelMotionPlayer` instance
instead of discarding and recreating. The cost is giving up the "every `Play()` is independent"
guarantee — pooled state must be carefully reset, or a bug reintroduces the stale-state class of bug
this design avoids. Revisit if profiling on a real scene shows this matters.

## Per-action Pause/Resume inside a Sequential sequence

Documented as a known limitation in [Architecture.md](Architecture.md) §4: once an action's handle is
folded into a Sequential `LSequence`, LitMotion drives it via the sequence's own virtual time, so the
action's own `PlaybackSpeed` write (what `Pause()`/`Resume()` do) has no effect. `IMotionActionRuntime`
still exposes both methods (the interface shape is uniform across Sequential/Parallel), but calling
them on an action that's part of a running Sequential sequence is currently a silent no-op, not an
error. Session-level `Pause`/`Resume` (the common case — pausing the whole player) always works
correctly in both modes.

## Per-row live progress during Sequential playback depends on a folded handle staying queryable

`MotionPlayerSession.GetActionProgress` (backing each action row's progress bar while the whole player
is playing — `Architecture.md` §10) reads `IMotionActionRuntime.Handle.Time`/`TotalDuration` directly.
In Parallel mode that's unambiguous — each entry keeps its own independent handle. In Sequential mode,
every entry's handle is folded into one master `LSequence` (§4), so this relies on LitMotion keeping
each folded handle's own `Time`/`TotalDuration` individually meaningful, not just the master handle's.
If that ever stops holding, `GetActionProgress` falls back to `MotionActionState` (`0` until that
action's slot completes, then `1`) — rows "catch up" step-wise instead of filling smoothly, a graceful
degradation rather than a wrong number, but not stress-tested against every LitMotion release.

## Inspector duplicate-via-JSON doesn't deep-copy `UnityEngine.Object` references correctly in all cases

`FeelMotionPlayerEditor`'s Duplicate (and its post-array-growth de-dupe pass) clone a managed reference
via `JsonUtility.ToJson`/`FromJson`. This is the same technique LitMotion.Animation's own editor uses
internally, and it's fine for value fields, but `JsonUtility` serializes `UnityEngine.Object` reference
fields (e.g. a `PropertyMotionAction`'s `_target`) by instance ID, which can resolve incorrectly across
domain reloads or when the cloned object briefly has no `instanceID` context. Editor-only risk, not a
runtime one — if duplicated actions ever show a wrong/null target after a reload, this is why.

## No automated UI test for the inspector

`FeelMotion.Tests.asmdef` is edit-mode `NUnit` against the runtime API — it does not and cannot test
the UIElements inspector itself (foldout/checkbox hit-testing, the Add Action dropdown, per-action
preview). That verification is manual, in-Editor, every time `Editor/FeelMotionPlayerEditor.cs` or
`Editor/ActionRowView.cs` changes.

## Randomization adds its own small per-Play()/per-managed-loop-iteration allocations

Same trade-off as "Is FeelMotion zero-allocation?" above, extended by Architecture.md §12's
Randomization system — and only paid by an action that actually has enabled randomizer modules:

- `RandomizationSet.CreateOrNull` allocates the set and one small wrapper object per enabled module,
  once per `Play()` — an action with no randomizers (the default) gets `null` back and pays nothing.
- Each module's sampled components (≤4 floats) are a small heap array, cached for `OncePerPlay` (one
  allocation for the whole `Play()`) or re-allocated every iteration for `EveryLoop` — the latter is a
  real per-loop-iteration allocation, not just per-`Play()`. Still well below a per-frame hot path (an
  iteration is one full tween loop, not one `Update()` tick), but worth knowing if a managed-loop action
  loops at a very high rate.
- `ManagedLoopPlayer` itself (one object) and each iteration's `MotionHandle` are allocated the same way
  a plain `PropertyMotionActionRuntime` already allocates per `Play()` — no new class of allocation, just
  more of them spread across iterations instead of one upfront.

## Managed-loop iteration doesn't preserve LitMotion's own LoopType across loops

`ManagedLoopPlayer` builds each iteration as an independent `Loops = 1` handle (Architecture.md §12) —
LitMotion's own `LoopType` (`Restart`/`Yoyo`/`Incremental`) only has meaning *within* a single native
multi-loop handle, describing how consecutive loops relate to each other and to the first. A
managed-loop action effectively always restarts (`LoopType.Restart` behavior) regardless of its
authored `LoopType` — `Yoyo` (ping-pong) and `Incremental` (each loop's end becomes the next loop's
start) are not reproduced across managed iterations. Only reachable when a looping action also has an
enabled `EveryLoop` randomizer; a plain looping action with no randomizers, or only `OncePerPlay` ones,
still goes through the native-`Loops` single-handle path and keeps its authored `LoopType` exactly as
before.

## Managed-loop (EveryLoop) randomization can't be used in Sequential playback mode

Same structural reason an infinitely-looping native tween can't be folded into a Sequential `LSequence`
(above, and `MotionPlayerSession.PlaySequential`'s existing check): each managed-loop iteration's
handle is created only as the previous one completes, so there's nothing to `Append()` into the
sequence ahead of time. `MotionPlayerSession` rejects the combination through the same per-action error
isolation, rather than letting it silently misbehave or throw uncaught.

## `FeelMotion.UI`'s asmdef split doesn't guarantee stripping on its own

Splitting UI actions into their own assembly (`Architecture.md` §9) means a project that references
`FeelMotion` but not `FeelMotion.UI` never compiles `UnityEngine.UI`-dependent code — that part is
guaranteed. Whether an *unused* UI action type is actually stripped from a built player when
`FeelMotion.UI` **is** present but no scene/prefab ever instantiates that specific action is up to
Unity's managed code stripping, which decides per-type based on what's reachable from serialized data —
not something this package controls or has verified end-to-end in a real build.
