/* Interface script: bind any source region to your own renderer. */
(function () {
    "use strict";
    var Bubble = Script.require(Script.resolvePath("../../../presentation/javascript/bubble.js"));
    var source = new Bubble.CaptionSource();
    var channel = "XRT/CaptionSource";
    var region = "full"; // full, header, messages or footer
    var lastText = "";
    var generation = null;

    function render(text) {
        // Bind text to your own Text entity, drawing surface or application here.
    }
    function update() {
        var value = source.snapshot(Date.now(), region), text = value ? value.text : "";
        if (text !== lastText) { lastText = text; render(text); }
    }
    function received(name, raw, sender, localOnly) {
        if (name !== channel || !localOnly || raw.length > 65536) { return; }
        var message;
        try { message = JSON.parse(raw); } catch (error) { return; }
        if (message.source !== "XRTranslate") { return; }
        if (message.generation !== generation) { source.reset(); generation = message.generation; }
        if (message.type === "clear") { source.clear(); }
        else if (message.value) {
            var value = message.value, now = Date.now();
            if (message.type === "display" && Array.isArray(value.messages)) {
                source.accept({ type: "display", revision: value.revision, header: value.header, footer: value.footer,
                    messages: value.messages.map(function (item) {
                        return { id: item.id, text: item.text, ttl_ms: Math.max(0, item.expiresAt - now) };
                    }) }, now);
            } else if (message.type === "caption") {
                value.expires_ms = Math.max(0, value.expiresAt - now);
                source.accept(value, now);
            }
        }
        update();
    }
    Messages.subscribe(channel);
    Messages.messageReceived.connect(received);
    var timer = Script.setInterval(update, 100);
    Script.scriptEnding.connect(function () {
        Messages.messageReceived.disconnect(received);
        Messages.unsubscribe(channel);
        Script.clearInterval(timer);
        render("");
    });
}());
