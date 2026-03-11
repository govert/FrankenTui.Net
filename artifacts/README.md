# Artifacts

Generated parity evidence, traces, snapshots, replays, benchmark outputs, and
doctor artifacts belong here.

The repository tracks the layout, not the generated contents. `.gitignore`
keeps artifact output local by default while preserving these stable buckets:

- `headless/`
- `pty/`
- `web/`
- `replay/`
- `benchmarks/`
- `contracts/`
- `comparison/`

Doctor runs currently materialize their runtime evidence in `replay/`, rendered
dashboard output in `web/`, benchmark output in `benchmarks/`, and contract
snapshots in `contracts/`.

`comparison/` may also contain generated local helper projects used to run the
managed upstream workspace for parity checks. Those helpers are rebuildable and
remain local-only.

`contracts/` contains rebuildable local snapshots of telemetry config,
Mermaid config, and OpenTUI contract artifacts produced by the doctor/tooling
lane.

Promote small curated baselines intentionally once they become stable review
assets.
