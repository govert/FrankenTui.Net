# 260-AUT Autonomous Port Loop

State + driver record for the agent-driven Rust→C# porting loop. Anchors to
`200-PRT-port-work-breakdown.md` / `210-STS-port-status.md`.

## Mechanism

Workflows in `.claude/workflows/`:
- `port-continue.js` — Scout (Explore) → Port (Sonnet, 1-to-1 incl. tests) →
  Verify (Opus oversight: depth + shortcut-phrase scan) → silent auto-re-port of
  shortcut/major files (≤2) → Build+test with auto-fix of failing ported files (≤3).
  Args (JSON): `{ crate, rustDir, project, namespace, testProject, subdirs?, maxFiles, portTests }`.
- `fix-extras-green.js`, `fix-build-green.js` — build-fix loops used to recover the
  solution after the Runtime refactor.

Driven by `/loop` (self-paced): each iteration runs one bounded batch, reviews
`flagged[]`/`build`, updates this doc, advances. Interrupt the user only on an
unresolvable build break or a file still failing oversight after retries.

## Foundation status (2026-06-09)

- Solution **compiles** (full `dotnet build FrankenTui.Net.sln` succeeds).
- Test baseline: **2815 pass / 134 fail** (Tests.Headless). The 134 are pre-existing
  refactor fallout — ShowcaseShellTests (85), WidgetClearContractTests (14),
  EffectSystemTests (9), Toast/Telemetry/etc. — tracked for a SEPARATE cleanup, not
  caused by new ports.
- Re-ported from upstream during recovery: RuntimeFrameStats, RuntimeInputEnvelope,
  WidgetInputState, WidgetFlowDirection. Honest regression: Doctor perf section now
  shows empty stats (new `CaptureAsync` API dropped `FrameStats`).

## Target order & progress

| # | Target | Scope | Gap (scouted) | Status |
|---|--------|-------|---------------|--------|
| 1 | ftui-widgets | whole crate (top-level stubs + command_palette/focus/modal) | ~37 remain | in progress — Batches 1-3 done (~20 files); +819 tests, all faithful |
| 2 | ftui-runtime | undo/, tick_strategy/, reactive/, simulator, eprocess_throttle | ~50 files | not started |
| 3 | ftui-render / ftui-text | depth fills (diff, presenter, width_cache, editor, rope) | many partial | not started |
| 4 | rest | extras (Mermaid/effects/syntax), harness, pty, web, doctor, showcase | large | not started |

## Iteration log

- 2026-06-09 — Batch 0 (validation): ported 8 widget stubs; oversight rigorous;
  uncovered + fixed the broken-tree blocker (Extras + tools/apps) before continuing.
- 2026-06-09 — Batches 2-3: KeyboardDrag, FilePicker, VoiDebugOverlay, LogViewer,
  TextArea, Progress + others — all faithful. Auto-re-port proved itself: TextArea
  (3 attempts) and Progress (2) had weakened/trivial test assertions that the
  retry loop detected and fixed. Cumulative: full solution builds clean, headless
  3641/3768 pass (+819 ported tests; baseline failures 134→127, none from new ports).
- 2026-06-09 — Batch 1: CommandPaletteScorer (faithful, 130 tests), Help/Inspector
  re-verified faithful. Caught a worker commenting out an Inspector p95 assertion;
  fixed inline (×10 budget, enforced) and hardened the workflow: scout now uses the
  "// Port of" header + matching *Tests.cs as the done-signal (not line ratio);
  oversight grades any weakened/commented-out assertion as a shortcut; auto-re-port
  now fires on any non-empty shortcutsFound. Solution builds; Inspector 115/115.
