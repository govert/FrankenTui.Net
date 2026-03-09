# 2026-03-09 Hosted-Parity Blockers

This note records blockers and still-open gaps encountered during the hosted
parity batch spanning `312-WGT` through `384-TOL`.

## Active Blockers

- The current workspace does not include a real browser automation or node-based
  execution lane, so `356-VRF` currently validates deterministic HTML/document
  output and shared web state through .NET tests rather than live browser
  interaction.
- `357-VRF` now has local hosted-parity evidence bundles, but it still lacks an
  automated upstream FrankenTUI comparison runner from this repo. Upstream
  parity comparison therefore remains an evidence workflow foundation, not a
  finished cross-language oracle.
- `358-VRF` has not yet gained benchmark or performance-gate automation. The
  hosted-parity batch prioritized shared correctness surfaces, doctor artifacts,
  and CI correctness gates before performance instrumentation.

## Work Continued Around These Blockers

- Interactive widget/session state, shared hosted demo surfaces, deterministic
  web output, and doctor dashboards were implemented without waiting for browser
  automation.
- CI now exercises build/test on Linux and Windows and refreshes doctor
  artifacts on Linux so the operational verification story is not blocked on the
  remaining browser/upstream/perf gaps.
- Hosted parity evidence writing is active in the local harness, which means the
  missing upstream-comparison lane is explicit and isolated rather than hidden in
  the code path.
