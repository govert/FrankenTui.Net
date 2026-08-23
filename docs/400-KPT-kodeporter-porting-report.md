# 400-KPT KodePorter Porting Report

## Purpose

This is the maintained narrative report for completing FrankenTui.Net against
upstream FrankenTUI. It is a human-readable KodePorter guidance view, not the
source of truth. Durable port intent lives under `.kodeporter/`; attributable
assertions and decisions live in `gneiss.db`; dense maps, captures, test rows,
traces, and comparison artifacts are organ data.

The report is updated after each meaningful reconciliation or implementation
batch. Earlier entries are retained when later evidence changes the conclusion.

## Port Characterization

FrankenTui.Net is a **tracked direct systems/repository port** from Rust to C#.
It is not a clean-room reimplementation, a source-language API clone, or a
drop-in Rust package substitute.

Its governing contract is:

- track upstream `main` and pin every reconciliation basis;
- retain a traceable crate/module/file/screen/tool relationship, normally
  one-to-one;
- preserve observable values, cells, byte streams, state transitions, effects,
  failure/cleanup timing, determinism, and operational behavior;
- require exact output where the source and context define an exact result;
- permit idiomatic .NET shape only where it does not obscure provenance or
  weaken behavior;
- keep runtime realization and host routes visible separately from surface
  availability;
- keep every necessary difference narrow, named, justified, tested, and
  reviewable;
- remain .NET 10-only, dependency-light, NativeAOT-conscious, and
  cross-platform.

The formal criterion is
`.kodeporter/criteria/frankentui-systems-parity.json`. Static showcase frames
use the narrower `showcase-frame-parity@1` criterion, and terminal lifecycle
uses `terminal-host-parity@1`.

## Completion Definition

“Complete” is a contextual preservation conclusion and will not be inferred
from build success or from a closed task list. It requires all of the following
on one labeled basis pair:

1. A closed, reproducible inventory of all in-scope upstream production,
   showcase, host, tool, and verification surfaces.
2. No unexplained applicable source item and no unreviewed fallback or unknown
   realization.
3. Passing build, test, deterministic replay, differential, PTY, web, real-host,
   NativeAOT, and operational gates applicable to the changed justification
   cone.
4. All 45 showcase screens materially preserve state and interaction; declared
   exact lanes are exactly equal.
5. Every surviving divergence is narrow, necessary under the charter, and
   accepted by an authorized review rather than merely documented by the
   implementer.
6. The target is reconciled to current upstream `main`, with no newer
   port-impacting source basis left unassessed.

## Current Labeled View — 2026-07-16 Baseline

### Context and bases

| Coordinate | Observed value | Interpretation |
| --- | --- | --- |
| Governing target intent | `CHARTER.md` | Direct, faithful, updateable systems port |
| Documented source basis | `f958e59e1406a90fdb92512103e3591911a9d68c` | Historical definition in `210-STS` / `242-MAP` |
| Embedded source checkout | `33ad1c57` detached, plus untracked `dump_screens.rs` | Actual pre-refresh workspace testimony |
| Fetched upstream `origin/main` | `15cc6543f76b814394c590f9e7719dedd6684e4c` (2026-07-12) | Required next source basis |
| Upstream delta | 330 commits, 370 changed files from documented basis | New evidence invalidates any unqualified “up to date” conclusion |
| Target commit | `afd20f0`, eight commits ahead of target `origin/main` | Committed local basis |
| Target state | Substantial pre-existing modified/untracked source, tests, tools, docs, and generated evidence | Actual executable target context; ownership of individual changes is not inferred |
| Policy | `frankentui-systems-port@1` | Preservation is not auto-accepted |

The source-basis mismatch is a **data change**, not a policy change. The
KodePorter formalization is a **definition/method change**: it makes the prior
standard explicit but does not itself change the source or prove preservation.

### Fresh observations

| Observation | Result | Consequence |
| --- | --- | --- |
| `dotnet build FrankenTui.Net.sln --no-restore` | success in 51.458 s; 70 warnings, 0 errors | Buildability observed; no parity conclusion |
| `dotnet test FrankenTui.Net.sln --no-restore --no-build` | 3,836 pass, 1 fail, 2 skip | Current test gate is not green |
| PTY partial-read repetition | 4 failures / 1 pass across five isolated runs | Timing-sensitive harness behavior; cause not yet accepted as product or test defect |
| Skipped tests | two skips state tracing is not ported | Positive evidence of an open capability; cannot be reported as absent without closed surface evidence |
| 45-screen comparison against current embedded upstream snapshots | 0 exact; 106/1,080 equal rows; 974 differing rows | Current static showcase preservation is disproved for this target/source context |
| Cached comparison corpus vs embedded upstream | all 45 cached snapshots hash-differ | Old comparison artifacts are stale organ data |
| Doctor benchmark run | exit 0; 10 measurements; no benchmark errors | Operational observation survives, but its report hard-codes bootstrap basis `7a910893` and cannot support current-basis preservation |

The 0/45 result is sensitive to current target routing. The dirty
`ShowcaseSurface.BuildContent` selects legacy inline builders instead of the
committed `Screen01...Screen45` classes, while new untracked screen widgets are
also present. This is evidence of an interleaved migration, not evidence about
who authored it or whether it should be discarded. The routing must be
reconciled deliberately before attributing mismatches solely to missing screen
implementations.

### Current conclusion

**Preservation standing: open / not accepted.**

The repository is highly implemented and buildable, but it is neither shown
complete nor shown current. Six previously registered partial contract families
remain, the current dirty-tree showcase fails every exact static lane, the test
gate has one nondeterministic failure and two explicit capability skips, and a
large unassessed upstream delta exists.

This conclusion supersedes only the *current interpretation* of older status
phrases. It does not erase the historical runs and implementation waves recorded
in `210-STS`.

## KodePorter Record

The durable domain record intentionally contains six migration units rather
than one noun per file or tool result:

- `tracked-repository`
- `kernel-render`
- `runtime-layout-text`
- `widgets-extras`
- `showcase-hosts`
- `verification-tooling`

The 45 static showcase cases are a closed **organ denominator** only for the
upstream screen registry at 80x24, frame 0. They do not close interactive,
multi-viewport, terminal-effects, or host coverage. Source/target maps, the
upstream delta matrix, comparison rows, and test runs remain regenerable organ
data.

One infrastructure extension was required immediately: source pinning now has
an opt-in `rust-workspace-v1` method that hashes every Rust source and Cargo
manifest across a multi-crate workspace while leaving the legacy single-crate
hash rule unchanged.

The first generated organ stores make the scale and the uncertainty explicit:

| Organ/view | Current result | What it may support |
| --- | --- | --- |
| Closed upstream production-file surface | 614 Rust files at `15cc6543` | Denominator for file-level source coverage |
| Closed target production-file surface | 445 C# files in the captured worktree | Denominator for target ownership/provenance review |
| Rust semantic map | 87,872 entities; 0 analyzer diagnostics | Source symbol navigation and candidate generation |
| C# semantic map | 16,535 entities; 42,754 analyzer diagnostics | Candidate hints only until project-reference loading is repaired |
| Exact file-header provenance | 113 target files cite an exact upstream source; 61 more have a same-stem candidate; 440 upstream files have neither | Positive correspondence evidence and a typed `unexplained` queue, not proof of absence |
| Derived open-work view | six migration units; receipt `49e15598a985ade808a730d027a2d6fa361935424aa91deccb9cb969de3ef1ae` | Reproducible scheduler input, not durable task truth |

This exposed a KodePorter tooling gap: a semantic-map provider that cannot load
the target's project-reference closure must lower its standing and retain its
diagnostics in the envelope. A large entity count must never make a degraded
analysis look authoritative.

## Derived Work View

The current priority queue is:

1. Move the managed source checkout to fetched `origin/main`, then pin a cleanly
   labeled repository-wide basis without losing the pre-existing helper.
2. Capture a closed source module/file surface and a target owner/provenance
   surface; classify missingness rather than asserting absence from name search.
3. Reconcile the showcase dispatcher and dedicated screen classes, then rerun
   the 45-screen plan-driven differential observation.
4. Classify and port the 330-commit upstream delta in dependency order.
5. Resolve the PTY timing failure and the two tracing skips with independent
   evidence.
6. Replace stale/hard-coded basis labels in generated doctor and comparison
   evidence.
7. Close kernel/render, runtime/layout/text, widgets/extras, showcase/hosts, and
   verification/tooling gaps with focused gates after each batch.

## Batch Journal

### Batch 0 — Foundation and baseline (2026-07-16)

Intent:
- characterize the port from its charter rather than assume a generic library
  template;
- establish current evidence before modifying implementation;
- preserve the heavily interleaved target worktree.

Actions and learnings:
- initialized KodePorter/Gneiss records and conservative policy;
- introduced the three compact criteria and six durable migration units;
- found that simple Rust pinning was single-crate-only and added an opt-in
  Cargo-workspace hashing method with focused tests;
- fetched upstream refs and discovered the documented basis was 330 commits
  behind current `main`;
- established that build success coexists with a failing test, two explicit
  skips, stale doctor basis metadata, stale comparison snapshots, and 0/45
  current exact showcase frames;
- identified current showcase routing as an interleaved migration requiring
  reconciliation rather than blind overwrite.

Status after batch:
- formalization: in progress, structurally instantiated;
- source reconciliation: open;
- target preservation: not accepted;
- implementation completion: open.

### Batch 1 — Current basis, independent observation, and input/render safety (2026-07-16)

Intent:
- move from the stale documented basis to current upstream testimony;
- exercise the observation/preservation split on a real differential lane;
- close bounded correctness failures before expanding breadth.

