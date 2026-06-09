export const meta = {
  name: 'port-continue',
  description: 'Continue the Rust→C# FrankenTui port: scout gaps, port files 1-to-1 with Sonnet workers, oversight-verify for shortcuts, build+test',
  whenToUse: 'Run to advance the port for one target project/area. Pass args to pick the crate, project, and (optionally) a subdir filter.',
  phases: [
    { title: 'Scout', detail: 'Find unported / stub .rs files in the target area' },
    { title: 'Port', detail: 'Sonnet workers port each .rs → .cs + xUnit tests, 1-to-1' },
    { title: 'Verify', detail: 'Oversight (Opus) checks fidelity & shortcut wording per file' },
    { title: 'Build', detail: 'dotnet build + targeted tests over the project' },
  ],
}

// ---- args (with defaults targeting widgets subdirs) ----
// NOTE: the runtime delivers `args` as a JSON STRING, so parse it.
const A = (typeof args === 'string' ? (args ? JSON.parse(args) : {}) : (args || {}))
const crate       = A.crate       || 'ftui-widgets'
const rustDir     = A.rustDir     || '.external/frankentui/crates/ftui-widgets/src'
const project     = A.project     || 'src/FrankenTui.Widgets'
const ns          = A.namespace   || 'FrankenTui.Widgets'
const testProject = A.testProject || 'tests/FrankenTui.Tests.Headless'
const subdirs     = A.subdirs     || null            // e.g. ['command_palette','focus','modal']; null = whole crate
const maxFiles    = A.maxFiles    || 8               // cap per run so oversight stays reviewable
const portTests   = A.portTests !== false           // default true
const exclude     = A.exclude     || []              // already-ported file/type names to skip (avoids churn)

const subdirNote = subdirs
  ? `RESTRICT to these subdirectories of the rust src: ${subdirs.join(', ')}.`
  : 'Cover the whole crate src/ (top-level files).'

// ---- schemas ----
const SCOUT_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['files', 'totalGap', 'droppedCount'],
  properties: {
    totalGap: { type: 'integer', description: 'total unported/stub .rs files found in scope' },
    droppedCount: { type: 'integer', description: 'how many in-scope files were NOT included this run due to the cap' },
    files: {
      type: 'array',
      items: {
        type: 'object', additionalProperties: false,
        required: ['name', 'rustPath', 'lines', 'csPath', 'status'],
        properties: {
          name:    { type: 'string', description: 'PascalCase type/file stem, e.g. ModalStack' },
          rustPath:{ type: 'string', description: 'repo-relative path to upstream .rs' },
          lines:   { type: 'integer' },
          csPath:  { type: 'string', description: 'repo-relative target .cs path' },
          testPath:{ type: 'string', description: 'repo-relative target *Tests.cs path' },
          status:  { type: 'string', enum: ['missing', 'stub'], description: 'missing = no .cs; stub = .cs exists but tiny vs upstream' },
        },
      },
    },
  },
}

const VERDICT_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['file', 'isFaithful', 'severity', 'shortcutsFound', 'missingItems', 'notes'],
  properties: {
    file:        { type: 'string' },
    isFaithful:  { type: 'boolean', description: 'true only if EVERY upstream type, fn, and test has a real, non-stubbed counterpart' },
    severity:    { type: 'string', enum: ['ok', 'minor', 'major', 'shortcut'], description: 'shortcut = abbreviated/placeholder port that must be redone' },
    shortcutsFound: { type: 'array', items: { type: 'string' }, description: 'exact quoted phrases/patterns indicating a shortcut (e.g. "simplified", "// TODO", "NotImplementedException", "for brevity", "...", "stub")' },
    missingItems:   { type: 'array', items: { type: 'string' }, description: 'upstream types/fns/tests with NO real counterpart in the port' },
    notes:       { type: 'string' },
  },
}

const BUILD_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['buildOk', 'errorCount', 'errors', 'testsRun', 'testsPassed', 'testsFailed', 'failures'],
  properties: {
    buildOk:     { type: 'boolean' },
    errorCount:  { type: 'integer' },
    errors:      { type: 'array', items: { type: 'string' } },
    testsRun:    { type: 'integer' },
    testsPassed: { type: 'integer' },
    testsFailed: { type: 'integer' },
    failures:    { type: 'array', items: { type: 'string' } },
  },
}

