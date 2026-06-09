export const meta = {
  name: 'fix-extras-green',
  description: 'Get FrankenTui.Extras + Tests.Headless compiling again after the Runtime refactor: re-port deleted types from upstream, then iterative faithful build-fix loop, then oversight for stubs',
  phases: [
    { title: 'Analyze', detail: 'Map the new IWidget contract, deleted types, and Extras errors' },
    { title: 'ReportTypes', detail: 'Re-port deleted Runtime types 1-to-1 from upstream' },
    { title: 'FixExtras', detail: 'Iterative build-fix loop until Extras compiles (no stubs)' },
    { title: 'FixTests', detail: 'Iterative build-fix loop until Tests.Headless compiles' },
    { title: 'Verify', detail: 'Oversight scan for stubbed Render / shortcut wording' },
  ],
}

const ROOT = 'C:\\\\Work\\\\FrankenTui.Net'

const ANALYZE_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['iwidgetContract', 'deletedTypes', 'errorFiles', 'notes'],
  properties: {
    iwidgetContract: { type: 'string', description: 'the CURRENT IWidget (and IRuntimeView if present) interface signatures, verbatim, that Extras must satisfy' },
    deletedTypes: {
      type: 'array',
      items: {
        type: 'object', additionalProperties: false,
        required: ['name', 'upstreamRust', 'targetCs', 'plan'],
        properties: {
          name: { type: 'string' },
          upstreamRust: { type: 'string', description: 'repo-relative upstream .rs path the type comes from, or "none" if it is a port-local concept with no single upstream file' },
          targetCs: { type: 'string', description: 'repo-relative target .cs path to (re)create in src/FrankenTui.Runtime' },
          plan: { type: 'string', description: 'what the type must contain so Extras compiles AND it is a faithful port (not a stub)' },
        },
      },
    },
    errorFiles: { type: 'array', items: { type: 'string' }, description: 'distinct Extras .cs files with compile errors' },
    notes: { type: 'string' },
  },
}

const BUILD_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['errorCount', 'remainingErrors', 'stubbedAnything', 'filesChanged', 'notes'],
  properties: {
    errorCount: { type: 'integer', description: 'compile errors remaining AFTER this pass (run dotnet build to confirm)' },
    remainingErrors: { type: 'array', items: { type: 'string' }, description: 'first line of each remaining error' },
    stubbedAnything: { type: 'boolean', description: 'true if you used any NotImplementedException/empty-body/TODO/placeholder to make it compile' },
    filesChanged: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
  },
}

const VERIFY_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['clean', 'stubs', 'notes'],
  properties: {
    clean: { type: 'boolean', description: 'true if no Render method or other member was stubbed/no-opped to force compilation' },
    stubs: { type: 'array', items: { type: 'string' }, description: 'file:member for each stubbed/empty/placeholder body found' },
    notes: { type: 'string' },
  },
}

// ---- Phase 1: Analyze ----
phase('Analyze')
const analysis = await agent(
  `Repo root ${ROOT}. The Runtime was refactored: IWidget.Render changed signature and these types were DELETED and no longer exist anywhere: RuntimeFrameStats, RuntimeInputEnvelope, WidgetInputState, WidgetFlowDirection. FrankenTui.Extras (and therefore tests/FrankenTui.Tests.Headless) no longer compiles.

Do:
1. Read the CURRENT IWidget interface (src/FrankenTui.Widgets or wherever it lives) and IRuntimeView, and capture their exact current signatures into iwidgetContract.
2. Build src/FrankenTui.Extras/*.csproj and collect the distinct erroring files into errorFiles.
3. For each deleted type, find its upstream Rust origin under .external/frankentui/crates/ftui-runtime (or ftui-widgets) and decide the target .cs path in src/FrankenTui.Runtime. These should be RE-PORTED faithfully from upstream (the user confirmed they were dropped by mistake), not stubbed. If a type is genuinely port-local with no single upstream file, set upstreamRust="none" and describe in plan what fields/behavior Extras requires of it.
Use ripgrep/build output; read only what you need. Return the structured analysis. Do not modify files.`,
  { label: 'analyze', phase: 'Analyze', schema: ANALYZE_SCHEMA, model: 'opus' },
)

log(`IWidget contract captured. Deleted types to re-port: ${analysis.deletedTypes.map(t => t.name).join(', ')}. Extras error files: ${analysis.errorFiles.length}.`)

// ---- Phase 2: Re-port deleted types (parallel; distinct new files) ----
phase('ReportTypes')
await parallel(analysis.deletedTypes.map(t => () => agent(
  `Repo root ${ROOT}. Re-port the type "${t.name}" into ${t.targetCs} (namespace FrankenTui.Runtime), faithfully from upstream ${t.upstreamRust}.
AGENTS.md Porting Conventions are binding (header "// Port of ${t.upstreamRust}", preserve names, carry docs). It must satisfy what Extras needs: ${t.plan}
HARD: no stubs, no NotImplementedException, no TODO/placeholder. If upstreamRust is "none", reconstruct a faithful type from the upstream concept and Extras' usage. New .cs files are auto-included by the csproj glob. Return a one-line summary.`,
  { label: `report:${t.name}`, phase: 'ReportTypes', model: 'sonnet' },
)))

