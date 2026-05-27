#!/usr/bin/env python3
"""Generate FrankenTUI Rust snapshots by capturing ANSI terminal output."""
import subprocess, re, os, sys, glob

SLUGS = {
    1:'guidedtour',2:'dashboard',3:'shakespeare',4:'codeexplorer',5:'widgetgallery',
    6:'layoutlab',7:'formsinput',8:'dataviz',9:'filebrowser',10:'advancedfeatures',
    11:'tablethemegallery',12:'terminalcapabilities',13:'macrorecorder',14:'performance',
    15:'markdownrichtext',16:'mermaidshowcase',17:'mermaidmegashowcase',18:'visualeffects',
    19:'responsivedemo',20:'logsearch',21:'notifications',22:'actiontimeline',
    23:'intrinsicsizing',24:'layoutinspector',25:'advancedtexteditor',26:'mouseplayground',
    27:'formvalidation',28:'virtualizedsearch',29:'asynctasks',30:'themestudio',
    31:'snapshotplayer',32:'performancehud',33:'explainabilitycockpit',34:'i18ndemo',
    35:'voioverlay',36:'inlinemodestory',37:'accessibilitypanel',38:'widgetbuilder',
    39:'commandpalettelab',40:'determinismlab',41:'hyperlinkplayground',42:'kanbanboard',
    43:'markdownliveeditor',44:'dragdrop',45:'quakeeasteregg',
}

RUST_BIN = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '.external', 'frankentui', 'target', 'release', 'ftui-demo-showcase.exe')
UPSTREAM_DIR = 'artifacts/showcase-compare/upstream'

def capture_ansi(screen_num, width=80, height=24, delay_ms=3000):
    proc = subprocess.run(
        [RUST_BIN, f'--screen={screen_num}', '--mouse=off'],
        capture_output=True, timeout=delay_ms//1000 + 5,
        cwd='.external/frankentui',
        encoding='utf-8', errors='replace',
        env={**os.environ, 'FTUI_DEMO_EXIT_AFTER_MS': str(delay_ms),
             'LINES': str(height), 'COLUMNS': str(width)})
    return proc.stdout

def ansi_to_grid(data, width=80, height=24):
    """State-machine ANSI parser: track cursor and place chars in grid."""
    grid = [[' ' for _ in range(width)] for _ in range(height)]
    row, col = 0, 0
    i, n = 0, len(data)
    
    while i < n:
        ch = data[i]
        if ch == '\x1b' and i+1 < n and data[i+1] == '[':
            j = i + 2
            while j < n and data[j] not in 'ABCDEFGHJKSTfmnrsuplh': j += 1
            if j < n:
                params = data[i+2:j]; final = data[j]
                if final == 'H':
                    parts = params.split(';')
                    if len(parts) == 2:
                        try: row = max(0, int(parts[0])-1); col = max(0, int(parts[1])-1)
                        except: pass
                elif final == 'A': row = max(0, row - (int(params) if params else 1))
                elif final == 'B': row = min(height-1, row + (int(params) if params else 1))
                elif final == 'C': col = min(width-1, col + (int(params) if params else 1))
                elif final == 'J' and params == '2':
                    grid = [[' ' for _ in range(width)] for _ in range(height)]
                    row = col = 0
                i = j + 1
                continue
        if ch == '\n': row = min(height-1, row+1); col = 0
        elif ch == '\r': col = 0
        elif ord(ch) >= 32:
            if 0 <= row < height and 0 <= col < width:
                grid[row][col] = ch
            col += 1
            if col >= width: col = 0; row = min(height-1, row+1)
        i += 1
    return grid

def write_snapshot(screen, width=80, height=24):
    data = capture_ansi(screen, width, height)
    grid = ansi_to_grid(data, width, height)
    # Don't rstrip — preserve all border characters and positioning
    result = '\n'.join(''.join(r) for r in grid)
    
    os.makedirs(UPSTREAM_DIR, exist_ok=True)
    slug = SLUGS.get(screen, f'screen{screen}')
    path = os.path.join(UPSTREAM_DIR, f'app_{slug}_{width}x{height}.upstream.snap')
    with open(path, 'w', encoding='utf-8', newline='') as f:
        f.write(result.rstrip('\n'))
    
    chars = sum(1 for c in result if not c.isspace())
    empty_rows = sum(1 for r in grid if all(c == ' ' for c in r))
    return screen, chars, empty_rows, path

if __name__ == '__main__':
    screens = range(1, 46)
    if len(sys.argv) > 1:
        parts = sys.argv[1].split('-')
        if len(parts) == 2:
            screens = range(int(parts[0]), int(parts[1])+1)
        else:
            screens = [int(sys.argv[1])]
    
    total_chars = 0
    for s in screens:
        num, chars, empty, path = write_snapshot(s)
        total_chars += chars
        status = 'OK' if empty < 3 else f'WARN:{empty} empty rows'
        print(f'  Screen {num:2d}: {chars:4d} non-space chars, {status} -> {os.path.basename(path)}')
    print(f'Total: {total_chars} non-space chars across {len(list(screens))} screens')