// ---- Phase 1: Scout ----
phase('Scout')
const scout = await agent(
  `You are scouting the FrankenTui.Net Rust→C# port (repo root C:\\Work\\FrankenTui.Net).
Target crate: ${crate}
Upstream rust src dir: ${rustDir}
C# project: ${project}   (namespace ${ns})
${subdirNote}

Find every upstream .rs file in scope that is NOT yet faithfully ported. Classify each as:
- MISSING: no matching .cs by stem anywhere in the project.
- STUB: a .cs exists but is clearly a skeleton — it LACKS a "// Port of <that .rs>" header line, OR (when tests are in scope) has no matching <Name>Tests.cs in the test project.
IMPORTANT: do NOT flag a file as a stub merely because the .cs has fewer lines than the .rs — faithful C# ports are routinely 40-60% of the Rust line count. A .cs that carries the "// Port of" header AND has a matching <Name>Tests.cs is DONE; exclude it.
Ignore trivial re-export shells (mod.rs / lib.rs that are <15 lines and only 're-export').

Use ripgrep/glob/bash for counts. Do NOT read full file bodies.
EXCLUDE these already-ported files/types — do NOT return them even if they look like stubs (C# ports are legitimately more compact than Rust): ${exclude.length ? exclude.join(', ') : '(none)'}.
For each file produce: name (PascalCase stem), rustPath, lines, csPath (under ${project}, stem-named .cs; for subdir files put them under ${project}/<Subdir>/<Name>.cs), testPath (${testProject}/<Name>Tests.cs), status.
Sort by lines DESCENDING. Return at most ${maxFiles} files (the largest/most impactful). Set totalGap to the full in-scope count and droppedCount = totalGap - returned.`,
  { label: `scout:${crate}`, phase: 'Scout', schema: SCOUT_SCHEMA, agentType: 'Explore' },
)

if (!scout || !scout.files || scout.files.length === 0) {
  log(`Scout found no gap files in scope for ${crate}. Nothing to port.`)
  return { crate, scope: subdirs, ported: [], flagged: [], build: null, scout }
}
if (scout.droppedCount > 0) {
  log(`⚠ Cap reached: porting ${scout.files.length} of ${scout.totalGap} in-scope gap files this run; ${scout.droppedCount} deferred. Re-run to continue.`)
}
log(`Scouted ${scout.files.length} file(s) to port: ${scout.files.map(f => `${f.name}(${f.lines}L,${f.status})`).join(', ')}`)

// ---- Phases 2+3: Port then Verify, pipelined per file ----
const portPrompt = (f) =>
  `You are a faithful Rust→C# porter for FrankenTui.Net (repo root C:\\Work\\FrankenTui.Net).
Read the porting doctrine in AGENTS.md (Porting Conventions) — it is BINDING.

PORT THIS FILE 1-TO-1, IN FULL:
  upstream: ${f.rustPath}  (${f.lines} lines)
  target:   ${f.csPath}    (namespace ${ns})
  ${portTests ? `tests:    ${f.testPath}  (xUnit, in namespace ${testProject.includes('Headless') ? 'FrankenTui.Tests.Headless' : ns + '.Tests'})` : 'Do NOT port tests this run.'}

HARD RULES — violating any of these fails oversight:
- This is a DIRECT, FAITHFUL, line-for-line port. Every Rust struct/enum/trait/fn AND every #[test] must have a real counterpart. NO omissions.
- Preserve upstream names (PascalCase types, snake_case→PascalCase fns, fields→_camelCase/PascalCase). No abbreviation.
- Carry upstream //! and /// docs across as /// summaries.
- File header MUST be: "// Port of ${f.rustPath}" plus a one-line description (match existing files like Block.cs).
- Traits → I-prefixed interfaces; enums-with-data → closed class hierarchy (per AGENTS.md).
- ${portTests ? 'Port the #[cfg(test)] block fully as [Fact]/[Theory] methods. Header: "// Upstream source: <rust path> (tests module)" + "// Full 1-1 port of all upstream <name> tests."' : ''}
- DO NOT use any of these shortcut patterns: NotImplementedException, "TODO", "FIXME", "for brevity", "simplified", "placeholder", "stub", "in a real implementation", "omitted", "// ...", "rest of", "abbreviated". If something is genuinely impossible in .NET, add a "// DIVERGENCE:" comment with the reason — do not silently drop it.
- NEVER comment out, relax, or delete a ported test assertion. For upstream perf/timing assertions (e.g. p95 <= N µs) KEEP the assertion but scale the budget ×10 with a "// DIVERGENCE: 10x for .NET JIT/timer overhead" comment — do not turn it into a log-only/soft check.
- NEVER run destructive git commands (git checkout/restore/reset/stash/clean/rm, or anything that reverts or discards working-tree files). The working tree contains a large uncommitted refactor; reverting tracked files destroys it. Only create/edit the specific .cs/test files for this port.
- New .cs files are auto-included by the SDK csproj glob; do not edit the .csproj.
- If a .cs stub already exists at the target, REPLACE it with the full port (Read it first).

Read the upstream .rs fully, then write the complete .cs (and tests). Return a short summary: types ported, fns ported, tests ported, any DIVERGENCE notes.`

