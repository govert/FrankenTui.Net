# 240-MAP Module Mapping Ledger

## Purpose

This is the first crate-to-project ledger for FrankenTui.Net. It records the
initial .NET project ownership for each meaningful upstream surface so porting
waves can stay traceable.

## Upstream Basis

- Upstream repo: `.external/frankentui`
- Basis commit for this ledger:
  `7a91089366bd4644e086d5a422cb76b052e3de17`

## Crate To Project Mapping

| Upstream Surface | Local Project | Category | Initial Status | Notes |
| --- | --- | --- | --- | --- |
| `ftui` | `src/FrankenTui` | public facade | scaffolded | top-level consumer surface |
| `ftui-core` | `src/FrankenTui.Core` | kernel | scaffolded | terminal lifecycle, capabilities, events |
| `ftui-backend` | `src/FrankenTui.Backend` | abstraction | scaffolded | backend traits and host abstraction |
| `ftui-tty` | `src/FrankenTui.Tty` | host | scaffolded | native terminal backend |
| `ftui-render` | `src/FrankenTui.Render` | kernel | scaffolded | cells, buffers, diffs, presenter |
| `ftui-style` | `src/FrankenTui.Style` | library | scaffolded | styles, themes, colors |
| `ftui-layout` | `src/FrankenTui.Layout` | library | scaffolded | layout solvers and pane geometry |
| `ftui-text` | `src/FrankenTui.Text` | library | scaffolded | wrapping, width, segmentation, editing |
| `ftui-runtime` | `src/FrankenTui.Runtime` | library | scaffolded | runtime loop, replay, subscriptions |
| `ftui-widgets` | `src/FrankenTui.Widgets` | library | scaffolded | widget surface |
| `ftui-extras` | `src/FrankenTui.Extras` | library | scaffolded | optional higher-level features |
| `ftui-a11y` | `src/FrankenTui.A11y` | library | scaffolded | accessibility integration |
| `ftui-i18n` | `src/FrankenTui.I18n` | library | scaffolded | localization and catalog support |
| `ftui-simd` | `src/FrankenTui.Simd` | optimization | landed | optional optimization surface with hook-based enablement |
| `ftui-harness` | `src/FrankenTui.Testing.Harness` | verification support | scaffolded | reusable support library, not a test bucket |
| `ftui-pty` | `src/FrankenTui.Testing.Pty` | verification support | scaffolded | reusable PTY support layer |
| `ftui-web` | `src/FrankenTui.Web` | host | scaffolded | deterministic web host surface |
| `ftui-showcase-wasm` | `apps/FrankenTui.Showcase.Wasm` | app | scaffolded | wasm showcase runner surface |
| `ftui-demo-showcase` | `apps/FrankenTui.Demo.Showcase` | app | scaffolded | terminal showcase app |
| `doctor_frankentui` | `tools/FrankenTui.Doctor` | tool | scaffolded | separate diagnostics and evidence tool |

## Repo-Level Verification Mapping

| Upstream Surface | Local Target | Notes |
| --- | --- | --- |
| `tests/compat` | `tests/FrankenTui.Tests.Pty` + `artifacts/pty/` | compatibility matrix and terminal-profile evidence |
| `tests/e2e` | `tests/FrankenTui.Tests.Pty`, `tests/FrankenTui.Tests.Web`, `artifacts/` | PTY, showcase, and host-driven evidence |
| `tests/repro` | `tests/FrankenTui.Tests.Headless` | focused regression corpus |
| `tests/fixtures` | `artifacts/` plus future tracked test assets | promote only stable baselines |
| `fuzz/` | future `VRF` work under `tests/` or `tools/` | not scaffolded yet, but still in scope |

## Interpretation Notes

- The mapping is intentionally close to upstream while the port is young.
- Reusable verification libraries live in `src/` because they are intended to
  be consumed by apps, tools, and test projects.
- Actual runnable tests live in `tests/` and are currently split by mode rather
  than by crate.
- This ledger records ownership first; several surfaces have now advanced far
  beyond the initial scaffolded baseline.
