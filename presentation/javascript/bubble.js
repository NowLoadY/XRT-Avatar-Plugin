/* Plain-text bubble layout shared by game adapters and the visual preview. */
(function (root, factory) {
    if (typeof module !== "undefined" && module.exports) { module.exports = factory(); }
    else { root.XrtBubble = factory(); }
}(this, function () {
    "use strict";
    // Generated style: edit presentation/bubble-style.json.
    var style = /* style:start */{"maxLines":3,"maxColumns":42,"lineHeight":0.043,"lineSpacing":1.25,"padding":0.025,"minWidth":0.44,"maxWidth":0.9,"columnWidth":0.019,"textColor":{"red":30,"green":36,"blue":40},"textAlpha":1,"backgroundColor":{"red":242,"green":244,"blue":244},"backgroundAlpha":0.9,"fadeInMs":160,"fadeOutMs":220,"maxLifetimeMs":8000}/* style:end */;

    var profiles = /* profiles:start */{"horizontal":{"maxLines":3,"maxColumns":42,"lineHeight":0.043,"lineSpacing":1.25,"padding":0.025,"minWidth":0.44,"maxWidth":0.9,"columnWidth":0.019,"textColor":{"red":30,"green":36,"blue":40},"textAlpha":1,"backgroundColor":{"red":242,"green":244,"blue":244},"backgroundAlpha":0.9,"fadeInMs":160,"fadeOutMs":220,"maxLifetimeMs":8000,"label":"Bubble","width":0.9,"height":0.24,"fontFamily":"sans"},"vertical":{"maxLines":8,"maxColumns":16,"lineHeight":0.043,"lineSpacing":1.25,"padding":0.025,"minWidth":0.44,"maxWidth":0.9,"columnWidth":0.019,"textColor":{"red":30,"green":36,"blue":40},"textAlpha":1,"backgroundColor":{"red":242,"green":244,"blue":244},"backgroundAlpha":0.9,"fadeInMs":160,"fadeOutMs":220,"maxLifetimeMs":8000,"label":"Vertical","width":0.36,"height":0.56,"fontFamily":"sans"},"board":{"maxLines":5,"maxColumns":36,"lineHeight":0.043,"lineSpacing":1.25,"padding":0.025,"minWidth":0.44,"maxWidth":0.9,"columnWidth":0.019,"textColor":{"red":30,"green":36,"blue":40},"textAlpha":1,"backgroundColor":{"red":242,"green":244,"blue":244},"backgroundAlpha":0,"fadeInMs":160,"fadeOutMs":220,"maxLifetimeMs":8000,"label":"Board","width":0.8,"height":0.38,"fontFamily":"handwritten"}}/* profiles:end */;

    function sourceText(value, maxBytes) {
        var text = points(String(value == null ? "" : value).slice(0, 8192)
            .replace(/[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F\u200B\u200E\u200F\u202A-\u202E\u2066-\u2069]/g, ""))
            .map(function (item) { return item.text; }).join("").trim();
        if (typeof maxBytes === "number" && isFinite(maxBytes) && bytes(text) > maxBytes) {
            var used = 0, kept = [];
            clusters(text).some(function (item) {
                var size = bytes(item.text);
                if (used + size > Math.max(0, maxBytes)) { return true; }
                kept.push(item.text); used += size; return false;
            });
            return kept.join("");
        }
        return text;
    }
    function points(text) {
        var result = [], i, first, second;
        for (i = 0; i < text.length; i++) {
            first = text.charCodeAt(i);
            if (first >= 0xD800 && first <= 0xDBFF && i + 1 < text.length) {
                second = text.charCodeAt(i + 1);
                if (second >= 0xDC00 && second <= 0xDFFF) {
                    result.push({ text: text.slice(i, i + 2), code: 0x10000 + (first - 0xD800) * 1024 + second - 0xDC00 });
                    i++; continue;
                }
            }
            if (first < 0xD800 || first > 0xDFFF) { result.push({ text: text.charAt(i), code: first }); }
        }
        return result;
    }
    function mark(code) {
        return (code >= 0x300 && code <= 0x36F) || (code >= 0x1AB0 && code <= 0x1AFF) ||
            (code >= 0x3099 && code <= 0x309A) || (code >= 0x1DC0 && code <= 0x1DFF) || (code >= 0x20D0 && code <= 0x20FF) ||
            (code >= 0xFE00 && code <= 0xFE0F) || (code >= 0xFE20 && code <= 0xFE2F) ||
            (code >= 0x1F3FB && code <= 0x1F3FF) || (code >= 0xE0100 && code <= 0xE01EF);
    }
    function wide(code) {
        return code >= 0x1100 && (code <= 0x115F || code === 0x2329 || code === 0x232A ||
            (code >= 0x2E80 && code <= 0xA4CF) || (code >= 0xAC00 && code <= 0xD7A3) ||
            (code >= 0xF900 && code <= 0xFAFF) || (code >= 0xFE10 && code <= 0xFE6F) ||
            (code >= 0xFF01 && code <= 0xFF60) || (code >= 0xFFE0 && code <= 0xFFE6) ||
            code >= 0x1F000);
    }
    function clusters(text) {
        var result = [], join = false, regional = false;
        points(text).forEach(function (point) {
            var code = point.code;
            if (code === 0x200D) {
                if (result.length) { result[result.length - 1].text += point.text; join = true; }
                return;
            }
            var flag = code >= 0x1F1E6 && code <= 0x1F1FF;
            if (result.length && (mark(code) || join || (flag && regional))) {
                result[result.length - 1].text += point.text;
                result[result.length - 1].width = Math.max(result[result.length - 1].width, wide(code) ? 2 : 1);
            } else { result.push({ text: point.text, width: wide(code) ? 2 : 1 }); }
            join = false;
            regional = flag && !regional;
        });
        return result;
    }
    function width(line) { return line.reduce(function (sum, item) { return sum + item.width; }, 0); }
    function trim(line) {
        while (line.length && line[0].text === " ") { line.shift(); }
        while (line.length && line[line.length - 1].text === " ") { line.pop(); }
        return line;
    }
    function bytes(text) {
        return points(text).reduce(function (sum, point) {
            return sum + (point.code < 0x80 ? 1 : point.code < 0x800 ? 2 : point.code < 0x10000 ? 3 : 4);
        }, 0);
    }
    function layout(value, options) {
        options = options || {};
        var selected = profiles[options.profile || "horizontal"] || profiles.horizontal;
        var maxLines = typeof options.maxLines === "number" && isFinite(options.maxLines)
            ? Math.max(1, Math.min(12, Math.floor(options.maxLines))) : selected.maxLines;
        var columns = typeof options.maxColumns === "number" && isFinite(options.maxColumns)
            ? Math.max(1, Math.min(120, Math.floor(options.maxColumns))) : selected.maxColumns;
        var source = sourceText(value).replace(/\s+/g, " ");
        var queue = clusters(source), lines = [], line = [], clipped = false;
        while (queue.length) {
            var item = queue.shift();
            if (width(line) + item.width > columns) {
                if (!line.length) { clipped = true; break; }
                var boundary = -1, j;
                for (j = line.length - 1; j >= 0; j--) {
                    if (line[j].text === " " || (/[。！？、，；：]$/.test(line[j].text) &&
                            width(line.slice(0, j + 1)) >= columns / 3)) { boundary = j; break; }
                }
                var carry = boundary > 0 ? line.splice(boundary + 1) : [];
                lines.push(trim(line)); line = [];
                queue = carry.concat([item], queue);
                if (lines.length === maxLines) { clipped = true; break; }
            } else if (line.length || item.text !== " ") { line.push(item); }
        }
        if (line.length && lines.length < maxLines) { lines.push(trim(line)); }
        if (clipped) {
            if (!lines.length) { lines.push([]); }
            line = lines[lines.length - 1];
            while (line.length && width(line) + 1 > columns) { line.pop(); }
            trim(line).push({ text: "…", width: 1 });
        }
        var text = lines.map(function (items) { return items.map(function (item) { return item.text; }).join(""); }).join("\n");
        if (typeof options.maxBytes === "number" && isFinite(options.maxBytes)) {
            var limit = Math.max(0, Math.floor(options.maxBytes));
            if (bytes(text) > limit) {
                var kept = [], used = 0;
                clusters(text).some(function (item) {
                    if (used + bytes(item.text) + 3 > limit) { return true; }
                    kept.push(item.text); used += bytes(item.text); return false;
                });
                var rows = kept.join("").split("\n"), tail = trim(clusters(rows.pop()));
                while (tail.length && width(tail) + 1 > columns) { tail.pop(); }
                rows.push(tail.map(function (item) { return item.text; }).join("") + (limit >= 3 ? "…" : ""));
                text = rows.join("\n");
            }
        }
        var rendered = text ? text.split("\n") : [];
        var widest = rendered.reduce(function (size, row) { return Math.max(size, width(clusters(row))); }, 0);
        return { text: text, lines: rendered.length,
            width: Math.min(selected.width, Math.max(Math.min(style.minWidth, selected.width), widest * selected.columnWidth + selected.padding * 2)),
            height: rendered.length * selected.lineHeight * selected.lineSpacing + selected.padding * 2 };
    }
    function lifetime(message) {
        return message && typeof message.expires_ms === "number" && isFinite(message.expires_ms)
            ? Math.max(0, Math.min(style.maxLifetimeMs, message.expires_ms)) : 0;
    }
    function CaptionSource() { this.reset(); }
    CaptionSource.prototype.reset = function () {
        this.revisions = Object.create(null); this.order = []; this.identity = null; this.value = null;
    };
    CaptionSource.prototype.clear = function () { this.value = null; };
    CaptionSource.prototype.accept = function (message, now) {
        if (!message || typeof message.text !== "string" || typeof message.stream !== "string" ||
                typeof message.id !== "string" || typeof message.revision !== "number" ||
                !isFinite(message.revision) || message.revision < 0 || message.revision % 1 ||
                typeof now !== "number" || !isFinite(now)) { return false; }
        var key = JSON.stringify([message.stream, message.id]);
        if (this.revisions[key] !== undefined &&
                (key !== this.identity || message.revision <= this.revisions[key])) { return false; }
        if (this.revisions[key] === undefined) { this.order.push(key); }
        this.revisions[key] = message.revision; this.identity = key;
        if (this.order.length > 64) { delete this.revisions[this.order.shift()]; }
        var text = sourceText(message.text), duration = lifetime(message);
        this.value = text && duration > 0 ? { text: text, stream: message.stream, id: message.id,
            revision: message.revision, expiresAt: now + duration } : null;
        return true;
    };
    CaptionSource.prototype.snapshot = function (now) {
        if (this.value && now >= this.value.expiresAt) { this.clear(); }
        if (!this.value) { return null; }
        var copy = {}, value = this.value;
        Object.keys(value).forEach(function (key) { copy[key] = value[key]; });
        return copy;
    };
    return { style: style, profiles: profiles, layout: layout, lifetime: lifetime,
        sourceText: sourceText, CaptionSource: CaptionSource };
}));
