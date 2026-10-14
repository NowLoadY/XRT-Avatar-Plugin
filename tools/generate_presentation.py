#!/usr/bin/env python3
"""Keep the portable JavaScript bubble style in sync with its JSON source."""
import argparse
import json
from pathlib import Path
import re

root = Path(__file__).resolve().parents[1]
parser = argparse.ArgumentParser(description=__doc__)
parser.add_argument('--check', action='store_true')
args = parser.parse_args()
style = json.loads((root / 'presentation/bubble-style.json').read_text())
path = root / 'presentation/javascript/bubble.js'
source = path.read_text()
pattern = r'/\* style:start \*/.*?/\* style:end \*/'
updated, count = re.subn(pattern, lambda _: '/* style:start */' + json.dumps(style, separators=(',', ':')) + '/* style:end */', source, flags=re.S)
if count != 1:
    raise SystemExit('Expected one generated bubble style block')
if args.check:
    if updated != source:
        raise SystemExit('Run python3 tools/generate_presentation.py')
else:
    path.write_text(updated)
