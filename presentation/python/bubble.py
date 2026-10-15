"""Plain-text bubble layout shared by native text adapters."""
import json
import math
from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]
STYLE = json.loads((ROOT / 'bubble-style.json').read_text())
PROFILES = {name: dict(STYLE, **preset) for name, preset in json.loads((ROOT / 'presets.json').read_text()).items()}


def source_text(value, max_bytes=None):
    source = '' if value is None else str(value)
    text = re.sub(r'[\x00-\x08\x0b\x0c\x0e-\x1f\x7f\u200b\u200e\u200f\u202a-\u202e\u2066-\u2069\ud800-\udfff]', '', source[:8192]).strip()
    if type(max_bytes) in (int, float) and math.isfinite(max_bytes):
        used, kept = 0, []
        for cluster, _ in _clusters(text):
            size = len(cluster.encode('utf-8'))
            if used + size > max(0, max_bytes):
                break
            kept.append(cluster)
            used += size
        return ''.join(kept)
    return text


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


def layout(value, max_bytes=None, max_columns=None, profile='horizontal', max_lines=None):
    selected = PROFILES.get(profile, PROFILES['horizontal'])
    row_limit = max(1, min(12, math.floor(max_lines))) if type(max_lines) in (int, float) and math.isfinite(max_lines) else selected['maxLines']
    columns = (max(1, min(120, math.floor(max_columns)))
               if isinstance(max_columns, (int, float)) and math.isfinite(max_columns)
               else selected['maxColumns'])
    source = re.sub(r'[^\S\n]+', ' ', re.sub(r'\r\n?', '\n', source_text(value)))
    queue, lines, line = _clusters(source), [], []
    clipped = False
    while queue:
        item = queue.pop(0)
        if item[0] == '\n':
            lines.append(_trim(line))
            line = []
            if len(lines) == row_limit:
                clipped = bool(queue)
                break
        elif _width(line) + item[1] > columns:
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
            if len(lines) == row_limit:
                clipped = True
                break
        elif line or item[0] != ' ':
            line.append(item)
    if line and len(lines) < row_limit:
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
            'width': min(selected['width'], max(min(STYLE['minWidth'], selected['width']), widest * selected['columnWidth'] + selected['padding'] * 2)),
            'height': len(rendered) * selected['lineHeight'] * selected['lineSpacing'] + selected['padding'] * 2}


def lifetime(message):
    value = message.get('expires_ms') if isinstance(message, dict) else None
    return max(0, min(STYLE['maxLifetimeMs'], value)) if type(value) in (int, float) and math.isfinite(value) else 0


class CaptionSource:
    """Current plain-text result, independent of a display's layout and placement."""
    def __init__(self):
        self.reset()

    def reset(self):
        self.revisions = {}
        self.identity = None
        self.display_mode = False
        self.display_revision = -1
        self.clear()

    def clear(self):
        self.value = None
        self.header = self.footer = None
        self.messages = []

    def accept_display(self, message, now):
        revision = message.get('revision')
        if (type(revision) is not int or not self.display_revision < revision <= 9007199254740991
                or any(key not in message or (message[key] is not None and not isinstance(message[key], str))
                       for key in ('header', 'footer'))
                or not isinstance(message.get('messages'), list) or len(message['messages']) > 8):
            return False
        ids, messages = set(), []
        for item in message['messages']:
            if (not isinstance(item, dict) or not isinstance(item.get('id'), str) or not item['id']
                    or item['id'] in ids or not isinstance(item.get('text'), str)
                    or type(item.get('ttl_ms')) not in (int, float)
                    or not math.isfinite(item['ttl_ms']) or item['ttl_ms'] < 0):
                return False
            ids.add(item['id'])
            text = source_text(item['text'])[:256]
            if text and item['ttl_ms'] > 0:
                messages.append(dict(id=item['id'], text=text, expiresAt=now + min(15000, item['ttl_ms'])))
        self.display_mode = True
        self.display_revision = revision
        self.header = source_text(message.get('header'))[:128] or None
        self.messages = messages
        self.footer = source_text(message.get('footer'))[:128] or None
        self.value = None
        return True

    def accept(self, message, now):
        if type(now) not in (int, float) or not math.isfinite(now):
            return False
        if isinstance(message, dict) and message.get('type') == 'display':
            return self.accept_display(message, now)
        if self.display_mode:
            return False
        if (not isinstance(message, dict) or not all(isinstance(message.get(k), str) for k in ('text', 'stream', 'id'))
                or type(message.get('revision')) is not int or message['revision'] < 0
                or type(now) not in (int, float) or not math.isfinite(now)):
            return False
        key = (message['stream'], message['id'])
        if key in self.revisions and (key != self.identity or message['revision'] <= self.revisions[key]):
            return False
        self.revisions[key] = message['revision']
        self.identity = key
        if len(self.revisions) > 64:
            del self.revisions[next(iter(self.revisions))]
        text, duration = source_text(message['text']), lifetime(message)
        self.value = dict(text=text, stream=message['stream'], id=message['id'],
                          revision=message['revision'], expiresAt=now + duration) if text and duration > 0 else None
        return True

    def snapshot(self, now, region='full'):
        if self.display_mode:
            self.messages = [item for item in self.messages if item['expiresAt'] > now]
            messages = [dict(item) for item in self.messages]
            body = '\n'.join(item['text'] for item in messages)
            text = {'header': self.header, 'messages': body, 'footer': self.footer}.get(region)
            if region not in ('header', 'messages', 'footer'):
                text = '\n'.join(part for part in (self.header, body, self.footer) if part)
            return dict(text=text or '', header=self.header, messages=messages, footer=self.footer,
                        revision=self.display_revision)
        if region in ('header', 'footer'):
            return None
        if self.value and now >= self.value['expiresAt']:
            self.clear()
        return dict(self.value) if self.value else None
