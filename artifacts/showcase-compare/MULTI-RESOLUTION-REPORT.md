# FrankenTui Showcase — Multi-Resolution Comparison Report

Generated: 2026-05-24 UTC  
Rust binary: `.external/frankentui/target/release/ftui-demo-showcase.exe`  
.NET tool: `tools/FrankenTui.ShowcaseCompare`  
Resolutions compared: 80x24, 120x40, 200x50 (45 screens each = 135 comparisons)

## Executive Summary

| Metric | 80x24 | 120x40 | 200x50 |
|---|---|---|---|
| Exact matches | 0/45 (0%) | 0/45 (0%) | 0/45 (0%) |
| Ratio range | 0.494 – 3.740 | 0.381 – 2.173 | 0.519 – 2.801 |
| Screens with less content than Rust | 12 | 13 | 9 |
| Screens with more content than Rust | 33 | 32 | 36 |
| Near-parity (0.9-1.1 ratio) | 4 | 7 | 3 |

**0 exact matches at any resolution.** All 45 screens render differently between the Rust upstream and .NET port.

## Best Matches (closest to 1.0 ratio at 80x24)

| Screen | 80x24 | 120x40 | 200x50 |
|---|---|---|---|
| #7 Forms & Input | 0.912 | 0.530 | 0.699 |
| #20 Log Search | 0.926 | 1.026 | 1.343 |
| #22 Action Timeline | 1.051 | 1.083 | 1.452 |
| #28 Virtualized Search | 1.031 | 1.078 | 1.394 |

## Worst Matches (furthest from 1.0 at 80x24)

| Screen | 80x24 | 120x40 | 200x50 |
|---|---|---|---|
| #44 Drag & Drop | 3.740 | 1.948 | 2.801 |
| #11 Table Theme Gallery | 2.466 | 1.377 | 1.888 |
| #27 Form Validation | 2.414 | 2.045 | 2.774 |
| #21 Notifications | 2.358 | 2.110 | 2.751 |

## Root Causes of Differences

1. **Box-drawing characters**: Rust uses rounded corners (`╭─╮╰╯`), .NET uses sharp corners (`┌─┐└┘`)
2. **Navigation bar separator**: Rust uses `│`, .NET uses spaces
3. **Screen generator content**: Each port's `ShowcaseViewFactory.Build()` maps to different default states, field labels, and layout per screen number
4. **Content verbosity**: .NET screens 11-45 generally have more text (descriptions, state labels) than Rust equivalents; screens 1-10 tend to have less

## Methodology

- **Rust snapshots**: Captured via `tools/snapshot_rust.py` which runs the release Rust binary and converts ANSI terminal output to plain text grids
- **.NET snapshots**: Rendered headlessly via `ShowcaseViewFactory.Build()` → `HeadlessBufferView.ScreenString()`
- **Comparison**: Row-by-row string comparison after whitespace normalization (line-ending trimming, UTF-8 BOM removal)

## Data Files

| Resolution | Index | Snapshots | Diffs |
|---|---|---|---|
| 80x24 | `artifacts/showcase-compare/index.md` | `local/*.local.snap` | `diff/*.diff.txt` |
| 120x40 | `artifacts/showcase-compare-120x40/index.md` | `local/*.local.snap` | `diff/*.diff.txt` |
| 200x50 | `artifacts/showcase-compare-200x50/index.md` | `local/*.local.snap` | `diff/*.diff.txt` |