// gate: Runtime must build before fixing Extras
const rtBuild = await agent(
  `Repo root ${ROOT}. Run: dotnet build src/FrankenTui.Runtime/FrankenTui.Runtime.csproj --nologo -v q . Report errorCount and the first line of each remaining error. If there are errors, fix them faithfully (the just-added types ${analysis.deletedTypes.map(t=>t.name).join(', ')} may need wiring) — no stubs — and rebuild. Set filesChanged accordingly.`,
  { label: 'runtime-build-gate', phase: 'ReportTypes', schema: BUILD_SCHEMA, model: 'sonnet' },
)
log(`Runtime build after re-port: ${rtBuild.errorCount} error(s).`)

// ---- Phase 3: Iterative build-fix loop for Extras ----
phase('FixExtras')
const maxIter = 7
let pass = 0, last = { errorCount: 999 }
while (pass < maxIter) {
  pass++
  last = await agent(
    `Repo root ${ROOT}. Make src/FrankenTui.Extras compile FAITHFULLY (this is pass ${pass}).
Current IWidget/IRuntimeView contract Extras must satisfy:
${analysis.iwidgetContract}

Steps: run \`dotnet build src/FrankenTui.Extras/FrankenTui.Extras.csproj --nologo -v q\`, read the errors, and fix as many as you can THIS PASS by adapting Extras widgets to the new API:
- Implement IWidget.Render(Rect, Frame) properly for each widget (translate its existing rendering/logic — do NOT leave an empty body, do NOT throw NotImplementedException).
- Replace references to re-ported types (RuntimeFrameStats, RuntimeInputEnvelope, WidgetInputState, WidgetFlowDirection) with the now-existing FrankenTui.Runtime types; add usings as needed.
- Rebuild to confirm your fixes reduced the error count.
ABSOLUTELY NO stubbing to force a green build: no empty Render bodies, no NotImplementedException, no TODO/placeholder, no commenting-out of logic. If a fix needs real work, do the real work.
Report errorCount remaining after your rebuild, remainingErrors, whether you stubbedAnything, and filesChanged.`,
    { label: `fix-extras#${pass}`, phase: 'FixExtras', schema: BUILD_SCHEMA, model: 'sonnet' },
  )
  log(`FixExtras pass ${pass}: ${last.errorCount} error(s) remaining${last.stubbedAnything ? ' ⚠ STUBBED' : ''}.`)
  if (last.errorCount === 0) break
}

// ---- Phase 4: Iterative build-fix loop for Tests.Headless ----
phase('FixTests')
let tpass = 0, tlast = { errorCount: 999 }
if (last.errorCount === 0) {
  while (tpass < 5) {
    tpass++
    tlast = await agent(
      `Repo root ${ROOT}. Make tests/FrankenTui.Tests.Headless compile (pass ${tpass}).
Run \`dotnet build tests/FrankenTui.Tests.Headless/FrankenTui.Tests.Headless.csproj --nologo -v q\` and fix compile errors faithfully:
- For 'inaccessible due to protection level' errors where a TEST legitimately needs an internal member, add [assembly: InternalsVisibleTo("FrankenTui.Tests.Headless")] to the OWNING production assembly (create/extend an AssemblyInfo.cs or Directory.Build.props in that src project) — do NOT change real members to public unless upstream has them public.
- For ambiguous 'Buffer' references, add 'using Buffer = FrankenTui.Render.Buffer;' to the test file (match existing convention).
- Do NOT delete or weaken test assertions to make them pass; only fix genuine compile breakage. NO stubbing.
Report errorCount, remainingErrors, stubbedAnything, filesChanged.`,
      { label: `fix-tests#${tpass}`, phase: 'FixTests', schema: BUILD_SCHEMA, model: 'sonnet' },
    )
    log(`FixTests pass ${tpass}: ${tlast.errorCount} error(s) remaining${tlast.stubbedAnything ? ' ⚠ STUBBED' : ''}.`)
    if (tlast.errorCount === 0) break
  }
} else {
  log(`Skipping FixTests: Extras still has ${last.errorCount} error(s).`)
}

// ---- Phase 5: Oversight for stubs ----
phase('Verify')
const verify = await agent(
  `Repo root ${ROOT}. OVERSIGHT: an automated loop just fixed compile errors in src/FrankenTui.Extras to adapt it to the new IWidget.Render(Rect, Frame) API. Audit the Extras widgets (especially every Render(Rect, Frame) implementation and any member touching the re-ported RuntimeFrameStats/RuntimeInputEnvelope/WidgetInputState/WidgetFlowDirection types) for SHORTCUTS that were used merely to force a green build: empty/no-op Render bodies, NotImplementedException, TODO/FIXME, 'placeholder', commented-out logic, or a Render that ignores its widget's actual content. List each as file:member in stubs. clean=true only if none found. Read what you need; do not modify files.`,
  { label: 'verify-stubs', phase: 'Verify', schema: VERIFY_SCHEMA, model: 'opus', agentType: 'Explore' },
)

return {
  extrasErrors: last.errorCount,
  testsErrors: tlast.errorCount,
  extrasStubbed: last.stubbedAnything,
  reportedTypes: analysis.deletedTypes.map(t => t.name),
  runtimeGate: rtBuild.errorCount,
  oversight: verify,
}