Basis and evidence changes:
- moved the embedded source checkout to detached upstream
  `15cc6543f76b814394c590f9e7719dedd6684e4c` while preserving the untracked
  source-side screen-dump helper;
- pinned the resulting whole Rust workspace as
  `upstream-15cc6543-local-helper` and captured the closed 614-file production
  surface separately, so the helper is truthfully present in the basis but not
  silently counted as product source;
- ran `showcase-static-frames` through independent source/target adapters. The
  observation is fail, 0/45, aid
  `a4f724df57fab0c5af055b02af38c920929be8f55e88e14f36fd5ce743765c69`,
  receipt
  `43fbc915dd218d5b60a74018b881d2a9a2655a2d819426db25ed018382f67147`.
  No `kp.preservation` conclusion was auto-created;
- the target adapter changed immediately after its initial target pin. The run
  remains valid testimony about those consumed coordinates, but will be rerun
  against a fresh target pin before it is used as the current acceptance view.

Implementation observations:
- ported current-upstream terminal parser recovery for complete DCS payloads,
  DCS-only interception, Alt ASCII/Unicode input, stray paste-end handling, and
  CSI control reprocessing;
- ported presenter last-column wrap-pending invalidation and wide-cell
  pre-clear/continuation ownership fixes;
- repaired Windows ConPTY reads so a timed read waits for first data, EOF, or
  deadline and then drains available bytes, rather than sleeping for the whole
  timeout before a single poll;
- added the missing core `KeySequenceInterpreter`, preserving the upstream
  timeout, flush, reset, modified-Escape, and double-Escape behavior. The .NET
  adaptation is explicit: release/repeat filtering occurs before construction
  of `KeyTerminalEvent`, because the managed semantic event denotes a press;
- completed the first runtime lifecycle slice: every run exit now completes
  lifecycle once, primary failures retain precedence, `on_error` precedes
  `on_shutdown`, cleanup steps remain isolated, active subscriptions reconcile
  and stop in two phases, background failures reach the model, and
  `SaveStateAndQuit` invokes the persistence boundary;
- taught the render sanitizer to recognize C1 ST (`U+009C`) inside OSC,
  DCS, PM, and APC strings so legitimate following text is not swallowed;
- seven focused input/render regressions pass; 96 related input/render tests
  pass; the PTY regression passes 11/11 repeated cases and the PTY class passes
  28/28; key-sequence tests pass 11/11; C1-ST sanitizer tests pass 5/5; runtime
  lifecycle tests pass 83/83. A settled solution build then passed with 0
  warnings and 0 errors, and the contemporaneous full headless suite observed
  3,822 pass, 0 fail, and the two already-known tracing skips. The five
  sanitizer cases were added immediately after that full-suite coordinate and
  are therefore reported separately rather than retroactively included.

Challenges and learning:
- an intermittent test was not merely “flaky”: comparison with the upstream
  blocking contract identified a real Windows adapter timing mismatch, while a
  deterministic phase-gated child process removed the test's separate
  sleep-boundary ambiguity;
- the managed parser is call-scoped while upstream now carries streaming parser
  state. The bounded recovery fixes are preserved, but streaming state, Kitty
  release/repeat kinds, bounded CSI/OSC state, and X10 mouse coverage remain a
  typed capability gap rather than being hidden behind passing regressions;
- lifecycle completion exposed rather than erased the next runtime boundary:
  persistence load/save, signal/exit-code handling, real input dispatch,
  simulator lifecycle, evidence/replay, policy bridges, telemetry installation,
  render-mode switching, and YAML parsing remain explicit stubs;
- several upstream fixes are structurally inapplicable to the current target
  design (for example OSC-8 ID injection when no ID-emitting API exists). Such
  cases need an inspectable correspondence/divergence decision, not a synthetic
  implementation merely to improve name coverage.

Change-basis classification:
- source refresh, new runs, and implementation/test results: **data/evidence
  change**;
- whole-workspace pinning and adapter-based plan execution: **verification
  method change**;
- policy and authority: unchanged.

Status after batch:
- the prior PTY failure is closed by new evidence;
- input/render safety standing improved but is not a whole-unit preservation
  claim;
- the two tracing skips and numerous closed-surface `unexplained` entries remain;
- overall preservation remains **open / not accepted**.

### Batch 2 — Decision evidence, deterministic replay, and compositional contracts (2026-07-16)

Intent:
- close several high-leverage upstream contracts whose absence made later
  runtime and rendering claims difficult to observe or explain;
- replace the two modal-tracing skips with production instrumentation and
  independent observers;
- keep each addition attached to the same source basis and to focused,
  regenerable evidence rather than infer whole-unit preservation.

Basis and scope:
- source basis remains detached upstream
  `15cc6543f76b814394c590f9e7719dedd6684e4c`;
- the target coordinate is the evolving preserved worktree after Batch 1. It
  is deliberately not relabeled as a pinned basis until the concurrent batch
  settles; observations below identify their command/test slice instead;
- affected migration units are `kernel-render`, `runtime-layout-text`, and
  `widgets-extras`;
- principal upstream testimony consulted was `ftui-render/render_certificate`,
  `ftui-text/cluster_map` and shaping output records, `ftui-runtime/event_trace`,
  `diff_evidence`, `evidence_bridges`, and `policy_config`, core animation
  group/stagger modules, and modal stack/focus tracing.

Implementation observations:
- added render certificates with conservative `FullRequired`, `SkipAll`, and
  `Narrow` outcomes, stable evidence JSON, and translation to the existing
  managed diff-skip hint. Seven managed cases and the six source cases pass;
- added UTF-8-byte-coordinate `ClusterMap` lookups, ranges, grapheme extraction,
  and the shaped-run records it consumes. All 29 focused cases pass. This does
  **not** claim the wider shaping engine is present;
- completed the event-trace schema, gzip/plain JSONL reader, lossless trace
  record replay, timestamp/delay controls, and epsilon-aware evidence verifier.
  A verifier may report deterministic success only with positive coverage of
  every recorded evidence entry. Six focused cases cover all record families,
  Rust-compatible tagged key/mouse shapes, malformed input, replay ordering,
  and field-level mismatch reporting;
- added the core animation contract plus group and stagger composition,
  including insertion/replacement lifecycle semantics, saturating time
  arithmetic, and exact xorshift64 jitter before `TimeSpan` projection. The 43
  focused cases pass;
- added production modal render spans through `ActivitySource` and discrete
  lifecycle/focus/trap events through `DiagnosticListener`. The two prior skips
  now execute through independent observers; 50 modal-stack and 86 focus
  integration cases pass with zero skips;
- ported all five currently bounded runtime evidence bridges, all nine
  available policy-to-controller conversions, and decision/transition JSONL
  with a fixed-capacity runtime ledger. Evidence and policy focused tests pass
  29/29. The runtime ledger is explicitly named `RuntimeDiffEvidenceLedger` to
  avoid silently shadowing the already different render-layer ledger;
- the combined certificate, cluster, trace, animation, and modal slice passes
  221/221 with no skips. The full headless corpus at this coordinate observed
  3,919 pass, one performance-timing failure, and zero skips. The failing
  Fenwick logarithmic-scaling test then passed in five consecutive isolated
  runs; its first full-suite observation remains failure testimony and is not
  rewritten as green.

Challenges, divergences, and learning:
- a source name was not sufficient type identity in the managed realization:
  introducing a second `DiffEvidenceLedger` changed resolution inside
  `AppRuntime`. An explicit domain-qualified managed name was safer than
  preserving spelling while changing behavior;
- Rust tracing maps naturally to two .NET mechanisms: scoped spans to
  `ActivitySource`, discrete structured events to `DiagnosticListener`. This is
  a realization divergence with observable field parity, not a new domain
  concept;
- nanosecond animation jitter must be computed before projecting to 100 ns
  `TimeSpan` ticks or deterministic source parity is lost;
- closed trace-entry coverage is a useful instance of the KodePorter absence
  rule: “no mismatch” is unknown until every applicable recorded entry has a
  comparison result;
- the aggregate performance failure is operational evidence sensitive to
  scheduling/JIT context. Isolated success narrowed the diagnosis but did not
  erase it. The managed test now symmetrically warms both call sites and takes
  the best of five identical rounds, retaining the source's 10x complexity
  criterion while excluding tiered-JIT and scheduler pauses. That adapted gate
  passes 10/10 isolated repetitions; an aggregate rerun after the concurrent
  batch is still required;
- generic `decision_core` traits, policy file/discovery loaders, and the full
  shaping engine remain typed open work. Target-only
  `SignificantDirtyRows` is reported as unsupported by the upstream bridge,
  not assigned a fabricated source label.

Change-basis classification:
- implementations, new observations, closed tracing skips, and focused test
  results: **data/evidence change**;
- managed trace reader/replayer/verifier and independent telemetry observers:
  **verification method change**;
- explicit type naming and managed telemetry realization: recorded
  correspondence/divergence decisions, with policy and authority unchanged.

Status after batch:
- the explicit tracing capability gap is closed and the headless corpus now
  has zero skips;
- runtime evidence, deterministic replay, animation composition, cluster maps,
  and render certificates have bounded positive evidence;
- the historical aggregate operational failure remains replayable testimony,
  while the adapted gate has bounded positive evidence. The current 0/45
  showcase observation, open loaders/shaping/runtime surfaces, and
  closed-surface unexplained queue prevent a preservation acceptance claim;
- overall preservation remains **open / not accepted**.

### Batch 3 — Closed foundations, shared coordinates, and negative evidence (2026-07-16)

Intent:
- make the KodePorter work instructions usable by attaching exact source and
  target map coordinates to the six already-declared migration units;
