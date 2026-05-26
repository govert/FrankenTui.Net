# FrankenTui Showcase — Multi-Resolution Comparison (Post-Porting)

Generated: 2026-05-26 21:28:34 UTC

## Summary

| Resolution | Exact | Min ratio | Max ratio | < Rust | ~parity | > Rust |
| ---: | ---: | ---: | ---: | ---: | ---: | ---: |
| 80x24 | 0 | 0.504 | 2.276 | 21 | 7 | 17 |
| 120x40 | 0 | 0.342 | 1.254 | 33 | 10 | 2 |
| 200x50 | 0 | 0.423 | 1.654 | 21 | 6 | 18 |

## Improvement vs Initial Baseline

| Metric | Initial | Current | Change |
| --- | ---: | ---: | --- |
| 80x24 max ratio | 3.740 | 2.276 | -39% less extreme |
| 120x40 max ratio | 2.173 | 1.254 | -42% less extreme |
| 80x24 ~parity | 4 | 7 | +3 |
| 120x40 ~parity | 7 | 10 | +3 |

## Top 5 Closest Matches (80x24)

| # | Screen | 80x24 | 120x40 |
| ---: | --- | ---: | ---: |
| 32 | Performance HUD | 1.015 | 0.550 |
| 14 | Performance | 1.054 | 0.812 |
| 22 | Action Timeline | 0.936 | 0.827 |
| 24 | Layout Inspector | 0.933 | 0.813 |
| 25 | Adv Text Editor | 0.932 | 0.752 |

## All 45 screens ported — 45/45 ✅
6 screen files in apps/FrankenTui.Demo.Showcase/Screens/
Systematic fixes: rounded borders, centered titles, tab bar separators