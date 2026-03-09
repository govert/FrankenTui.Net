# 344-WEB Web Boundary

## Purpose

This note defines the boundary for the in-scope web and wasm work in the
FrankenTui.Net port.

## In Scope

- `src/FrankenTui.Web` as the deterministic HTML host over the shared .NET
  render and runtime path
- `apps/FrankenTui.Showcase.Wasm` as the local showcase-oriented wasm/web host
  surface for parity work
- shared session metadata, accessibility snapshots, and artifact-producing HTML
  documents used by verification, demo, and doctor flows
- web parity tests that confirm the hosted surface stays aligned with the same
  terminal-facing widget tree and session model

## Reference Only

- upstream or future website repos, docs-site content, deployment wrappers, or
  unrelated marketing/documentation assets
- remote protocol bridges or browser-host orchestration code that is not needed
  for the coherent local FrankenTui.Net library surface
- copied upstream web assets when the local port can instead render from the
  shared core/runtime/widget path

## Execution Rule

The web/wasm surface in this repo is a host over the same core-facing session
and widget model used by terminal, demo, doctor, and verification flows. It is
not a separate UI implementation track.