- close several foundational contracts that later differential work depends
  on: streaming input, animation composition, accessibility testimony, policy
  loading, state persistence, and generation-safe grapheme ownership;
- retain failed or blocked source runs as method/environment evidence rather
  than converting them into target-port conclusions.

Basis and scope:
- a fresh fetch confirmed that detached source basis
  `15cc6543f76b814394c590f9e7719dedd6684e4c` still equals `origin/main` on
  2026-07-16;
- the target remains the evolving attributed worktree. It is intentionally not
  assigned a new basis label while wrap, input-macro, and widget-accessibility
  changes are active;
- affected migration units are `kernel-render`, `runtime-layout-text`, and
  `widgets-extras`; KodePorter guidance work also affects all six WorkBrief
  projections without asserting that their implementation units are complete.

KodePorter operating observations:
- the first generated WorkBriefs exposed an infrastructure gap: a migration
  unit could be created before a map existed, but there was no append-only way
  to add its later source/target anchors. KodePorter now provides
  `kp unit add-anchors`, appends only new anchors, preserves the dossier, and is
  idempotent. Its complete test corpus passes 111/111;
- exact anchors were then attached to `tracked-repository`, `kernel-render`,
  `runtime-layout-text`, `widgets-extras`, `showcase-hosts`, and
  `verification-tooling`. Six regenerated WorkBrief reports now carry real map
  coordinates instead of empty scope descriptions;
- the regenerated open-work view has receipt
  `49e15598a985ade808a730d027a2d6fa361935424aa91deccb9cb969de3ef1ae`
  at data/definition cut 50. It remains a scheduler view, not task testimony;
- the source-verification environment and its failure were recorded as a
  KodePorter note (`64ee4fa26b49`), so later readers can distinguish missing
  source execution from failed target behavior.

Implementation and verification observations:
- the input parser is now a persistent bounded state machine covering Ground,
  Escape, CSI/ignore, SS3, OSC/ignore, DCS, UTF-8, X10, and streaming paste.
  It carries Kitty press/repeat/release, Super, F1-F24, bounded recovery, and
  timeout resolution. The independent source parser passes 123/123; the
  managed closeout selection passes 16/16, including a fixed-seed 128-way
  bulk-versus-one-byte chunk test;
- the complete default-feature animation family is represented by base
  animation, callbacks, presets, spring, timeline, group, and stagger modules.
  The source denominator passes 255/255. The managed family passes 248/248;
  the different count is explained by grouped xUnit scenarios, while the newly
  assigned families directly translate 202/202 applicable source cases;
- schema-directed policy loading now covers the applicable TOML, JSON, Cargo
  metadata, discovery precedence, validation, and typed-error contract without
  pretending to be a general TOML parser. Its managed slice passes 38/38;
- the `ftui-a11y` crate foundation now carries all 23 roles, full node/state/
  live-region records, immutable tree/diff/mirror/announcement behavior,
  preference exports, motion filtering, and contrast profiles. Source tests
  pass 85/85 and the consolidated managed suite passes 27/27. No action enum
  was invented because current upstream has none;
- state persistence now has typed storage failures, defensively copied memory
  storage, base64 JSON file storage with write-then-rename replacement, and a
  thread-safe registry. Re-entrant backend I/O does not hold the cache lock,
  and mutation during a flush remains dirty. Program auto/manual load/save and
  checkpoint paths now use the configured registry; 22 consolidated registry
  cases and 105 combined registry/program cases pass;
- the prior grapheme-pool placeholder was replaced after audit found that it
  reused generation zero and could let a stale ID alias later text. The port
  now matches masked generations, LIFO slot reuse, clear invalidation,
  reference saturation, closed-set mark-and-sweep, clone independence, and
  frame/buffer shared resolution. The independent source oracle passes 60/60;
  26 managed cases plus 201 related buffer/presenter/toast cases pass;
- the managed headless project builds successfully at this coordinate. A
  settled aggregate test run is deferred until the three concurrent slices
  finish, so no whole-corpus green conclusion is inferred here.

Negative evidence, adaptations, and boundaries:
- focused `ftui-core` and `ftui-text` source suites execute successfully, but
  every attempted `ftui-runtime` source test currently stops before execution:
  pinned `rustc 1.96.0-nightly (2026-03-11)` cannot compile current upstream
  `ftui-widgets/fenwick.rs` because `isolate_lowest_one` requires a newer
  feature/toolchain. The state-persistence source file declares 37 default and
  five feature-gated tests, but they are **not** reported as passing. This is a
  verification-environment observation, not a port mismatch;
- the parser still has no target clipboard event for OSC-52, collapses four
  directional source scroll kinds to the existing coarse `Scroll`, and uses
  managed persistent `Parse` overloads instead of Rust callback/vector reuse
  APIs;
- `.NET TimeSpan` resolves to 100 ns rather than Rust's 1 ns. Two non-default
  feature-gated spring tracing cases remain an explicit realization boundary;
- file persistence uses portable managed `File.Move(overwrite: true)` as the
  closest cross-platform realization of source atomic rename;
- the earlier operational Fenwick observation remains preserved. Its managed
  gate now symmetrically warms both call sites and takes the best of five
  identical rounds, retaining the source 10x bound while excluding tiered-JIT
  and scheduler pauses; that adapted gate passes 10/10 isolated repetitions.

KodePorter learning:
- anchors belong to the durable port record because they express intended
  scope, but the expanded map slice in a WorkBrief remains organ/view data;
- source test availability is a separate warrant axis. Counting declarations
  supplies a denominator, while a compiler/toolchain failure supplies no pass
  observation;
- “same public text” was insufficient for the grapheme contract: realization
  route and ownership lifetime had to be visible to discover that frame and
  buffer used disconnected stores. Runtime-route visibility therefore remains
  essential alongside API coverage;
- consolidated managed tests may cover a closed source denominator without
  sharing its case count. Reports must state the mapping and applicable
  denominator instead of presenting a misleading quotient;
- the conservative ontology remains adequate: these findings required richer
  observations, map coordinates, typed missingness, correspondence, and
  realization views, not new permanent domain nouns.

Change-basis classification:
- implementations, source/managed runs, target build results, upstream-fetch
  confirmation, and the toolchain failure: **data/evidence change**;
- map anchoring, executable WorkBrief coordinates, deterministic chunk corpus,
  registry re-entrancy observer, and shared-pool integration tests:
  **verification method change**;
- managed exception/file-I/O/telemetry realizations: explicit correspondence
  decisions;
- policy, context, and authority: unchanged.

Status after batch:
- parser, animation, a11y foundation, policy loading, state registry, and
  grapheme ownership have bounded positive evidence;
- wrap completion, input-macro replay, widget accessibility integration, the
  45-screen differential plan, and the large closed-surface unexplained queue
  remain open;
- no preservation claim is autoaccepted; overall preservation remains
  **open / not accepted**.

### Batch 4 — Surface closure versus realization routes (2026-07-16)

Intent:
- close the current wrapping and widget-accessibility queues with explicit
  source-surface denominators;
- replace the render arena placeholder with a managed lifetime realization;
- test whether a tracked upstream source file is necessarily part of the
  compiled product before treating file coverage as public-surface coverage.

Basis and scope:
- source remains pinned to
  `15cc6543f76b814394c590f9e7719dedd6684e4c`, still the fetched
  `origin/main` coordinate recorded in Batch 3;
- the target is still an evolving attributed worktree. A new target basis and
  KodePorter map will be cut only after the active input-macro, hyphenation,
  and responsive-layout slices settle;
- this batch affects `kernel-render`, `runtime-layout-text`, and
  `widgets-extras`. It changes implementation and verification evidence, not
  policy, context, or delegated authority.

Implementation and verification observations:
- `wrap.rs` is now represented through all 41 public top-level and associated
  surface entries: `None`, `Word`, `Char`, `WordChar`, and `Optimal` modes;
  options; newline/indent/trailing-space rules; long-word fallback; Unicode
  grapheme truncation; and the Knuth-Plass objective seam. The upstream wrap
  selection passes 172/172; 119 managed cases cover the 155 declarations in
  `wrap.rs`, and 25 dependent layout/text cases pass. The managed text project
  and changed-file diagnostics are clean;
- all nine upstream widget `Accessible` implementers now emit the canonical
  a11y roles, bounds, states, values, names, descriptions, and parent/child
  relationships. The existing public widget DTO surface remains available as
  an explicit compatibility projection. Fourteen focused cases and 503
  related a11y/widget regressions pass. The direct widget source selection is
  still blocked before execution by the independently recorded Fenwick/
  toolchain failure, so no source-pass observation is invented;
- the frame arena now owns per-frame scratch strings, formatted values,
  slices, lists, and scalar boxes, with reset generations and retained/high-
  water accounting. The upstream arena suite passes 27/27 and the managed
  realization passes 15/15. CLR values cannot carry Rust borrow lifetimes, so
  this is a managed ownership correspondence rather than a claim of identical
  memory semantics. Runtime reset/attachment is still an open integration
  edge while `Program.cs` is owned by the input-macro slice;
- continuation render marks now have a thread-local managed stack, immutable
  snapshots, exact display labels, and disposable scope guards. Eleven
  managed cases pass and changed-file diagnostics are clean. A normal
  `cargo test -p ftui-render render_context` observation executes **zero**
  cases: current `lib.rs` does not declare `render_context.rs`. Compiling that
  self-contained source file directly with `rustc --test` executes its dormant
  suite and passes 10/10. The managed code is therefore a faithful realization
  of a tracked upstream organ file, but is not evidence of a currently routed
  Rust crate capability.

