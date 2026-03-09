# 220-PTH Pathfinder Baseline

## Purpose

This document captures the first upstream baseline for the porting work. It
collapses the early `220-PTH` tasks into one tracked reference so the .NET
workspace, parity corpus, and verification doctrine all start from the same
upstream understanding.

## Upstream Basis

- Upstream repository: `.external/frankentui`
- Upstream workspace basis for this setup slice:
  `7a91089366bd4644e086d5a422cb76b052e3de17`
- Workspace members in scope for the port:
  `ftui`, `ftui-core`, `ftui-backend`, `ftui-tty`, `ftui-render`,
  `ftui-style`, `ftui-layout`, `ftui-text`, `ftui-runtime`, `ftui-widgets`,
  `ftui-extras`, `ftui-a11y`, `ftui-i18n`, `ftui-simd`, `ftui-harness`,
  `ftui-pty`, `ftui-web`, `ftui-showcase-wasm`, `ftui-demo-showcase`,
  `doctor_frankentui`
- Repo-level verification surfaces in scope:
  `tests/compat`, `tests/e2e`, `tests/repro`, `tests/fixtures`, and `fuzz/`

## Initial Parity Corpus

The first parity corpus is reference-first. We record the upstream sources now
and only promote local generated evidence once the corresponding .NET surface
exists.

### Render And Headless

- `.external/frankentui/crates/ftui-render/tests/headless_integration.rs`
- `.external/frankentui/crates/ftui-render/tests/presenter_control_chars.rs`
- `.external/frankentui/crates/ftui-render/tests/wide_char_clipping.rs`
- `.external/frankentui/crates/ftui-render/tests/resize_storm_regression.rs`
- `.external/frankentui/tests/textarea_wrap.rs`
- `.external/frankentui/tests/repro_viewport_fill.rs`

### Runtime And Replay

- `.external/frankentui/crates/ftui-runtime/tests/deterministic_replay.rs`
- `.external/frankentui/crates/ftui-runtime/tests/e2e_simulator_replay_fidelity.rs`
- `.external/frankentui/crates/ftui-runtime/tests/e2e_inline_mode_scrollback.rs`
- `.external/frankentui/tests/baseline.json`

### PTY And Terminal Compatibility

- `.external/frankentui/crates/ftui-pty/tests/pty_canonicalize.rs`
- `.external/frankentui/crates/ftui-pty/tests/vt_support_matrix_runner.rs`
- `.external/frankentui/tests/compat/COMPAT_MATRIX.md`
- `.external/frankentui/docs/adr/ADR-005-one-writer-rule.md`

### Web, WASM, And Showcase

- `.external/frankentui/crates/ftui-web/tests/wasm_step_program.rs`
- `.external/frankentui/docs/spec/wasm-showcase-runner-contract.md`
- `.external/frankentui/docs/testing/e2e-coverage-matrix.md`

### Doctor And Evidence

- `.external/frankentui/crates/doctor_frankentui/tests/e2e_contract_gate.rs`
- `.external/frankentui/crates/doctor_frankentui/tests/sandbox_redaction_tests.rs`
- `.external/frankentui/crates/doctor_frankentui/tests/subprocess_orchestration_integration.rs`
- `.external/frankentui/docs/spec/opentui-evidence-manifest.md`

## Terminal Support Inheritance

FrankenTui.Net inherits upstream support claims conservatively. The starting
position is:

- modern VT-capable terminals only
- Windows, macOS, and Linux remain first-class OS targets
- compatibility claims must be backed by equivalent .NET evidence before they
  are repeated in local docs
- the upstream compatibility profiles in
  `.external/frankentui/tests/compat/COMPAT_MATRIX.md` are the first target
  matrix to reproduce, not a promise to broaden support independently

The initial compatibility profiles to mirror are:

- `xterm-256color`
- `screen-256color`
- `kitty`
- `alacritty`
- `WezTerm`

## Verification Doctrine

The .NET port starts with these testing rules:

- follow the upstream no-mock posture from
  `.external/frankentui/docs/testing/no-mock-policy.md`
- allow output capture, pure data builders, and minimal wrapper-focused test
  implementations, but do not replace real terminal or render behavior with
  mocks
- use headless verification first for kernel, render, layout, text, runtime,
  and later widget behavior
- use PTY-backed verification for raw mode, cleanup, scrollback, one-writer
  behavior, and cross-terminal expectations
- use dedicated web-host runners for `FrankenTui.Web` and
  `FrankenTui.Showcase.Wasm`
- validate `FrankenTui.Doctor` through invariant, contract, subprocess, and
  evidence-manifest cases rather than through mocked orchestration

The initial .NET verification entry points are:

- `tests/FrankenTui.Tests.Headless`
- `tests/FrankenTui.Tests.Pty`
- `tests/FrankenTui.Tests.Web`
- `src/FrankenTui.Testing.Harness`
- `src/FrankenTui.Testing.Pty`

## In-Scope Host And Tool Surfaces

The first repo baseline treats these as first-class surfaces, not deferred
afterthoughts:

- `FrankenTui.Demo.Showcase`
- `FrankenTui.Showcase.Wasm`
- `FrankenTui.Web`
- `FrankenTui.Doctor`
- reusable verification support in `FrankenTui.Testing.Harness` and
  `FrankenTui.Testing.Pty`

The repo is still a port, not a fork. Upstream docs remain authoritative
reference material unless .NET-specific divergence requires local replacements.
