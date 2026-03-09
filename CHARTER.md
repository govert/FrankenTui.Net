# CHARTER

## Project Name

`FrankenTui.Net`

`FrankenTui.Net` is a traceable, updateable, in-sync .NET port of the Rust-based [FrankenTUI](https://github.com/Dicklesworthstone/frankentui) project.

The name is intentionally direct. It prioritizes lineage, searchability, and long-term port maintenance over novelty. The implementation may later split into multiple packages, but the project identity should stay obviously tied to FrankenTUI.

## Mission

Build a .NET 10-only, cross-platform port of FrankenTUI that reproduces FrankenTUI behavior as faithfully as practical, with special emphasis on:

- Bit-identical rendered output where terminal capabilities and encoding make that meaningful
- Console movement-identical behavior, including cursor motion and screen updates
- A traceable relationship to upstream FrankenTUI so the port can be audited, updated, and kept in sync over time
- A dependency-light design suitable for NativeAOT
- Full planned coverage of the FrankenTUI library, supporting tools, testing and verification surface, and web/wasm host surfaces that materially define the project in practice

This project exists to port FrankenTUI itself, including the supporting tooling, verification, and host surfaces that are part of its practical repository scope, not to design a new general-purpose .NET terminal UI framework.

## Vision

The first release should feel like a direct systems port of FrankenTUI into idiomatic-enough .NET, not a reinterpretation.

The core library should preserve the rendering model, terminal discipline, and behavioral constraints of FrankenTUI. A future .NET-facing integration layer may adapt that core to common .NET hosting conventions such as dependency injection, logging abstractions, and application scaffolding, but that is explicitly secondary to port fidelity.

The core remains the architectural center, but the repository should ultimately cover the surrounding verification, demo, doctor, and web/wasm surfaces needed to keep the port honest, usable, and updateable.

The project will initially track upstream FrankenTUI `main` rather than pinning to a fixed tag or commit. That choice is intended to maximize sync pressure early, while the port structure is still being established.

## Guiding Principles

1. **Fidelity over reinvention**
   The default decision is to match FrankenTUI semantics, output, and trade-offs unless there is a strong reason not to.

2. **Traceability over convenience**
   Ported code and behavior should be attributable to upstream concepts, modules, and commits where practical.

3. **Determinism over magic**
   Rendering, diffing, terminal writes, teardown, and state transitions should be explicit and testable.

4. **Small surface area over broad integration**
   The initial core should remain narrow, predictable, and dependency-light.

5. **NativeAOT friendliness as a standing constraint**
   Avoid reflection-heavy patterns, runtime code generation, and unnecessary framework coupling.

6. **Cross-platform correctness over platform-specific embellishment**
   Windows Terminal, macOS terminals, and Linux terminals must all be first-class targets.

## Scope

### In Scope

- A .NET 10 implementation of the FrankenTUI core concepts and behavior
- A coherent public facade and package surface for the .NET port
- Cross-platform terminal support on Windows, macOS, and Linux
- Terminal host support aligned with FrankenTUI’s practical support model rather than broadened independently by the .NET port
- Web/wasm host surfaces corresponding to the upstream `ftui-web` and `ftui-showcase-wasm` scope
- Rendering and diff/update behavior intended to match FrankenTUI as closely as feasible
- Terminal lifecycle correctness, including setup, mode transitions, cleanup, and restoration
- Supporting tools corresponding to the practical `doctor_frankentui` surface, adapted as needed for the .NET port context
- Verification tooling, parity corpora, replay/evidence assets, snapshot/golden coverage, PTY/integration harnesses, and benchmark/regression gates
- Demo/showcase surfaces used to exercise and verify the port
- A codebase structure that makes upstream comparison and future syncing practical
- A NativeAOT-buildable path, if feasible without violating the core fidelity goals

### Out of Scope

- Broad API compatibility with existing .NET TUI libraries
- Strict API compatibility with the Rust FrankenTUI crate
- Immediate first-class integration with dependency injection containers, logging providers, or generic host builders
- Building a large widget ecosystem before the core port is trustworthy
- Supporting older .NET runtimes
- Supporting non-VT legacy terminal behavior as a primary target
- Mirroring upstream website/content repositories or copying nonessential upstream assets into this repository when upstream references are sufficient

## Non-Goals

- We are not trying to create the most idiomatic .NET API on day one.
- We are not trying to abstract away every terminal quirk behind a large compatibility layer.
- We are not trying to optimize for drop-in replacement of other .NET UI libraries.
- We are not trying to chase a broad feature set ahead of behavioral equivalence.
- We are not trying to mechanically fork or mirror every upstream repository artifact when a traced port plus upstream references is the better representation.

## Primary Requirements

### Behavioral Compatibility

- The project should aim for bit-identical output streams wherever the comparison is meaningful and stable.
- The project should aim for console movement-identical behavior, including cursor visibility changes, cursor movement, line clearing, region updates, and teardown behavior.
- Differences from FrankenTUI must be documented rather than silently accepted.

### Upstream Traceability

- The port should preserve a clear mapping between .NET components and upstream FrankenTUI concepts.
- Where practical, files, types, tests, or comments should reference the upstream source module or commit used as the basis for the port.
- The project should maintain both strict provenance links at the implementation level and a higher-level narrative trace in documentation.
- Strict provenance is current policy, not doctrine. It should be pursued aggressively early on because it helps the port stay honest, but it may be relaxed selectively when direct lineage stops being the most maintainable representation.
- Port changes should be reviewable in terms of both .NET quality and upstream fidelity.

### Syncability

- The structure of the port should make future upstream updates cheaper, not harder.
- Large .NET-specific reinterpretations of upstream logic should be avoided unless clearly justified.
- When the .NET port intentionally diverges, the divergence should be explicit, local, and documented.

### Minimal Dependencies

- External dependencies should be kept to the smallest practical set.
- Standard library and BCL solutions are preferred when they do not materially harm fidelity, maintainability, or performance.
- Dependencies that undermine NativeAOT, increase startup cost, or obscure low-level behavior should be treated skeptically.

### NativeAOT Support

- The project should be designed to support NativeAOT builds.
- If full NativeAOT support is not possible initially, the blockers must be documented and actively designed around.
- Core architecture decisions should assume NativeAOT matters even before it is fully validated.

### Verification And Evidence

- The project should build its own verification stack, including headless, PTY, replay, benchmark, and host-specific checks where appropriate.
- Verification should begin at the earliest layer that can be tested rather than waiting for upper layers such as widgets or demos.
- Benchmarks, traces, snapshots, parity corpora, and evidence artifacts should be treated as core engineering assets.

### Tooling And Host Surface Coverage

- Supporting tools that materially participate in capture, diagnostics, replay, parity evaluation, or maintainer workflows are in scope.
- Web/wasm host surfaces that materially define upstream FrankenTUI behavior are in scope.
- Tooling and host surfaces should adapt the shared core rather than becoming alternate implementations with separate semantics.

## Architecture Direction

### Core First

The first-class deliverable is a low-level core library that mirrors FrankenTUI’s behavior and internal model closely enough to stay maintainable as a port.

This core should:

- Own terminal state and writes directly
- Centralize rendering and diff application
- Avoid incidental dependency on host frameworks
- Prefer explicit state machines and data flow over ambient services

### Verification And Tooling As First-Class Surfaces

The repository should also carry the verification and tooling surfaces needed to
prove, inspect, and maintain parity with upstream FrankenTUI.

These surfaces should:

- Exercise the same core rather than reimplementing it
- Produce durable evidence for parity, divergence, and regression analysis
- Stay aligned with the core package structure and provenance model

### Additional Host Surfaces

Terminal hosts are not the only relevant runtime surfaces. Web/wasm host
surfaces that are part of FrankenTUI’s practical repository scope should be
ported as first-class consumers of the same core behavior.

### Future Shim Layer

A later companion layer may expose a more .NET-native integration story for:

- Dependency injection / IoC containers
- Logging abstractions
- Hosting and application startup scaffolding
- Diagnostics hooks and configuration patterns expected by .NET applications

That shim must adapt the core without distorting it. The existence of a future shim is a design constraint now, but not current scope.

## Quality Bar

The project should be judged first by output fidelity and terminal correctness, then by API elegance.

The quality bar includes:

- Reproducible render output
- Predictable terminal cleanup and restoration
- Test coverage around terminal deltas and cursor motion behavior
- Cross-platform verification in supported terminals
- Credible parity evidence across headless, PTY, replay, benchmark, and host-specific verification paths
- Web/wasm and tooling surfaces that stay aligned with the same core semantics
- Clear documentation of known divergences

## Trade-Offs

### Decisions We Intentionally Favor

- Port fidelity over idiomatic .NET design purity
- Stable internal correspondence with FrankenTUI over maximal refactoring freedom
- Simplicity and low dependency count over feature accretion
- Explicit low-level control over abstraction-heavy architecture

### Decisions We Intentionally Do Not Favor

- Designing public APIs primarily to look like familiar .NET libraries
- Hiding upstream behavior differences behind convenient but lossy wrappers
- Accepting behavioral drift in exchange for faster early progress
- Adopting dependencies just to save a modest amount of implementation effort

## Success Criteria

The project is succeeding when:

- A maintainer can point to the upstream FrankenTUI basis for major parts of the implementation
- Rendered terminal behavior matches FrankenTUI closely enough to catch meaningful diffs in automated tests
- The core remains understandable as a port rather than an unrelated rewrite
- Verification, demo, web/wasm, and tooling surfaces exercise the same ported core rather than drifting into alternate implementations
- NativeAOT remains viable as the project grows
- Future upstream syncing work is incremental instead of traumatic

## Governance for Divergence

When the .NET port diverges from FrankenTUI, the burden of proof is on the divergence.

Acceptable reasons include:

- .NET runtime constraints
- NativeAOT constraints
- Platform-specific terminal behavior that cannot be reconciled cleanly
- Clear correctness or maintainability problems in a direct transliteration

Every meaningful divergence should be:

- Intentional
- Documented
- Tested
- Kept as narrow as possible

## Early Repository Conventions

- Treat upstream FrankenTUI `main` as the initial tracking target, and record the upstream commit basis for each significant porting wave.
- Prefer naming and module decomposition that preserves recognizability from upstream.
- Maintain strict provenance where practical in files, tests, and port notes, while also keeping higher-level sync and divergence documentation current.
- Keep generated code, source generators, and reflection-based helpers out of the core unless there is a compelling need.
- Treat benchmarks and output snapshots as core engineering assets, not optional polish.
- Treat harnesses, replay traces, doctor tooling, and showcase runners as core repository assets, not afterthoughts.

## Open Questions

These should be resolved early, but they do not block starting the project:

1. What concrete repository mechanism should carry strict provenance: comments in code, per-module port notes, commit-message discipline, or a dedicated mapping document?
2. At what point should the project stop treating strict provenance as the default for new work and treat it instead as one input among maintainability concerns?
3. What exact definition of “end of the first porting pass” should trigger upstream conformance testing?
4. Which FrankenTUI terminal support assumptions need to be copied verbatim into project documentation so the .NET port does not accidentally over-promise?
5. Which parts of the upstream `doctor_frankentui` surface should be direct ports versus .NET-native adaptations over equivalent responsibilities?
6. What is the exact boundary between in-scope web/wasm host behavior and reference-only external website or remote-protocol assets?