Challenges, divergences, and learning:
- a closed file inventory is not a closed executable surface. KodePorter needs
  both containment provenance (“this file exists at the basis”) and runtime/
  build-route evidence (“this module is reachable from the product”). The
  dormant render-context file is a concrete counterexample to same-stem or
  file-count completion metrics;
- source tests can have two distinct methods at the same evidence coordinate:
  Cargo observed zero applicable compiled tests, while direct module
  compilation observed ten passing dormant tests. These observations are not
  contradictory because their verification methods and consumed sets differ;
- wrap compatibility is bounded by the observed fixtures. Managed
  `StringInfo` and the checked-in width projection can differ from Rust's
  Unicode segmentation/width data outside that corpus; `WordSegments` remains
  a dependency-light segmentation rather than a claim of exhaustive UAX #29
  parity. Automatic dictionary hyphenation is correctly retained as a
  separate open unit;
- legacy widget nullable checked/expanded state projects to `false` through
  the old Boolean DTO properties, while new nullable companions preserve the
  canonical unknown state. That compatibility bridge is visible rather than
  silently changing the prior target API;
- managed arena references are GC-owned and may outlive reset if a caller
  retains them. Generational reset evidence protects the arena contract, but
  cannot reproduce Rust compile-time lifetime rejection.

Change-basis classification:
- implementation changes and all source/managed run results: **data/evidence
  change**;
- the direct-`rustc` dormant-module run and canonical/legacy accessibility
  projection tests: **verification method change**;
- arena lifetime, enum-default, string segmentation, and legacy a11y
  projection choices: explicit correspondence/divergence decisions;
- policy/definition, context/authority, and acceptance authority: unchanged.

Status after batch:
- wrapping, nine-widget accessibility, frame-arena storage, and dormant
  render-mark semantics have bounded positive evidence under their stated
  methods;
- frame-arena runtime attachment, automatic hyphenation, input macro,
  responsive layout, full text/render/runtime crates, and all 45 showcase
  frames remain open;
- a new target basis/map and settled aggregate run are deferred until active
  edits stop. No preservation claim is autoaccepted; overall preservation
  remains **open / not accepted**.

### Batch 5 — Deterministic operation, algorithm fingerprints, and explicit corrections (2026-07-16)

Intent:
- complete several bounded source modules whose observable contracts cross
  interface, result, effects, failure, determinism, realization, and
  operational dimensions;
- route the previously standalone frame arena through the real program render
  lifecycle;
- preserve exact source algorithms where the runtime permits it, while
  labeling managed adaptations and intentional corrections separately.

Basis and scope:
- source remains pinned to
  `15cc6543f76b814394c590f9e7719dedd6684e4c`, the fetched `origin/main`
  coordinate used by Batches 3 and 4;
- target repository HEAD remains `afd20f0`; all observations in this batch
  consume an attributed dirty-worktree coordinate rather than pretending that
  HEAD alone identifies the target under test;
- this batch affects `kernel-render`, `runtime-layout-text`,
  `widgets-extras`, and `verification-tooling`. Policy, context, and
  acceptance authority remain unchanged.

Implementation and verification observations:
- input-macro recording, timing, replay, serialization, and program-facing
  controls now cover the complete current source-shaped surface. Twenty
  focused managed cases pass. The subsequently added deterministic
  `ProgramSimulator` realizes three core types, two simulator-error variants,
  eleven command-record variants, and thirty operations, including exact
  virtual deadlines, recursive command stop rules, subscription delivery,
  state-registry flow, frame capture, logging, and one-shot shutdown. The
  combined simulator/macro selection passes 49/49, and the full headless run
  observed at that coordinate passes 4,730/4,730;
- the simulator source oracle command
  `cargo test -p ftui-runtime simulator --lib` is still blocked before its 50
  declared cases execute by the independently recorded pinned-toolchain
  failure in `ftui-widgets/fenwick.rs`. This is a failed verification method,
  not a failed simulator observation and not an automatically rejected
  preservation claim;
- hyphenation now covers all 22 public entries, the 121 built-in patterns, 11
  exceptions, Liang/TeX trie behavior, margins, case-insensitive matching,
  and wrap penalties. The source selection passes 37/37, the managed
  selection 40/40, and the combined wrap/hyphenation selection 159/159;
- responsive layout now covers the closed 110-entry denominator across
  direction, visibility, responsive selection, grid, and support vocabulary,
  while retaining the earlier target compatibility surface. Five focused
  source groups pass 110/110 under the explicitly different
  `nightly-2026-07-06` method; the pinned nightly cannot compile the unrelated
  Fenwick call. The managed exact-denominator selection passes 110/110 and 35
  existing layout regressions pass. No whole-crate pass is inferred from a
  timed-out whole-crate attempt;
- `RoaringBitmap` and `QuotientFilter` replace missing render algorithms. The
  former passes 16/16 source and 17/17 managed cases, including array-to-
  bitmap promotion and the full unsigned domain. The latter passes 23/23
  source and 32/32 managed cases. Its Rust `DefaultHasher`/SipHash-1-3 route is
  checked with cross-runtime known-answer fingerprints rather than only
  self-consistency tests;
- `SpatialHitIndex` now realizes the upstream uniform grid, replacement,
  update/remove compaction, z/order tie-breaks, dirty invalidation, caching,
  statistics, and edge grids. Source passes 72/72 and managed passes 24/24.
  The target deliberately normalizes a configured zero cell size to one: the
  source constructor computes a one-cell grid but retains zero in the stored
  configuration, leading routed registration/query code to divide by zero.
  The managed behavior follows the source tests and documented intent; it is
  recorded as an explicit correction, not concealed as exact identity;
- normalization covers all eight functions and all four Unicode
  normalization forms. Source with the `normalization` feature passes 55/55;
  managed passes 49/49, with 55/55 dependent search cases and 208/208 combined
  wrap/hyphenation/normalization cases. This exposed a separate normalized-
  search range-mapping gap now owned by the active search slice;
- fit metrics now cover fixed-point cell measurements, DPR/zoom viewports,
  automatic/fixed/minimum fit policy, overflow, saturating generations,
  invalidation ordering, lifecycle coalescing, and snapshots. Source passes
  57/57 and managed passes 34/34; solution-bound diagnostics report no errors
  or warnings in either changed file;
- the program now resets and attaches its owned `FrameArena` before every
  render. The focused program selection passes 84/84 and observes successive
  arena generations with empty pre-view allocation counts. This changes the
  arena from organ-level implementation evidence to an exercised runtime
  realization route.

Challenges, divergences, and boundaries:
- the managed terminal-event union still lacks upstream Tick, IME, and
  Clipboard variants. The deterministic simulator therefore requires explicit
  event and tick adapters only when those routes are exercised; message-only
  simulation remains adapter-free. No fictitious event variants were added to
  make structural counts look complete;
- target `Cmd<T>` lacks the source `SetTickStrategy` variant, which the source
  simulator records as a no-op. Its structural absence has no missing
  simulator effect under the observed contract. The target-only
  `SaveStateAndQuit` remains a labeled compatibility extension;
- `TimeSpan`/`int`, nullable managed errors, and CLR reference ownership are
  adaptations of Rust `Duration`/`usize`, `Result`, and borrow distinctions;
- the hyphenation corpus is the source proof-scale built-in corpus, not a
  production-language dictionary. Normalization follows the installed .NET
  Unicode database, materializes BCL normalization, rejects malformed UTF-16,
  and uses UTF-16 coordinates; these are explicit operational and
  representation boundaries;
- quotient-filter hashing is bit-matched for the source primitive/string
  domains and for types implementing the explicit hash contract. Arbitrary
  CLR objects use a deterministic type/text fallback and are not claimed to
  share Rust's generic `Hash` semantics;
- `HitRegionKind` cannot carry the source `Custom(u8)` payload and has
  target-only variants. Responsive associated-data enums are represented by
  record hierarchies, and `default(Visibility)` has the CLR all-zero behavior
  while constructed/default-factory values follow the source default.

KodePorter learning:
- algorithmic parity sometimes needs portable fingerprints. A differential
  oracle that publishes cross-runtime hash inputs and outputs supplies stronger
  independence than two target tests sharing the same implementation;
- an intentional correction is neither a known deviation nor silent
  preservation. It needs source behavior, documented intent, affected routes,
  and the chosen target decision visible together so a later upstream fix can
  supersede it cleanly;
- interface closure and effect closure can differ. An absent no-op command
  variant is a structural gap without a missing simulator effect, while a
  frame arena with matching methods was incomplete until the real program
  route reset and attached it;
- runtime-native discriminated unions should not be expanded merely to satisfy
  one verifier. Plan adapters are the conservative place to bridge represent-
  ation when identity and lifecycle do not justify a new domain concept;
- environment/toolchain selection belongs in the observation method. The
  110 responsive source passes under a newer nightly and the blocked pinned-
  nightly attempt remain separate replayable observations;
- module-sized closed denominators, typed divergence, source/target runs, and
  route evidence continue to compose from the existing KodePorter spine. This
  batch did not require another permanent ontology noun.

Change-basis classification:
- implementations, focused and aggregate runs, hash fingerprints, and arena
  lifecycle observations: **data/evidence change**;
- newer-nightly responsive runs, feature-enabled normalization, direct hash
  known answers, and adapter-dependent simulator routes: **verification method
  change**;
- zero-cell normalization, managed union/result/ownership mappings, and
  target compatibility extensions: explicit correspondence/divergence
  decisions;
- policy/definition, context/authority, and acceptance authority: unchanged.

Status after batch:
- the modules named above have bounded positive evidence under their stated
  denominators and methods; this does not imply crate, showcase, or whole-port
  preservation;
