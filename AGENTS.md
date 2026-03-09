# AGENTS

Read [README.md](./README.md) for orientation, then read
[CHARTER.md](./CHARTER.md) before making substantive project decisions.
Use [docs/README.md](./docs/README.md) as the entry point for tracked project
documentation.

## Current Doctrine

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

Additional agent-specific execution doctrine will be added here over time.
