# Docs

This directory contains tracked project documentation that supplements the
repository root documents.

## Index

- [100-PLN-planning-doctrine.md](./100-PLN-planning-doctrine.md): planning
  rules, code scheme, and hierarchy conventions
- [110-UPR-frankentui-roadmap.md](./110-UPR-frankentui-roadmap.md): upstream
  FrankenTUI workspace roadmap and port relevance map
- [200-PRT-port-work-breakdown.md](./200-PRT-port-work-breakdown.md): initial
  dependency-based hierarchical port plan
- [210-STS-port-status.md](./210-STS-port-status.md): canonical status ledger
  for the hierarchical port plan
- [220-PTH-pathfinder-baseline.md](./220-PTH-pathfinder-baseline.md):
  upstream baseline, parity corpus, and early verification doctrine
- [230-ARC-solution-skeleton.md](./230-ARC-solution-skeleton.md): .NET
  solution shape, project grouping, and artifact layout
- [240-MAP-module-mapping-ledger.md](./240-MAP-module-mapping-ledger.md):
  upstream crate-to-project mapping ledger
- [242-MAP-upstream-sync-workflow.md](./242-MAP-upstream-sync-workflow.md):
  upstream basis recording and `.external` refresh/reconciliation workflow
- [244-MAP-divergence-ledgers.md](./244-MAP-divergence-ledgers.md):
  maintained index of active and historical divergence ledgers across host,
  runtime, and web
  surfaces
- [245-MAP-divergence-triage-policy.md](./245-MAP-divergence-triage-policy.md):
  classification and escalation rules for new parity differences and bug triage
- [246-MAP-upstream-contract-gap-register.md](./246-MAP-upstream-contract-gap-register.md):
  maintained list of upstream contracts not yet fully ported into
  FrankenTui.Net
- [247-MAP-telemetry-mermaid-opentui-contract-wave.md](./247-MAP-telemetry-mermaid-opentui-contract-wave.md):
  current local baseline and remaining partial depth for the telemetry,
  Mermaid, and OpenTUI contract wave
- [248-MAP-runtime-input-and-proof-wave.md](./248-MAP-runtime-input-and-proof-wave.md):
  shared runtime-input closure, stronger routed-output/sanitizer proof, wider
  sample comparison, and OpenTUI planner integration
- [249-MAP-operator-runtime-depth-wave.md](./249-MAP-operator-runtime-depth-wave.md):
  runtime-driven operator-surface closure for command palette, log search,
  macro playback, and HUD stats
- [250-MAP-final-contract-closure-wave.md](./250-MAP-final-contract-closure-wave.md):
  final local upstream-contract closure wave across backend proof, shared
  samples, telemetry, pane state, and Mermaid
- [294-TXT-shaping-and-hyphenation-evaluation.md](./294-TXT-shaping-and-hyphenation-evaluation.md):
  current .NET text-shaping and hyphenation decision under AOT constraints
- [304-RTM-determinism-and-evidence.md](./304-RTM-determinism-and-evidence.md):
  runtime determinism, evidence-manifest, DOM-runner, and benchmark-gate
  baseline
- [335-HST-host-divergence-ledger.md](./335-HST-host-divergence-ledger.md):
  per-platform host validation status, evidence sources, and divergence notes
- [336-HST-inline-mode-divergence-ledger.md](./336-HST-inline-mode-divergence-ledger.md):
  historical record of the closed inline-mode parity gap and its verification
- [371-EXT-extras-classification.md](./371-EXT-extras-classification.md):
  current classification of parity-critical, later, and optional extras
- [372-EXT-material-extras-slice.md](./372-EXT-material-extras-slice.md):
  current in-sequence material extras surface landed for the .NET port
- [373-EXT-simd-optimization-surface.md](./373-EXT-simd-optimization-surface.md):
  optional safe optimization surface and hook model for the .NET port
- [344-WEB-web-boundary.md](./344-WEB-web-boundary.md): in-scope boundary for
  the local web and wasm host surface
- [357-VRF-shared-sample-comparison-scaffold.md](./357-VRF-shared-sample-comparison-scaffold.md):
  deterministic cross-implementation sample runner scaffold against the managed
  upstream workspace
- [391-DOC-dotnet-implementation-notes.md](./391-DOC-dotnet-implementation-notes.md):
  .NET-specific implementation, verification, and maintainer-orientation notes
- [EXTERNALS.md](./EXTERNALS.md): inventory of local external repositories and
  libraries managed under `.external/`, plus rebuild instructions
- [2026-03-09-hosted-parity-blockers.md](./2026-03-09-hosted-parity-blockers.md):
  blockers and remaining gaps from the hosted parity batch
- [2026-03-09-big-batch-blockers.md](./2026-03-09-big-batch-blockers.md):
  blockers and remaining gaps from the base-port batch
- [2026-03-12-windows-conpty-evidence-blocker.md](./2026-03-12-windows-conpty-evidence-blocker.md):
  explicit remaining non-local blocker for native Windows ConPTY evidence

## Root Documents

- [../CHARTER.md](../CHARTER.md): governing project definition
- [../PROVENANCE.md](../PROVENANCE.md): upstream basis and rights framing
- [../LICENSE](../LICENSE): repository licensing overview
- [../AGENTS.md](../AGENTS.md): current repository execution doctrine for agents