- full search, dependency graph/van-Emde-Boas layout support, the remaining
  render/runtime/text closed-surface queue, host parity, and all 45 showcase
  differential frames remain open;
- the 4,730-case aggregate is an observation at its consumed worktree cut. A
  new settled run and target KodePorter basis/map remain required after active
  slices stop. No preservation claim is autoaccepted; overall preservation
  remains **open / not accepted**.

### Batch 6 — Operational evidence, route-sensitive rendering, and bounded primitives (2026-07-16)

Intent:
- close another set of current-upstream render, layout, and text modules with
  explicit source denominators and independent source/managed observations;
- strengthen operational verification around allocation, emitted bytes, and
  per-frame limits without treating a passing threshold as a universal
  performance claim;
- follow newly implemented helpers through their real consumers so apparently
  local parity does not hide a routed behavior change.

Basis and scope:
- source remains pinned to
  `15cc6543f76b814394c590f9e7719dedd6684e4c`, fetched `origin/main` as of
  2026-07-16;
- target repository HEAD remains `afd20f0`; this batch consumes a sequence of
  attributable dirty-worktree cuts. The focused observations identify their
  files and filters, while the 4,950-case aggregate identifies the later
  frame-guardrail/drawing cut at which it was taken;
- this batch affects `kernel-render`, `runtime-layout-text`, and
  `verification-tooling`. It changes implementation, correspondence, and run
  evidence. It does not change the port policy, review context, standing
  authority, or overall acceptance decision.

Implementation and verification observations:
- allocation-budget evidence now covers online Welford moments, exponential
  moving averages, two-sided CUSUM, e-process alarms, JSONL persistence,
  reset, and non-finite inputs. The upstream selection passes 60/60; the
  managed module passes 38/38, and 44/44 pass with the existing runtime
  evidence bridge. A source-shaped render `EvidenceEntry` exposed a namespace
  collision with the runtime testimony type; the consumer now names its
  intended type explicitly rather than weakening either vocabulary;
- drawing now covers square, rounded, double, heavy, and ASCII borders;
  horizontal/vertical lines; filled and outlined rectangles; clipped and
  unclipped text; boxes; and area painting. Upstream passes 62/62 and the
  focused managed contract passes 41/41. The investigation also corrected
  `Buffer.SetFast` fallbacks for wide/continuation cells, nested scissor and
  opacity, and non-trivial background alpha. Drawing plus style-area routing
  passes 51/51;
- dependency-graph and van-Emde-Boas support now cover 51 applicable managed
  contracts, with 161/161 dependent layout cases passing. The corresponding
  upstream graph, vEB, property, and incremental-dependent selections pass
  84/84 under `nightly-2026-07-06`; this remains a method-qualified result
  because the pinned source nightly is blocked by the separately recorded
  Fenwick compiler incompatibility;
- search now realizes all four search types, both width variants, seven result
  fields, presets, and all seven top-level functions. Upstream with the
  normalization feature passes 66/66; managed search passes 70/70 and the
  combined text selection passes 284/284. Normalized matches are projected
  back to original UTF-16 ranges through grapheme-coordinate maps rather than
  returning offsets in the materialized normalized string;
- frame guardrails now realize the current closed denominator of thirteen
  domain types, twelve variants, twenty-six fields, forty-two operations and
  defaults, five clone paths, and `CELL_SIZE_BYTES`. Upstream passes 46/46;
  managed guardrails pass 20/20 and 122/122 collision consumers. The full
  headless project at the settled guardrail/drawing cut passes 4,950/4,950;
- link registration now covers ordinal deduplication, 24-bit IDs, LIFO slot
  reuse, unregister/clear/clone, lookup, safety validation, and memory
  estimation. Upstream passes 45/45 and managed passes 28/28. The prior target
  checked UTF-16 code units; the source limit is 4,096 UTF-8 bytes. The target
  now measures UTF-8, rejects controls, and explicitly rejects unpaired UTF-16
  surrogates that cannot represent a Rust `str`;
- counting-writer and presentation statistics replace a compressed skeleton
  with explicit write, flush, reset, inner access/transfer, byte ratios,
  conservative budgets, and monotonic timing contracts. Upstream passes
  41/41 and managed passes 28/28. The managed transfer invalidates the wrapper
  after `IntoInner`, successful synchronous stream writes are counted in
  bytes, signed compatibility overloads reject negative counts, and budget
  overflow is visible;
- all changed production and focused test files named above report zero
  solution-bound diagnostics. Their scoped `git diff --check` runs are clean
  apart from line-ending notices. No warning-free whole-repository claim is
  made: aggregate builds continue to expose known warnings in unrelated
  pre-existing files.

Challenges, divergences, and boundaries:
- Rust `f64::max` and managed `Math.Max` do not select the same result for
  every NaN ordering. The allocation port uses an explicit source-compatible
  maximum where the algorithm requires that behavior; generic BCL familiarity
  is not evidence of numeric equivalence;
- source-shaped `PaintArea` with full opacity directly assigns a translucent
  background, while the target compatibility style-area route historically
  composites translucent styles. These are now separate labeled overload
  paths. Collapsing them into one helper first produced an aggregate alpha
  regression (64 instead of 128), which the full headless run detected;
- Rust `usize`, ownership, and tagged enums map to managed `ulong` or bounded
  signed adapters, invalidated mutable wrappers, and record/class unions.
  Nullable cycle errors, explicit out-of-range handles, and allocation of
  tagged records are representation correspondences, not claims of identical
  memory layout;
- the link memory estimate is a managed heap estimate. Its register/get/
  reuse effects are preserved, but its numeric value cannot be equated with
  Rust `Vec`/hash-map capacity accounting across runtimes;
- search normalization follows the installed .NET Unicode implementation and
  returns UTF-16 coordinates. The differential corpus supports the observed
  result claim; it does not prove equality for future Unicode database
  versions or malformed managed strings;
- operational budget formulas are source-identical under representable
  counts, but `WithinBudget` is only one criterion. It is not evidence that
  terminal latency, allocation, concurrency, or security behavior is globally
  preserved.

KodePorter learning:
- a helper's local source tests are insufficient when another public route
  already depends on different compatibility semantics. The consumed-set cone
  should include routed callers, and a work brief should request both focused
  differential evidence and the smallest meaningful aggregate regression;
- representation limits need roles in a criterion: URL length is a source
  UTF-8-byte rule, while the target stores UTF-16. Recording only “maximum
  length 4,096” loses the unit and would have blessed a real mismatch;
- runtime names can collide without domain concepts colliding. Explicit
  namespace qualification is a code adaptation; it does not justify renaming
  the KodePorter evidence vocabulary or adding a second testimony noun;
- numeric algorithms need edge-coordinate fixtures, especially NaN,
  infinity, overflow, exact threshold boundaries, and normalized-to-original
  index maps. Ordinary happy-path examples cannot warrant these contracts;
- ownership differences are best expressed as correspondence plus observable
  lifecycle behavior. Invalidating a managed wrapper after transfer makes the
  source consumption rule testable without pretending CLR references have
  borrow-checker identity;
- a whole-project pass earned its value by falsifying a locally plausible
  opacity implementation. Aggregate runs should remain observations with
  precise worktree cuts, not be promoted into permanent “healthy” facts;
- these cases continue to compose from criterion dimensions, verification
  plans, observations, correspondences, divergences, and derived impact views.
  No additional permanent ontology noun was required.

Change-basis classification:
- implementations, focused tests, consumer tests, and the 4,950-case
  aggregate: **data/evidence change**;
- newer-nightly graph/vEB runs, feature-enabled normalized search, failure-
  edge fixtures, and aggregate consumer routing: **verification method
  change**;
- source/compatibility paint overloads, UTF-8/UTF-16 validation, numeric edge
  behavior, managed ownership transfer, and representation mappings:
  explicit correspondence/divergence decisions;
- policy/definition, context/authority, and acceptance authority: unchanged.

Status after batch:
- the modules named above have bounded positive evidence under their declared
  denominators, environments, and worktree cuts. The latest settled aggregate
  observation in this batch is 4,950/4,950, not a statement about later active
  edits;
- rope text storage, incremental layout orchestration, and headless-render
  projection are active attributed slices and are intentionally excluded from
  this batch until their evidence settles;
- remaining render/runtime/text closed surfaces, hosts, and all 45 showcase
  differential frames remain open. A new target basis/map, current aggregate,
  and showcase rerun are still required. No preservation claim is
  autoaccepted; overall preservation remains **open / not accepted**.

### Batch 7 — Reachable frames, coordinate truth, and compatibility projections (2026-07-16)

Intent:
- settle the incremental-layout, rope, and headless-render slices that Batch 6
  deliberately left active;
- close the current sanitizer, ANSI emitter, and frame metadata surfaces with
  source denominators and consumer-route evidence;
- preserve established managed conveniences as labeled projections while
  restoring source information that those conveniences had collapsed.

Basis and scope:
- source remains pinned to
  `15cc6543f76b814394c590f9e7719dedd6684e4c`, fetched `origin/main` as of
  2026-07-16;
- target repository HEAD remains `afd20f0`; the observations below consume
  attributable dirty-worktree cuts. Focused and dependent selections are
  reported separately so no later concurrent edit is silently included;
- this batch affects `kernel-render`, `runtime-layout-text`,
  `widgets-extras`, and `verification-tooling`. Policy, review context,
  authority, and overall acceptance criteria remain unchanged.

