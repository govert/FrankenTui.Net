import re, os, datetime

def parse_index(path):
    results = {}
    with open(path, 'r', encoding='utf-8-sig') as f:
        lines = f.readlines()
    for line in lines:
        line = line.strip()
        if not line.startswith('| ') or '---' in line or 'Screen' in line.split('|')[1]:
            continue
        parts = [p.strip() for p in line.split('|')]
        if len(parts) < 10: continue
        try:
            num = int(parts[1].split()[0])
            exact = parts[3]
            equal_r = int(parts[4])
            diff_r = int(parts[5])
            up_chars = int(parts[6])
            loc_chars = int(parts[7])
            ratio = float(parts[8])
            results[num] = (exact, equal_r, diff_r, up_chars, loc_chars, ratio)
        except (ValueError, IndexError):
            continue
    return results

r80 = parse_index('artifacts/showcase-compare/index.md')
r120 = parse_index('artifacts/showcase-compare-120x40/index.md')

titles = {1:'Guided Tour',2:'Dashboard',3:'Shakespeare',4:'Code Explorer',
          5:'Widget Gallery',6:'Layout Lab',7:'Forms & Input',8:'Data Viz',
          9:'File Browser',10:'Adv Features',11:'Table Theme Gallery',12:'Term Capabilities',
          13:'Macro Recorder',14:'Performance',15:'Markdown Rich Text',16:'Mermaid Showcase',
          17:'Mermaid Mega',18:'Visual Effects',19:'Responsive Layout',20:'Log Search',
          21:'Notifications',22:'Action Timeline',23:'Intrinsic Sizing',24:'Layout Inspector',
          25:'Adv Text Editor',26:'Mouse Playground',27:'Form Validation',28:'Virtualized Search',
          29:'Async Tasks',30:'Theme Studio',31:'Snapshot Player',32:'Performance HUD',
          33:'Explainability',34:'i18n Stress Lab',35:'VOI Overlay',36:'Inline Mode Story',
          37:'Accessibility',38:'Widget Builder',39:'Command Palette',40:'Determinism Lab',
          41:'Hyperlink Playground',42:'Kanban Board',43:'Markdown Live Editor',44:'Drag & Drop',
          45:'Quake E1M1'}

def ratio_band(r):
    if r < 0.7: return '<< Rust'
    if r < 0.9: return '< Rust'
    if r <= 1.1: return '~parity'
    if r <= 1.5: return '> Rust'
    return '>> Rust'

lines = []
lines.append('# FrankenTui Showcase Comparison -- Multi-Resolution Report')
lines.append('')
lines.append('## Summary')
lines.append('')
exact80 = sum(1 for k,v in r80.items() if v[0]=='yes')
exact120 = sum(1 for k,v in r120.items() if v[0]=='yes')
lines.append(f'| Resolution | Exact matches | Total screens | Match rate |')
lines.append(f'| ---: | ---: | ---: | ---: |')
lines.append(f'| 80x24 | {exact80} | {len(r80)} | {exact80/len(r80)*100:.1f}% |')
lines.append(f'| 120x40 | {exact120} | {len(r120)} | {exact120/len(r120)*100:.1f}% |')
lines.append('')

lines.append('## Content Ratio Comparison (char-count local/upstream)')
lines.append('')
lines.append(f'| # | Screen | 80x24 ratio | 80x24 band | 120x40 ratio | 120x40 band |')
lines.append(f'| ---: | --- | ---: | --- | ---: | --- |')

for i in range(1,46):
    r8 = r80.get(i, ('-',0,0,0,0,0))
    r12 = r120.get(i, ('-',0,0,0,0,0))
    lines.append(f'| {i} | {titles[i]} | {r8[6]:.3f} | {ratio_band(r8[6])} | {r12[6]:.3f} | {ratio_band(r12[6])} |')

lines.append('')
lines.append('## Key Observations')
lines.append('')
lines.append(f'- **0 exact matches** at either resolution -- all {len(r80)} screens render differently')
vals80 = [v[6] for v in r80.values()]
vals120 = [v[6] for v in r120.values()]
lines.append(f'- **80x24 ratio range**: {min(vals80):.3f} -- {max(vals80):.3f}')
lines.append(f'- **120x40 ratio range**: {min(vals120):.3f} -- {max(vals120):.3f}')
lt80 = sum(1 for v in vals80 if v < 1.0)
gt80 = sum(1 for v in vals80 if v > 1.0)
lt120 = sum(1 for v in vals120 if v < 1.0)
gt120 = sum(1 for v in vals120 if v > 1.0)
lines.append(f'- **80x24**: {lt80} screens less content, {gt80} more content than Rust')
lines.append(f'- **120x40**: {lt120} screens less content, {gt120} more content than Rust')
lines.append('')
lines.append('### Root causes of diffs')
lines.append('')
lines.append('1. **Different box-drawing chars**: Rust uses rounded corners, .NET uses sharp corners')
lines.append('2. **Different nav bar formatting**: Rust uses pipe separators, .NET uses spaces')
lines.append('3. **Different screen generators**: ShowcaseViewFactory.Build() maps to .NET-specific content')
lines.append('4. **Different defaults and field sets**: Each port has its own set of form fields per screen')
lines.append('')
lines.append(f'Generated: {datetime.datetime.utcnow().strftime("%Y-%m-%d %H:%M:%S UTC")}')

with open('artifacts/showcase-compare/MULTI-RESOLUTION-REPORT.md', 'w', encoding='utf-8') as f:
    f.write('\n'.join(lines))

print(f'Wrote {len(lines)} lines to MULTI-RESOLUTION-REPORT.md')