const maxRetries = A.maxRetries != null ? A.maxRetries : 2   // silent auto re-port attempts per file

const verifyPrompt = (f, summary) =>
  `You are the OVERSIGHT reviewer for the FrankenTui.Net port (repo root C:\\Work\\FrankenTui.Net).
A worker just ported:
  upstream: ${f.rustPath}
  port:     ${f.csPath}
  ${portTests ? `tests:    ${f.testPath}` : ''}
Worker's self-report: ${summary}

Independently verify FIDELITY and DEPTH (do not trust the self-report):
1. Read the upstream .rs and the produced .cs (and tests). Enumerate upstream public types, fns, and #[test] functions, and confirm each has a REAL, non-stubbed counterpart. List any with no counterpart in missingItems.
2. Scan the .cs/tests for shortcut wording/patterns: NotImplementedException, TODO, FIXME, "for brevity", "simplified", "placeholder", "stub", "in a real implementation", "omitted", "// ...", "rest of", "abbreviated", or a body that is dramatically smaller than upstream warrants. ALSO treat any commented-out, relaxed, soft/log-only, or removed test assertion (vs the upstream assertion) as a shortcut. Put each exact occurrence in shortcutsFound.
3. severity: 'ok' = faithful full port; 'minor' = small cosmetic gaps (e.g. a missing InternalsVisibleTo) with EMPTY shortcutsFound; 'major' = meaningful behavior/types missing; 'shortcut' = abbreviated/placeholder port OR any weakened/removed assertion — must be redone. If shortcutsFound is non-empty, severity must be 'shortcut' or 'major', never 'minor'.
isFaithful is true ONLY if there are zero shortcuts and zero missing upstream items (DIVERGENCE comments with real reasons are acceptable). Do not modify files.`

const reportPrompt = (f, v) =>
  `You are RE-PORTING a file the oversight reviewer REJECTED for taking shortcuts. Repo root C:\\Work\\FrankenTui.Net. AGENTS.md Porting Conventions are binding.
  upstream: ${f.rustPath} (${f.lines} lines)
  port:     ${f.csPath}
  ${portTests ? `tests:    ${f.testPath}` : ''}
The previous attempt failed oversight with severity "${v.severity}".
SHORTCUTS FOUND (remove every one): ${JSON.stringify(v.shortcutsFound)}
MISSING UPSTREAM ITEMS (add a real, complete counterpart for every one): ${JSON.stringify(v.missingItems)}
Reviewer notes: ${v.notes}

Read the upstream .rs IN FULL and rewrite the .cs (and tests) as a complete, faithful 1-to-1 port. Every upstream type/fn/#[test] must have a real implementation — NO shortcut patterns (NotImplementedException, TODO, "for brevity", "simplified", "placeholder", "stub", "// ...", etc.). Use "// DIVERGENCE:" only for genuine .NET constraints. Return a short summary of what you fixed.`