Implementation and verification observations:
- incremental layout now covers both public types, all 27 public operations,
  four public statistics fields, default/debug behavior, dependency wiring,
  hash-deduplicated invalidation, ancestor/sibling propagation, cached
  computation, bulk operations, and statistics. The current upstream inline,
  end-to-end, and golden denominator passes 75/75 under
  `nightly-2026-07-06`; managed passes 33/33, and the integrated incremental,
  graph, vEB, direction, visibility, responsive, and grid selection passes
  194/194. A cross-runtime FxHash known answer pins the five-way 200x60 split
  to `3939641721629212131`;
- the rope contract was extended in place in the existing TextArea internals
  rather than creating a duplicate text-storage noun. Upstream passes 52/52;
  managed passes 62/62, comprising all source behavior families plus ten
  explicit coordinate and validation adaptations. Rope, TextArea, search,
  normalization, hyphenation, and wrapping pass 403/403, with a final
  Rope/TextArea cut passing 125/125. LF, CRLF, CR, VT, FF, NEL, LS, and PS are
  preserved rather than normalized;
- source-shaped headless terminal emulation now covers three public types,
  seventeen operations/accessors, six data fields, display and clone behavior,
  byte processing, row/screen views, reset, assertions, structural diffs, and
  deterministic file/ANSI exports. Upstream passes 42/42; managed passes
  18/18, and the terminal-model/presenter/buffer-view consumer selection passes
  38/38. Existing `HeadlessBufferView` remains an explicit direct-buffer
  compatibility bridge;
- the headless route exposed and corrected terminal-model behavior for CSI
  movement/bounds, pending right-margin wrap, and zero-width scalar
  composition. In particular, pending wrap had overwritten a grapheme under
  real presenter output. Three whole-suite attempts under concurrent agent
  load failed only unrelated timing/performance cases, so they are retained as
  non-clean run observations; no clean aggregate is inferred from the stable
  38-case scoped result;
- sanitization now covers the free sanitizer, the two source trust variants,
  eight operations, string-reference/display/equality behavior, C0/DEL/C1,
  CSI/OSC/DCS/PM/APC, ESC and C1 ST, abort/truncation/nesting, Unicode, and an
  adversarial corpus. Upstream passes 123/123; managed passes 50/50 and 71/71
  with presenter/backend/terminal consumers. The existing sanitizer entry
  points remain, while one immutable `Text` value plus `TextTrust` realizes the
  source variant distinction without an inheritance hierarchy;
- ANSI generation now covers SGR on/off codes and Bold/Dim collateral,
  true/256/16/default colors, packed transparency, absolute and relative
  cursor movement, erase and scroll regions, synchronized output, OSC 8,
  alternate screen, paste, mouse hygiene, focus, and the full current constant
  set. Upstream passes 85/85; 61 managed ANSI cases and 48 presenter/terminal/
  headless consumers pass. The target retains its cursor-choice, text-
  sanitizer, and Kitty-keyboard helpers as compatibility extensions;
- ANSI consumer evidence corrected two routed differences: current source
  terminates OSC 8 with BEL rather than ST, and emits the default erase forms
  `CSI K`/`CSI J` rather than explicit `CSI 0 K`/`CSI 0 J`. The presenter
  expectation and terminal emulation now follow the current source bytes;
- the frame surface now covers source-shaped `Custom(u8)` hit payloads, hit
  cells/results/grids, per-cell queries and area enumeration, clone/clear/
  mutation, nested owner provenance, widget budgets and complete signals,
  cursor/degradation/arena/link metadata, interning, drawing, and frame
  lifecycle. Upstream passes 86/86; managed passes 36/36 and 910/910 related
  frame, widget, modal, grapheme, and runtime cases;
- frames again borrow an optional external link registry in domain terms: a
  plain frame returns link ID zero, `WithLinks`/`SetLinks` use the supplied
  registry, and the real program now supplies `TerminalWriter.Links` on every
  render. A runtime-route test observes a model registering a URL through its
  frame and then resolves the same ID through the persistent writer registry;
- solution-bound diagnostics are zero for the new/rewritten frame, hit,
  headless, rope, sanitizer, ANSI, and focused test files. `Program.cs` has zero
  errors and retains its pre-existing unused `_fairnessConfigLogged` warning;
  no warning-free whole-repository claim is made. Scoped diff hygiene is clean
  apart from line-ending notices.

Challenges, divergences, and boundaries:
- managed rope storage remains an immutable `string`, not Ropey's B-tree.
  Interface, Unicode coordinate, line, result, and failure behavior is
  warranted by the corpus; large-edit asymptotic complexity is not;
- Rust rope `char` means Unicode scalar, byte positions mean UTF-8 offsets,
  while the established TextArea surface also has UTF-16 and legacy byte-
  column expectations. Source operations are exposed as
  `LineWithTerminator` and `ByteToScalarLineCol`; the old terminator-stripped
  `Line` and UTF-8-byte-column `ByteToLineCol` remain visibly separate;
- headless captured bytes map to copy-on-access `ReadOnlyMemory<byte>`;
  borrowed string slices map to fresh managed arrays; assertion panics map to
  `InvalidOperationException`; invalid dimensions map to
  `ArgumentOutOfRangeException`. LF and BOM-free UTF-8 are fixed for replayable
  exports rather than inherited from the host platform;
- sanitizer parsing is necessarily UTF-16-code-unit based in managed code,
  while Rust input is valid UTF-8. Valid surrogate pairs survive and isolated
  surrogates are target-only rejected/stripped hardening. C1 ST occupies two
  UTF-8 bytes but one managed code unit; no unit-free length claim is made;
- the sanitizer's same-reference fast path approximates Rust `Cow::Borrowed`;
  the slow path allocates. CLR immutable strings cannot reproduce Rust borrow
  lifetimes, and `IntoOwned` produces an equivalent value rather than a
  compiler-enforced ownership transition;
- ANSI result bytes are exact for the ASCII control sequences, but the managed
  API returns strings/builders rather than generic `Write` sinks. Source zero-
  allocation stack-buffer performance is not claimed by result parity;
- the existing `HitRegionKind` had collapsed arbitrary `Custom(u8)` payloads
  into one enum value and several named modal conveniences. A parallel
  source-shaped `HitRegion` value restores the byte payload; hit cells/results
  retain both source and compatibility coordinates so existing widgets do not
  lose their named projection. This is one domain concept with two views, not
  two competing region truths;
- Rust optional mutable hit-cell borrowing maps to an attributable
  `TryMutate` operation. Managed hit-grid storage is array-indexed and rejects
  dimensions whose product cannot fit a CLR array index; no claim is made for
  Rust allocation behavior at impossible managed sizes;
- current upstream `IncrementalLayout.ResultChanged` returns true only before
  a cache entry exists, despite broader wording in its documentation. The
  target preserves observed source behavior and records the wording tension
  rather than silently implementing an inferred fix.

KodePorter learning:
- coordinate systems and units belong on criteria and evidence. “Line,”
  “column,” “length,” and “character” are not adequate without UTF-8 byte,
  UTF-16 code unit, scalar, grapheme, or display-cell roles;
- compatibility projections should be first-class in correspondence reports,
  not multiplied into ontology. Rope lines and hit regions show the same
  pattern: preserve the established consumer view, add the lost source
  coordinate, and test their mapping explicitly;
- runtime-route evidence can change the conclusion of a structurally complete
  helper. Optional frame links were not realized until `Program` supplied the
  writer-owned registry; the model-to-frame-to-presenter path is stronger
  evidence than a registry unit test;
- exact terminal control bytes are small, high-impact differential fixtures.
  A semantically accepted alternate terminator is still a result mismatch when
  the charter calls for current-source fidelity;
- aggregate failures need typed attribution. The concurrent full-suite timing
  failures neither refute headless parity nor become invisible; they remain
  failed run observations outside the stable consumed slice and motivate a
  later quiescent aggregate;
- operational equivalence is separable from functional equivalence. Rope
  storage and ANSI allocation are explicit open operational criteria even
  though their observed results pass;
- all of these cases compose from existing bases, correspondences, criteria,
  observations, route evidence, divergences, and views. No new permanent
  KodePorter ontology noun was needed.

Change-basis classification:
- implementations, focused/consumer runs, runtime-link route, and terminal-
  model corrections: **data/evidence change**;
- newer-nightly incremental oracle, known-answer hash, malformed UTF-16
  fixtures, exact ANSI byte comparison, and concurrent aggregate attempts:
  **verification method change**;
- rope coordinate names, headless ownership/error mappings, `TextTrust`, ANSI
  string emission, hit-region dual projection, and mutable-cell adaptation:
  explicit correspondence/divergence decisions;
- policy/definition, context/authority, and acceptance authority: unchanged.

Status after batch:
- incremental layout, rope functional behavior, headless terminal emulation,
  sanitization, ANSI results, and frame metadata/routing have bounded positive
  evidence under the stated methods and cuts;
- layout cache, bidi, and packed cell semantics are active attributed slices
  and are excluded until their evidence settles. Rope large-edit complexity
  and ANSI allocation behavior remain explicit operational unknowns;
- a quiescent whole-suite run, new target basis/map, current showcase
  differential, remaining source surfaces, and host completion remain open.
  No preservation claim is autoaccepted; overall preservation remains
  **open / not accepted**.

### Batch 8 — Packed truth, cache identity, and the executable terminal oracle (2026-07-16)

Intent:
- settle the layout-cache and packed-cell slices that Batch 7 deliberately
  excluded while they were active;
- replace the partial character-oriented terminal model with the current
  upstream byte-stream state machine, because this model is itself an oracle
  for presenter and headless verification;
- distinguish exact result evidence, compatibility projections, runtime
  routing, and operational representation limits rather than compressing them
  into one parity label.

