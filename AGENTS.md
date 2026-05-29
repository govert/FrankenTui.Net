# AGENTS

Read [README.md](./README.md) for orientation, then read
[CHARTER.md](./CHARTER.md) before making substantive project decisions.
Use [docs/README.md](./docs/README.md) as the entry point for tracked project
documentation.

## Current Doctrine

- **This is a direct, faithful, line-for-line port of the Rust FrankenTUI codebase
  to .NET.** Every screen, widget, subsystem, and tool upstream must have a
  corresponding .NET implementation that produces identical output. When in doubt,
  the Rust code is the source of truth. Never settle for "close enough" — the
  target is bit-identical rendered output and behavior.
- `CHARTER.md` is the prescriptive execution doctrine for this repository.
- If supporting docs drift, `CHARTER.md` wins unless a later governance document
  explicitly says otherwise.
- Optimize for faithful FrankenTUI porting, traceability to upstream, explicit
  divergence management, deterministic behavior, and a small dependency surface.
- Treat provenance and licensing as first-class engineering concerns; consult
  [PROVENANCE.md](./PROVENANCE.md) and [LICENSE](./LICENSE) when adding or
  porting material.
- Treat [docs/EXTERNALS.md](./docs/EXTERNALS.md) as the inventory of managed
  local external repositories and the source of truth for recreating
  `.external/`.
- Use [docs/210-STS-port-status.md](./docs/210-STS-port-status.md) as the
  canonical execution-status ledger before starting or resuming a workstream.
- Use [docs/242-MAP-upstream-sync-workflow.md](./docs/242-MAP-upstream-sync-workflow.md),
  [docs/304-RTM-determinism-and-evidence.md](./docs/304-RTM-determinism-and-evidence.md),
  and [docs/335-HST-host-divergence-ledger.md](./docs/335-HST-host-divergence-ledger.md)
  when working on provenance-sensitive, fidelity-sensitive, or host-sensitive
  surfaces.
- When reporting each work batch, orient the user to the relevant planning-doc
  location by code and path, so implementation progress stays anchored to the
  tracked hierarchy.

## Tooling Hints

- For C#/.NET work, prefer the installed `roscli` CLI over MCP wiring for
  semantic navigation, structured edits, and diagnostics.
- Use `xmlcli` for XML/XAML-oriented structure and validation work.
- Use `dotnet-inspect` for external package/framework API inspection.
- A C# language server may be used when helpful, but `roscli` is the default
  first tool for repo-local semantic work.

## Porting Conventions (Cross-Language Mapping Rules)

These are the governing rules for every ported file. They exist to keep the
.NET codebase traceable, diffable, and syncable against the upstream Rust
workspace under `.external/frankentui/`.

### File Mapping

- **1-1 file mapping is the default.** Every upstream `.rs` source file maps
to a `.cs` file with the same stem name in the corresponding .NET project.
  - `crates/ftui-widgets/src/borders.rs` → `src/FrankenTui.Widgets/Borders.cs`
  - `crates/ftui-extras/src/filesize.rs` → `src/FrankenTui.Extras/FileSize.cs`
  - `crates/ftui-render/src/sanitize.rs` → `src/FrankenTui.Render/Sanitize.cs`
  - `crates/ftui-runtime/src/retry.rs` → `src/FrankenTui.Runtime/Retry.cs`
- **Exception**: if an upstream file is explicitly deprecated, empty, or a
  no-op that has no behavioral equivalent in .NET, skip it and record the
  decision in the file mapping comment of the adjacent ported module.

### Namespace Mapping

- Namespace hierarchy mirrors upstream crate boundaries:
  - `ftui-widgets` → `FrankenTui.Widgets`
  - `ftui-extras` → `FrankenTui.Extras`
  - `ftui-render` → `FrankenTui.Render`
  - `ftui-runtime` → `FrankenTui.Runtime`
- Upstream `mod` directory structures become nested namespaces.

### Type Naming

- **Preserve upstream type names wherever possible.** Rust `struct` and `enum`
  names are used verbatim in C# unless a clash forces a change.
- **When a name clash occurs**, prefer the closest alternative and document
the divergence in the file header. Clashes include:
  - C# reserved keywords (`ref`, `out`, `in`, `base`, `string`, `int`, etc.)
  - BCL types that would cause ambiguity at common `using` scope
    (`String`, `List`, `Dictionary`, `Task`, `Timer`, etc.)
  - Inherited `object` members (`GetType`, `Equals`, `ToString` as fields)
- **Rust traits** become C# interfaces with an `I` prefix
  (e.g., `DiagnosticRecord` → `IDiagnosticRecord`).
  When a trait is primarily a generic bound that maps better to an abstract
  base class, use the abstract base class and note why.
- **Simple C-like enums** become C# `enum` with matching variant names.
- **Enum variants with associated data** become a closed class hierarchy
  with a base record/class and per-variant derived records.

### Naming Convention Translation

| Rust convention | C# convention | Example |
| --- | --- | --- |
| `PascalCase` types | `PascalCase` | `BorderSet` → `BorderSet` |
| `snake_case` functions | `PascalCase` | `to_border_set` → `ToBorderSet()` |
| `UPPER_SNAKE_CASE` consts | `PascalCase` static readonly | `BINARY_UNITS_SHORT` → `BinaryUnitsShort` |
| `snake_case` fields | `_camelCase` private or `PascalCase` property | `max_entries` → `_maxEntries` |

### Test Porting

- Upstream `#[cfg(test)] mod tests { ... }` blocks become xUnit `[Fact]` or
  `[Theory]` methods in the corresponding test project.
- Test files mirror the source file name with a `Tests` suffix:
  `Borders.cs` → `BordersTests.cs` in `tests/FrankenTui.Tests.Headless/`.
- Tests stay out of the source assembly; only the production code is ported
  into `src/`.

### Documentation Headers

Every ported `.cs` file must carry a header comment block identifying:
- The upstream source file path (relative to `.external/frankentui/`)
- The upstream commit basis (from the workspace)
- Any documented intentional divergences

### Provenance And Divergence

- All ported code should be directly attributable to upstream concepts.
- When the .NET port intentionally diverges, add a `// DIVERGENCE:` comment
  at the point of divergence with the reason.
- Keep [docs/246-MAP-upstream-contract-gap-register.md](./docs/246-MAP-upstream-contract-gap-register.md)
  current as porting proceeds.

Additional agent-specific execution doctrine will be added here over time.
