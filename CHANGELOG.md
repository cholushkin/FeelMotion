# Changelog

All notable changes to this package are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Changed

- Renamed package id from `com.cholushkin.feelmotion` to `com.qcat.feelmotion`.
- Moved `Examples/` into `Samples~/Examples` so the demo is no longer imported or compiled
  by default — it is now opt-in via the Package Manager's Samples tab.

### Removed

- Removed the unused `com.qcat.fonts-pack-a` dependency from `package.json` (it was not
  referenced by any runtime, editor, or example code).

## [0.1.0] - 2026-10-17

Initial package release.

### Added

- Core `FeelMotionPlayer` runtime: Sequential/Parallel playback, Play/Pause/Rewind/Drop,
  per-action error isolation, global duration/intensity multipliers, auto-play modes.
- Built-in action set covering Transform, UI, Camera, Rendering (incl. Material by shader
  property name), Audio, Physics/Physics2D, TextMeshPro, Particles, Control (Delay/Event/Play
  FeelMotion Player) and bare-value broadcasting.
- UIElements Inspector: searchable Add Action menu, per-action enable/remove/move/duplicate,
  live per-action and per-loop progress bars (with a distinct color for looping actions), a
  user-editable label per action, an easings cheatsheet link with an always-visible/synchronized
  custom curve, and a Debug-panel message log surfacing authoring errors (e.g. a missing material
  shader property) and playback notices.
- Edit-mode test suite (`FeelMotion.Tests`) covering the runtime's playback/value/duration math.

See [Roadmap.md](Documentation~/Roadmap.md) for what's intentionally not built yet, and
[Concerns.md](Documentation~/Concerns.md) for known limitations.