Basis and scope:
- source remains pinned to
  `15cc6543f76b814394c590f9e7719dedd6684e4c`; the consulted source files are
  `ftui-layout/src/cache.rs`, `ftui-render/src/cell.rs`, and
  `ftui-render/src/terminal_model.rs`;
- source SHA-256 coordinates include cell
  `41CFEF735B64FC44DC6C9516B8DB2CC36459A0DABC407BB113D14B331CF69634`
  and terminal model
  `5C74171590A69E122C0764D753EDE661A7A6D2BB1210ED94B298DA8F8476F0FB`;
- target repository HEAD remains `afd20f0`; evidence consumes attributable
  dirty-worktree cuts and is therefore not silently promoted to a new target
  basis. This batch affects `kernel-render`, `runtime-layout-text`, and
  `verification-tooling`;
- cell and terminal source oracles ran with `rustc 1.96.0-nightly
  (3b1b0ef4d 2026-03-11)`. The cache oracle used the explicitly labeled newer
  `nightly-2026-07-06` toolchain because the workspace-pinned compiler cannot
  compile current layout dependencies. Policy, context, authority, and
  acceptance criteria are unchanged.

Implementation and verification observations:
- the layout cache now contains the complete canonical key, statistics,
  standard cache, S3-FIFO cache, coherence identifier, and coherence cache
  surfaces while retaining the existing simpler `LayoutCache` and key
  conveniences. An internal source-shaped S3-FIFO dependency projection and
  an actual stable-measurer route through `Flex`/`LayoutRounding` preserve the
  temporal solver behavior instead of leaving the cache as an isolated data
  structure;
- the cache source denominator passes 49/49: 43 inline cases, one ratio case,
  one coherence integration, and four routed temporal cases. Managed passes
  54/54 focused and 243/243 across the dependent layout selection. Exact
  SipHash-1-3 and `rustc_hash` 2.1 numeric fingerprints match direct Rust
  probes; the Layout project builds with zero warnings/errors and all four
  changed files have zero solution-bound diagnostics;
- packed cell behavior now closes the six-type source surface spanning
  `GraphemeId`, `CellContent`, `Cell`, `PackedRgba`, style flags, and cell
  attributes. The port corrected the 24-bit maximum link ID, masked grapheme
  width before packing, made invalid raw Unicode scalars return typed absence,
  and changed opacity midpoint handling to Rust's half-away-from-zero rule;
- the exact cell source denominator passes 145/145. Managed focused evidence
  passes 33/33; the buffer, presenter, frame, grapheme-pool, and cell selection
  passes 111/111, and the pre-existing terminal/backend compatibility
  selection passes 12/12. Two exhaustive fingerprints pin 65,536 source-over
  alpha pairs (`0xB98ABE8BBEC3AB0E`) and 2,304 alpha/opacity combinations
  (`0x09B48A48060C846D`), in addition to literal edge outputs;
- managed layout evidence proves 4-byte packed words, one-byte flags, a
  16-byte `Cell`, 16-byte array stride, and source word ordering. All seven
  cell production/test files have zero solution-bound diagnostics and clean
  scoped diff hygiene;
- the terminal model is now a streaming byte-state parser with persistent
  incomplete UTF-8, Ground/Escape/CSI/OSC states, saturating CSI parameters,
  C0 movement, Unicode width, combining and wide-cell behavior, immediate
  edge wrapping, CUP/relative/absolute motion, all ED/EL modes, the complete
  current SGR flag and basic/bright/256/RGB palettes, DEC 25/1049/2026 modes,
  OSC 8 identity/lookup, reset, row/cell/current views, grid differences, and
  readable sequence dumps;
- the terminal source denominator passes 103/103. Managed terminal cases pass
  77/77, and terminal, headless, presenter, and diff consumers pass 111/111 at
  the settled cut. Both production and parity-test files have zero
  solution-bound diagnostics; the Render project has zero errors and retains
  one unrelated pre-existing `Budget.cs` unused-field warning;
- source-shaped terminal absence is exposed by nullable cell/row and
  `TryRowText`; the established `RowText` empty-string projection,
  `ScreenString`, `CursorColumn`/`CursorRow`, `SyncOutputDepth`, and string
  processing adapter remain available. `ESC 7/8` save/restore also remains a
  tested compatibility extension even though current upstream only accepts
  and ignores those operations;
- direct inspection and the 103-case source oracle corrected the edge-wrap
  interpretation used by the earlier partial managed terminal model. Current
  upstream advances and wraps immediately after writing the edge cell. Batch
  7's pending-wrap observation remains true of that earlier attributable
  target cut and remains replayable; it is not current source testimony and
  is superseded for the present port view.

Challenges, divergences, and boundaries:
- Rust `usize` cache sizes and terminal dimensions map to nonnegative managed
  `int` cache capacities and terminal-sized `ushort` coordinates. Defensive
  array copies replace borrowed slices. The existing managed `Direction`
  collision maps the cache key's source axis to the explicitly named
  `AxisDirection` projection;
- the S3-FIFO dependency is specialized internally rather than exposed as a
  general-purpose permanent domain noun. What matters to the port record is
  the cache behavior, identity, and routed effect; the dense queue state and
  hash tables remain regenerable implementation material;
- CLR does not guarantee Rust's `repr(align(16))`. Cell size, field order, and
  stride are exact, and consumers are directed to unaligned SIMD loads, but
  address alignment is not claimed. Likewise CLR `default(Cell)` is forced to
  all zero bits; `new Cell()`/`Cell.Empty` are the faithful opaque-white Rust
  default and grid stores must take that route;
- Rust `char` is a Unicode scalar while C# `char` is a UTF-16 code unit.
  `Rune` is the faithful cell/terminal path. Isolated surrogate input is
  target-only malformed input, and arbitrary invalid packed scalar values
  return null instead of throwing;
- source debug traits map to managed `ToString`; packed equality and numeric
  results are warranted, but prior compiler-generated record text is not
  treated as a compatibility promise;
- the terminal model's source `usize` grid is projected to the managed
  terminal coordinate domain, and its source top-level `ModelCell` is retained
  as the established nested managed value. Managed immutable strings replace
  Rust-owned strings without changing observed text. No claim is made for
  allocation equivalence;
- the terminal oracle validates the subset of terminal behavior emitted by
  FrankenTUI. Passing it does not imply conformance to a complete VT emulator,
  scrollback implementation, or host-terminal timing model.

KodePorter learning:
- executable test helpers may be verification infrastructure and product
  surface simultaneously. The terminal model needs its own denominator,
  source basis, and divergences before its output can warrant presenter
  preservation claims;
- changed inference must name the consumed evidence. The wrap conclusion
  changed because a fuller source implementation and direct oracle replaced a
  partial target interpretation, not because policy or authority changed.
  Preserving both labeled observations makes that revision explainable;
- bit packing benefits from compact, cross-runtime known answers. Exhaustive
  fingerprints give dense result and determinism evidence without turning all
  67,840 rows into durable ontology objects; the corpus and rows remain organ
  data and the labeled result remains a view input;
- language defaults are behavior. `default(Cell)` versus `new Cell()` and
  `ModeFlags::default()` versus `ModeFlags::new()` are distinct realization
  routes that a surface-only correspondence would miss;
- cache equivalence is multidimensional: public availability, exact identity
  hashes, temporal admission/eviction results, routed solver effects, and
  memory/ownership adaptations need separate criteria. A single “cache
  ported” status would conceal both strong evidence and remaining operational
  differences;
- preserving a simpler managed projection while adding typed absence or a
  faithful `Rune`/source route is a correspondence decision, not a reason to
  mint duplicate domain concepts. No new permanent KodePorter noun was needed.

Change-basis classification:
- implementations, focused/consumer runs, exhaustive numeric fingerprints,
  and stable-measurer routing: **data/evidence change**;
- newer-nightly cache execution, direct terminal byte-fragment tests, packed
  layout/fingerprint probes, and the fuller source-oracle comparison:
  **verification method change**;
- immediate-wrap correction: changed inference from newly consumed source
  evidence, explicitly superseding the prior target-cut interpretation;
- coordinate widths, defensive ownership, nested cell naming, CLR alignment/
  default behavior, source-shaped typed absence, and retained ESC save/restore:
  explicit correspondence/divergence decisions;
- policy/definition, context/authority, and acceptance authority: unchanged.

Status after batch:
- layout-cache functional/identity behavior, packed-cell semantics, and the
  terminal executable oracle have bounded positive evidence under their
  declared denominators, environments, and worktree cuts;
- bidi remains an active attributed slice and is excluded from this settled
  batch. E-graph layout and render diff-strategy work have entered the derived
  queue after this cut;
- CLR cell address alignment, allocation equivalence, a quiescent whole-suite
  run, a new target basis/map, the current 45-screen showcase differential,
  remaining source surfaces, and host completion remain open. No preservation
  claim is autoaccepted; overall preservation remains **open / not accepted**.

### Batch 9 — Text policy composition, bidi coordinates, and opt-in algorithms (2026-07-16)

Intent:
- settle the bidi slice that Batch 8 left active and close its coordinate,
  conformance, and provenance evidence;
- build the vertical metrics, horizontal justification, and user-facing layout
  policy in dependency order so declared quality intent and realized runtime
  tier remain separately inspectable;
- close current e-graph and diff-strategy helper surfaces without claiming a
  production route where upstream itself has none.

Basis and scope:
- source remains pinned to
  `15cc6543f76b814394c590f9e7719dedd6684e4c`; consulted source files are
  `ftui-text/src/bidi.rs`, `vertical_metrics.rs`, `justification.rs`, and
  `layout_policy.rs`, `ftui-layout/src/egraph.rs`, and
  `ftui-render/src/diff_strategy.rs`;
