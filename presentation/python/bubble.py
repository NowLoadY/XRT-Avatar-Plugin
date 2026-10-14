"""Plain-text bubble layout shared by native text adapters."""
import json
import math
from pathlib import Path
import re

STYLE = json.loads((Path(__file__).resolve().parents[1] / 'bubble-style.json').read_text())


def _mark(code):
    return (0x300 <= code <= 0x36F or 0x1AB0 <= code <= 0x1AFF or
            0x3099 <= code <= 0x309A or 0x1DC0 <= code <= 0x1DFF or 0x20D0 <= code <= 0x20FF or
            0xFE00 <= code <= 0xFE0F or 0xFE20 <= code <= 0xFE2F or
            0x1F3FB <= code <= 0x1F3FF or 0xE0100 <= code <= 0xE01EF)


def _wide(code):
    return code >= 0x1100 and (code <= 0x115F or code in (0x2329, 0x232A) or
        0x2E80 <= code <= 0xA4CF or 0xAC00 <= code <= 0xD7A3 or
        0xF900 <= code <= 0xFAFF or 0xFE10 <= code <= 0xFE6F or
        0xFF01 <= code <= 0xFF60 or 0xFFE0 <= code <= 0xFFE6 or code >= 0x1F000)


def _clusters(text):
    result = []
    join = regional = False
    for char in text:
        code = ord(char)
        if 0xD800 <= code <= 0xDFFF:
            continue
        if code == 0x200D:
            if result:
                result[-1][0] += char
                join = True
            continue
        flag = 0x1F1E6 <= code <= 0x1F1FF
        width = 2 if _wide(code) else 1
        if result and (_mark(code) or join or (flag and regional)):
            result[-1][0] += char
            result[-1][1] = max(result[-1][1], width)
        else:
            result.append([char, width])
        join = False
        regional = flag and not regional
    return result


def _width(line):
    return sum(item[1] for item in line)


def _trim(line):
    while line and line[0][0] == ' ':
        line.pop(0)
    while line and line[-1][0] == ' ':
        line.pop()
    return line


def layout(value, max_bytes=None, max_columns=None):
    columns = (max(1, min(STYLE['maxColumns'], math.floor(max_columns)))
               if isinstance(max_columns, (int, float)) and math.isfinite(max_columns)
               else STYLE['maxColumns'])
    source = '' if value is None else str(value)
    source = re.sub(r'[\x00-\x08\x0b\x0c\x0e-\x1f\x7f\u200b\u200e\u200f\u202a-\u202e\u2066-\u2069]', '', source[:8192])
    source = re.sub(r'\s+', ' ', source).strip()
    queue, lines, line = _clusters(source), [], []
    clipped = False
    while queue:
        item = queue.pop(0)
        if _width(line) + item[1] > columns:
            if not line:
                clipped = True
                break
            boundary = next((i for i in range(len(line) - 1, -1, -1)
                if line[i][0] == ' ' or (line[i][0][-1:] in '。！？、，；：'
                    and _width(line[:i + 1]) >= columns / 3)), -1)
            carry = line[boundary + 1:] if boundary > 0 else []
            if boundary > 0:
                line = line[:boundary + 1]
            lines.append(_trim(line))
            line = []
            queue = carry + [item] + queue
            if len(lines) == STYLE['maxLines']:
                clipped = True
                break
        elif line or item[0] != ' ':
            line.append(item)
    if line and len(lines) < STYLE['maxLines']:
        lines.append(_trim(line))
    if clipped:
        if not lines:
            lines.append([])
        line = lines[-1]
        while line and _width(line) + 1 > columns:
            line.pop()
        _trim(line).append(['…', 1])
    text = '\n'.join(''.join(item[0] for item in row) for row in lines)
    if isinstance(max_bytes, (int, float)) and math.isfinite(max_bytes):
        limit = max(0, math.floor(max_bytes))
        if len(text.encode('utf-8')) > limit:
            kept, used = [], 0
            for item in _clusters(text):
                size = len(item[0].encode('utf-8'))
                if used + size + 3 > limit:
                    break
                kept.append(item[0])
                used += size
            rows = ''.join(kept).split('\n')
            tail = _trim(_clusters(rows.pop()))
            while tail and _width(tail) + 1 > columns:
                tail.pop()
            rows.append(''.join(item[0] for item in tail) + ('…' if limit >= 3 else ''))
            text = '\n'.join(rows)
    rendered = text.split('\n') if text else []
    widest = max((_width(_clusters(row)) for row in rendered), default=0)
    return {'text': text, 'lines': len(rendered),
            'width': min(STYLE['maxWidth'], max(STYLE['minWidth'], widest * STYLE['columnWidth'] + STYLE['padding'] * 2)),
            'height': len(rendered) * STYLE['lineHeight'] * STYLE['lineSpacing'] + STYLE['padding'] * 2}


def lifetime(message):
    value = message.get('expires_ms') if isinstance(message, dict) else None
    return max(0, min(STYLE['maxLifetimeMs'], value)) if type(value) in (int, float) and math.isfinite(value) else 0
