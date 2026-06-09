export const meta = {
  name: 'fix-build-green',
  description: 'Generic faithful build-fix loop: drive a set of projects to 0 compile errors after an API refactor, then oversight for stubs',
  phases: [
    { title: 'Analyze', detail: 'Capture current API contracts + collect compile errors' },
    { title: 'Fix', detail: 'Iterative faithful build-fix loop (no stubs)' },
    { title: 'Verify', detail: 'Oversight scan for stubs/shortcuts' },
  ],
}

const ROOT = 'C:\\\\Work\\\\FrankenTui.Net'
const A = (typeof args === 'string' ? (args ? JSON.parse(args) : {}) : (args || {}))
const projects = A.projects || [
  'tools/FrankenTui.Doctor/FrankenTui.Doctor.csproj',
  'tools/FrankenTui.ShowcaseCompare/FrankenTui.ShowcaseCompare.csproj',
  'apps/FrankenTui.Showcase.Wasm/FrankenTui.Showcase.Wasm.csproj',
]
const maxIter = A.maxIter || 8
const buildCmds = projects.map(p => `dotnet build ${p} --nologo -v q`).join('\n  ')

const ANALYZE_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['contracts', 'errorCount', 'errorSummary', 'notes'],
  properties: {
    contracts: { type: 'string', description: 'verbatim current signatures these projects must satisfy: IWidget, IRuntimeView, the Ui facade members they call, and which widget types (StatusWidget/PaddingWidget/etc.) now exist or what replaced them' },
    errorCount: { type: 'integer' },
    errorSummary: { type: 'array', items: { type: 'string' }, description: 'first line of each distinct compile error' },
    notes: { type: 'string' },
  },
}
const BUILD_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['errorCount', 'remainingErrors', 'stubbedAnything', 'filesChanged', 'notes'],
  properties: {
    errorCount: { type: 'integer' },
    remainingErrors: { type: 'array', items: { type: 'string' } },
    stubbedAnything: { type: 'boolean' },
    filesChanged: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
  },
}
const VERIFY_SCHEMA = {
  type: 'object', additionalProperties: false,
  required: ['clean', 'stubs', 'notes'],
  properties: {
    clean: { type: 'boolean' },
    stubs: { type: 'array', items: { type: 'string' } },
    notes: { type: 'string' },
  },
}

phase('Analyze')
const analysis = await agent(
  `Repo root ${ROOT}. These projects no longer compile after a Runtime/Widgets API refactor:
${projects.map(p => '  - ' + p).join('\n')}
Build each and collect the distinct errors:
  ${buildCmds}
Then read the CURRENT contracts these projects must satisfy and capture them verbatim into 'contracts': the IWidget interface (note Render(Rect, Frame)), IRuntimeView, the Ui facade (which members exist now, e.g. whether RenderHostedParity exists), and which widget types referenced by the errors (StatusWidget, PaddingWidget, TabsWidget.FocusedIndex, etc.) still exist or what replaced them. Return structured analysis. Do not modify files.`,
  { label: 'analyze', phase: 'Analyze', schema: ANALYZE_SCHEMA, model: 'opus' },
)
log(`Analyze: ${analysis.errorCount} error(s) across ${projects.length} project(s).`)

phase('Fix')
let pass = 0, last = { errorCount: analysis.errorCount || 999 }
while (pass < maxIter && last.errorCount !== 0) {
  pass++
  last = await agent(
    `Repo root ${ROOT}. Make these projects compile FAITHFULLY (pass ${pass}):
${projects.map(p => '  - ' + p).join('\n')}
Current API contracts to satisfy:
${analysis.contracts}

Build them (${buildCmds}), read errors, and fix as many as you can by adapting the CALLING code to the new API:
- IWidget.Render now takes (Rect, Frame) — update call sites and any inline IWidget usage; where code passed an IWidget to something wanting IRuntimeView, adapt correctly (use the right type/wrapper that exists now).
- For missing widget types (StatusWidget, PaddingWidget, etc.) or facade members (Ui.RenderHostedParity), use the current replacement that exists in src — find it, don't invent. If genuinely removed with no replacement, adapt the caller to the new pattern rather than stubbing.
- For changed members (TabsWidget.FocusedIndex, CaptureAsync 'policy' param), use the current API shape.
Rebuild to confirm error count dropped. NO stubbing to force green: no NotImplementedException, no empty bodies replacing real logic, no commenting-out of features, no deleting functionality. Do the real adaptation.
Report errorCount remaining after rebuild, remainingErrors, stubbedAnything, filesChanged.`,
    { label: `fix#${pass}`, phase: 'Fix', schema: BUILD_SCHEMA, model: 'sonnet' },
  )
  log(`Fix pass ${pass}: ${last.errorCount} error(s) remaining${last.stubbedAnything ? ' ⚠ STUBBED' : ''}.`)
}

phase('Verify')
const verify = await agent(
  `Repo root ${ROOT}. OVERSIGHT: an automated loop just adapted these projects to the new API: ${projects.join(', ')}. Audit the changes (git diff is available via bash) for SHORTCUTS used merely to force a green build: NotImplementedException, empty bodies replacing real logic, commented-out features, deleted functionality, TODO/FIXME/placeholder. List each as file:member in stubs. clean=true only if none. Do not modify files.`,
  { label: 'verify-stubs', phase: 'Verify', schema: VERIFY_SCHEMA, model: 'opus', agentType: 'Explore' },
)

return { projects, finalErrors: last.errorCount, stubbed: last.stubbedAnything, oversight: verify }
