# External Dependencies Workspace

## Purpose

`.external/` is the local, intentionally `.gitignore`d workspace for checked-out
copies of external repositories, libraries, and other upstream source trees used
for reference, comparison, provenance work, and sync operations.

The contents of `.external/` are not tracked in Git. This document tracks what
is expected to exist there and how to recreate it.

## Layout Rule

- Root path: `./.external/`
- Policy: local-only, untracked, reproducible from this document
- Use: upstream reference material, comparison baselines, and local sync work

## Managed Entries

### `frankentui`

- Path: `./.external/frankentui`
- Type: Git repository clone
- Upstream: `https://github.com/Dicklesworthstone/frankentui.git`
- Branch: `main`
- Current local reference commit: `40c98246f27f9d174b3923c8df841ba325247dd4`
- Purpose: canonical upstream basis for FrankenTui.Net porting and provenance
  checks

## Recreate `.external/`

From the repository root:

```bash
mkdir -p .external
git clone https://github.com/Dicklesworthstone/frankentui.git .external/frankentui
git -C .external/frankentui checkout 40c98246f27f9d174b3923c8df841ba325247dd4
```

If you want the latest upstream tracking state instead of the recorded
reference, omit the final `checkout` and keep the clone on `main`.

## Maintenance Rule

When a new external repository or library is intentionally managed under
`.external/`, update this document with:

- the local path
- the upstream source URL
- the branch, tag, or commit policy
- the purpose of the local copy
- exact recreation commands when they differ from a normal clone
