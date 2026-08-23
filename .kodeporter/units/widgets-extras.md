---
id: widgets-extras
name: Widgets, extras, and optional acceleration
status: in-progress
sourceAnchors:
  - symbolPath: ftui-widgets
    basisLabel: upstream-15cc6543-local-helper
    contentHash: a72b16435e64287f7d3595727ae9a390cd0ca770bab16ee693123ce9c6576399
  - symbolPath: ftui-extras
    basisLabel: upstream-15cc6543-local-helper
    contentHash: c177cddd0830e279e94117d2147e5acb831490735618173022833aecc92167a0
  - symbolPath: ftui-a11y
    basisLabel: upstream-15cc6543-local-helper
    contentHash: 56aadcb11f49648000aecc432e9482bd282c053b6967025e112603d1fa5d9e96
targetAnchors:
  - symbolPath: FrankenTui.Widgets
    basisLabel: target-current-worktree
    contentHash: 1c0aaa9c4c07865a5ea0591d5030e75b1ab3adfa510e15aa026b4128f8a10522
  - symbolPath: FrankenTui.Extras
    basisLabel: target-current-worktree
    contentHash: 30d0f63a02520dbbb4508f6a5f023afb1f9731744b0e7820af13c5598a077140
  - symbolPath: FrankenTui.A11y
    basisLabel: target-current-worktree
    contentHash: 51d4f6b971cdf6eb29536a428b01b3d8524158b683d938725f2a71a0339d9a27
claims: []
depth: dossiered
---

## Purpose

Port reusable widgets, focus/modal/input state, extras, accessibility/i18n helpers, canvas/VFX primitives, themes, Mermaid support, and optional safe acceleration.

## Contract

Prefer one-to-one source/file ownership and strengthen shared layers when showcase pressure reveals missing capability. Preserve rendering, mutation, focus, hit testing, degradation, clearing, accessibility, and error behavior. Demo-only substitutes do not close a reusable upstream contract.

## Questions

Which upstream widget and extras files have no independent .NET owner? Which local compatibility helpers are placeholders, wrappers, or fallbacks rather than native realizations? Are the deleted `ThemeSystem.cs` and untracked replacement files one coherent migration?

## Evidence

Closed module/public-surface inventories, provenance headers, widget render/state tests, property tests, focus and modal tapes, canvas/VFX goldens, and explicit realization labels.

