# 373-EXT SIMD Optimization Surface

## Purpose

This document records the completed `373-EXT` batch: the .NET equivalent of the
upstream `ftui-simd` optional optimization surface.

The goal is not to bake acceleration logic into every core assembly. The goal
is to keep the base implementation correct and portable while allowing selected
hot paths to opt into safe acceleration when the optimization assembly is
present and enabled.

## Local Shape

The optimization surface now lives in:

- `src/FrankenTui.Simd`

It currently provides:

- capability detection for vector and platform SIMD availability
- a safe accelerated row-diff path for render-buffer comparison
- a safe accelerated word-wrap path for repeated text wrapping
- explicit enable/disable hooks so the optimization layer stays optional

## Hooking Model

The core assemblies do not take a hard dependency on `FrankenTui.Simd`.
Instead:

- `FrankenTui.Render` exposes an `IBufferDiffAccelerator` hook
- `FrankenTui.Text` exposes an `ITextWrapAccelerator` hook
- `FrankenTui.Simd` registers implementations into those hooks when
  `SimdAccelerators.EnableIfSupported()` is called

This is the .NET analogue of the upstream “optional optimization crate” model:
the fast path is available, but the baseline system remains fully functional
without it.

## Enabled Surfaces

The current repo enables SIMD/optimization hooks in places where performance
evidence matters most:

- benchmark and gate execution via `FrankenTui.Testing.Harness`
- the demo showcase app
- the wasm/web showcase page
- the doctor tool

That keeps the acceleration story visible in the actual repo workflows instead
of leaving it as a dormant library-only experiment.

## Verification

The optimization surface is verified by:

- equivalence tests that compare optimized and non-optimized diff/wrap results
- the existing benchmark gate and benchmark-runner tests
- full solution test runs, including web and PTY coverage, with the optimization
  project present in the graph

## Scope Boundary

This batch does not attempt:

- unsafe-code intrinsics
- architecture-specific hand-written assembly logic
- broad algorithm rewrites across the whole stack

The current rule is narrower: port the optional safe optimization surface, wire
it into the measured hot paths, and keep behavior identical to the base path.