- target HEAD remains `afd20f0`; all results consume attributable dirty-
  worktree cuts rather than asserting a new durable target basis. This batch
  affects `kernel-render`, `runtime-layout-text`, and
  `verification-tooling`;
- the standard source toolchain executes bidi, text-policy, and render
  oracles. E-graph uses the already declared
  `nightly-2026-07-06` method because current layout dependencies do not
  compile with the older installed workspace nightly;
- policy definitions, review context, authority, and overall acceptance
  remain unchanged.

Implementation and verification observations:
- bidi now covers the complete current public surface: levels, logical runs,
  resolved segments, base direction, levels, reordering, scalar/logical and
  visual maps, and source L2 behavior. Upstream feature-enabled tests pass
  45/45; managed passes 51/51, and bidi with ClusterMap, segmentation, shaping
  projection, and text rendering passes 86/86;
- the underlying resolver passes 91,707/91,707 cases from the pinned UAX #9
  `BidiCharacterTest-14.0.0` corpus. A separate eight-row cross-runtime
  differential pins representative mixed Latin, Hebrew, Arabic, numbers,
  controls, isolates, brackets, and neutral behavior. The Text project and
  all three bidi production/test files have zero warnings/errors and zero
  solution-bound diagnostics;
- direct probes corrected a misleading source comment: explicit directional
  controls remain represented by the current source result rather than being
  removed. The target follows observed source behavior and records the
  comment tension instead of treating prose as stronger testimony than code
  plus execution;
- vertical metrics now close leading variants, paragraph margins, baseline
  grids, compact/readable/typographic policy resolution, paragraph/line/
  document heights, fixed-point rounding, cell-row conversion, saturation,
  display, defaults, and determinism. The inline source denominator passes
  40/40, the full 34-case source layout/shaping property integration passes
  (four declarations directly exercise this module), and managed passes
  53/53;
- justification now closes alignment and last-line modes, three space
  categories, natural/stretch/shrink glue, presets, fixed-point adjustment,
  penalties, ratios, TeX cubic badness, line demerits, warnings, display,
  defaults, and saturation. Upstream passes 64/64 and managed passes 66/66;
- layout policy composes the already closed `ParagraphObjective`, vertical
  metrics, and justification configurations. It preserves the four ordered
  tiers, degradation chains, runtime capability presets, overrides, line
  height, typed insufficient-capability failure, requested/effective tiers,
  feature summaries, and deterministic equality. Upstream passes 47/47;
  managed passes 43 grouped cases covering every source behavior family;
- the source `snapshot_justified_matrix` passes 12/12 and the full source
  layout/shaping property integration passes 34/34. The managed policy,
  vertical, justification, and existing wrap selection passes 281/281. These
  are positive composition results, not yet a claim that shaping fallback or
  every snapshot renderer route has been ported;
- e-graph now covers all twelve expression variants, IDs, hash-consing,
  union-find, extraction, saturation, guards, evidence, constraint and flex
  encoders, both solve paths, and memory estimates. Source passes 64/64 inline
  plus 1/1 doctest; managed passes 65/65 and the dependent layout aggregate
  passes 308/308. The Layout project and both e-graph files have zero
  warnings/errors and zero solution-bound diagnostics;
- upstream has benchmark consumers for e-graph but no production `src` route.
  The managed e-graph therefore remains opt-in rather than being wired into
  `Flex` on speculation. This is a faithful realization conclusion, not an
  implementation omission hidden as parity;
- diff strategy now closes configuration/default/sanitization, estimator
  updates and quantiles, three strategies, tie and uncertainty rules,
  hysteresis, evidence, JSONL/display, lifecycle, clones, and accessors. It
  corrects the stale target cost model to current source's probability-
  weighted scanned/dirty regions, including the concentrated half-screen
  regression;
- the diff-strategy source denominator passes 107/107. Managed passes 34/34
  grouped parity cases and 24/24 BufferDiff, presenter, headless, gauntlet, and
  established selector consumers. Both files have zero solution-bound
  diagnostics and clean scoped hygiene;
- separately adapted SixLabors.Fonts and `unicode-bidi` material now has a
  repository-level third-party notice and a reproduced Apache-2.0 license in
  addition to per-file SPDX/source coordinates. This is attributable
  provenance evidence, not legal-compliance certification.

Challenges, divergences, and boundaries:
- source bidi segment maps are Unicode-scalar coordinates, while
  `Bidi.ResolveLevels` intentionally returns one level per UTF-8 byte because
  that is the current `unicode-bidi` route. Managed string lengths remain
  UTF-16 code units. Each coordinate is named and tested; no unqualified
  “character index” equivalence is claimed;
- malformed UTF-16 is rejected as target-only input. Current L2 reordering is
  preserved; L3 combining-mark reposition and L4 glyph mirroring/shaping are
  outside this source helper and remain later-route concerns;
- the UAX #9 engine is adapted from SixLabors.Fonts v1.0.0 commit
  `32bef42997adb10268369ca149777f00e4241ce9` and property/bracket data from
  `unicode-bidi` 0.3.18, checksum
  `5c1cb5db39152898a79168971543b1cb5020dff7fe43c8dc468b0885f5e29df5`.
  No runtime package dependency was added;
- Rust enum variants with payloads map to small tagged managed values. Several
  default-sensitive value types deliberately encode their zero representation
  so `default(LayoutTier)` and `default(LayoutPolicy)` preserve Rust's
  Balanced defaults. For glue/control structs, `new T()` is the faithful Rust
  default while unavoidable CLR `default(T)` remains all-zero and is tested as
  a representation distinction;
- Rust `Result<ResolvedPolicy, PolicyError>` maps to an explicit
  `PolicyResolution` carrying either the resolved value or typed error. This
  keeps failure testimony inspectable without making exceptions the only
  channel; `Unwrap` is a managed convenience with the source explanation;
- fixed-point horizontal units are 1/256 cell and vertical units are 1/256
  pixel. They are separate criteria despite sharing the integer scale. Rust
  `usize` line counts/capacities map to `nuint` or nonnegative managed limits;
- e-graph associated-data variants map to a closed record hierarchy and
  vector fields use defensive arrays with structural equality. Its memory
  estimate follows the source logical layout/capacity model and is explicitly
  not CLR heap telemetry;
- diff-strategy established `int` inputs retain managed-only negative-value
  normalization and saturating legacy evidence fields. Internal products use
  wider arithmetic. The target-only `SignificantDirtyRows` runtime regime is
  preserved but is never invented by the source Bayesian selector;
- source inline and matrix/property evidence do not yet warrant full shaping,
  glyph mirroring, proportional-font rendering, or performance equivalence.
  Those remain separate realization and operational criteria.

KodePorter learning:
- a policy object should preserve declared intent, effective realization, the
  reason for degradation, and the capability context. Flattening Quality→
  Balanced into one status would erase both user intent and runtime truth;
- typed result values make failed resolution an observation, not a rejected
  claim. A caller or reviewer can apply standing policy to the same error
  without rerunning layout code;
- source comments, executable code, and differential output can disagree.
  The explicit-control bidi correction demonstrates why evidence roles and
  survival must remain distinct rather than being collapsed into “confidence”;
- large conformance corpora and matrix rows belong in organ stores. Durable
  records need their basis, method, denominator, hashes, and result—not 91,707
  permanent ontology instances;
- runtime-route absence is positive realization evidence when the inspected
  surface is closed. “Implemented and opt-in because upstream is opt-in” is a
  stronger, more honest conclusion than either “unused” or speculative wiring;
- unit vocabulary matters: subcell, subpixel, UTF-8 byte, UTF-16 unit,
  Unicode scalar, grapheme, and display cell must stay explicit in work briefs,
  criteria, reports, and visualizations;
- third-party provenance is evidence about implementation origin and method.
  It belongs with source coordinates and report explanation without being
  promoted into a new port-domain ontology noun.

Change-basis classification:
- implementations, direct oracles, conformance corpus, grouped managed cases,
  dependent aggregates, and diff cost correction: **data/evidence change**;
- feature-enabled bidi, UAX corpus, newer-nightly e-graph, source matrix/
  property suites, and cross-runtime coordinate probes:
  **verification method change**;
- explicit-control interpretation: changed inference from direct code/probe
  evidence, not a policy or authority change;
- scalar/byte/UTF-16 coordinates, tagged variants, default encodings,
  `PolicyResolution`, defensive vectors, opt-in e-graph route, and managed diff
  numeric boundaries: explicit correspondence/divergence decisions;
- third-party notice/license addition: provenance/accountability metadata
  change, not behavioral acceptance;
- policy/definition, context/authority, and acceptance authority: unchanged.

Status after batch:
- bidi resolution, vertical metrics, justification arithmetic, policy
  resolution, e-graph opt-in behavior, and diff-strategy selection have bounded
  positive evidence under their declared methods and cuts;
- text view, layout workspace, and render diff execution are active attributed
  slices and excluded until their evidence settles;
- shaping fallback/glyph routes, remaining text/layout/render surfaces, hosts,
  a quiescent whole-suite run, new target basis/map, and all 45 showcase
  differentials remain open. No preservation claim is autoaccepted; overall
  preservation remains **open / not accepted**.

## Report Update Rules

For every later batch, append:

- source and target basis coordinates;
- changed migration units and upstream files consulted;
- observation commands and receipts;
- whether the conclusion changed because of evidence/data, policy/definition,
  context/authority, or verification method;
- challenges and unexpected dependencies;
- explicit divergences or decisions;
- new learning for KodePorter tooling or vocabulary;
- the recomputed current conclusion and next derived queue.

Never rewrite an earlier observation merely because later evidence changes the
answer.