// Port → verify → (silently re-port + re-verify up to maxRetries) per file, in parallel.
const handleFile = async (f) => {
  let summary = await agent(portPrompt(f), { label: `port:${f.name}`, phase: 'Port', model: 'sonnet' })
  let verdict = await agent(verifyPrompt(f, summary), { label: `verify:${f.name}`, phase: 'Verify', model: 'opus', schema: VERDICT_SCHEMA, agentType: 'Explore' })
  let attempts = 1
  // Re-port for genuine content shortcuts: severity shortcut/major OR ANY shortcutsFound
  // entry (e.g. a commented-out / weakened test assertion, which graders may mislabel
  // 'minor'). Cosmetic isFaithful=false with no shortcutsFound (e.g. missing
  // InternalsVisibleTo) is NOT re-ported — re-porting the file can't fix that.
  const needsRepc = (v) => v && (v.severity === 'shortcut' || v.severity === 'major' || (v.shortcutsFound && v.shortcutsFound.length > 0))
  while (needsRepc(verdict) && attempts <= maxRetries) {
    log(`↻ Re-porting ${f.name} (attempt ${attempts + 1}/${maxRetries + 1}; was ${verdict.severity})`)
    summary = await agent(reportPrompt(f, verdict), { label: `report:${f.name}#${attempts}`, phase: 'Port', model: 'sonnet' })
    verdict = await agent(verifyPrompt(f, summary), { label: `reverify:${f.name}#${attempts}`, phase: 'Verify', model: 'opus', schema: VERDICT_SCHEMA, agentType: 'Explore' })
    attempts++
  }
  return { ...f, summary, verdict, attempts }
}

const results = await parallel(scout.files.map(f => () => handleFile(f)))

const ported = results.filter(Boolean)
// Only files still failing AFTER all silent re-port attempts are surfaced as flagged.
const flagged = ported.filter(r => r.verdict && (r.verdict.severity === 'shortcut' || r.verdict.severity === 'major'))
if (flagged.length) {
  log(`🚩 ${flagged.length} file(s) STILL failing oversight after ${maxRetries} re-port attempt(s): ${flagged.map(r => `${r.name}[${r.verdict.severity}]`).join(', ')}`)
} else {
  log(`✓ Oversight passed all ${ported.length} ported file(s) (some after silent re-port).`)
}

// ---- Phase 4: Build + targeted tests, with auto-fix loop ----
phase('Build')
const portedNames = ported.map(r => r.name)
const maxBuildIter = A.maxBuildIter != null ? A.maxBuildIter : 3
let bpass = 0
let build = { buildOk: false, errorCount: 0, errors: [], testsRun: 0, testsPassed: 0, testsFailed: 0, failures: [] }
while (bpass < maxBuildIter) {
  bpass++
  build = await agent(
    `Build/test gate WITH FAITHFUL AUTO-FIX for the FrankenTui.Net port (repo root C:\\Work\\FrankenTui.Net). Pass ${bpass}.
Files ported this run: ${portedNames.join(', ')}.

1. Build: dotnet build ${project}/*.csproj --nologo -v q . If there are compile errors IN THE FILES PORTED THIS RUN (${portedNames.join(', ')} or their test files), fix them faithfully (no stubs). Do not touch unrelated pre-existing errors elsewhere.
2. Run targeted tests for the ported types:
   dotnet test ${testProject}/*.csproj --no-restore --nologo ${portedNames.map(n => `--filter "FullyQualifiedName~${n}Tests"`).join(' ')}
   (If multiple --filter args aren't supported, run them one at a time and sum.)
3. For each FAILING test: the test is a 1-to-1 port of the upstream Rust test and is the source of truth, so FIX THE PRODUCTION PORT (the .cs under ${project}) to produce the upstream-correct behavior. Read the upstream .rs if needed. Only adjust a test if it provably diverges from the upstream Rust assertion — never weaken or delete assertions to make a test pass, never use NotImplementedException/placeholder. Then rerun the targeted tests.
NEVER run destructive git commands (checkout/restore/reset/stash/clean) — the tree has a large uncommitted refactor and reverting tracked files destroys it. Only edit the ported files; do not touch unrelated files.
Report final buildOk, errorCount/errors, testsRun/testsPassed/testsFailed, and the first line of each remaining failure.`,
    { label: `build+fix#${bpass}`, phase: 'Build', schema: BUILD_SCHEMA, model: 'sonnet' },
  )
  log(`Build pass ${bpass}: build ${build.buildOk ? 'OK' : 'FAIL'}, tests ${build.testsPassed}/${build.testsRun} pass, ${build.testsFailed} fail.`)
  if (build.buildOk && build.testsFailed === 0) break
}

return {
  crate,
  scope: subdirs,
  scout: { totalGap: scout.totalGap, dropped: scout.droppedCount },
  ported: ported.map(r => ({ name: r.name, rustPath: r.rustPath, csPath: r.csPath, lines: r.lines, attempts: r.attempts, verdict: r.verdict })),
  flagged: flagged.map(r => ({ name: r.name, severity: r.verdict.severity, shortcutsFound: r.verdict.shortcutsFound, missingItems: r.verdict.missingItems, notes: r.verdict.notes })),
  build,
}
