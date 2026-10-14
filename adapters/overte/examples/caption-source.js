/* Interface script: replace the callback with your own renderer. */
(function () {
    "use strict";
    var channel = "XRT/CaptionSource";
    var expiresAt = 0;
    var lastText = "";

    function render(text) {
        // Bind text to your own Text entity, drawing surface or application here.
        // Keep the original text; apply wrapping only inside your renderer.
    }

    function update(text, expires) {
        expiresAt = expires;
        if (text !== lastText) { lastText = text; render(text); }
    }

    function received(name, raw, sender, localOnly) {
        if (name !== channel || !localOnly || raw.length > 65536) { return; }
        var message;
        try { message = JSON.parse(raw); } catch (error) { return; }
        if (message.source !== "XRTranslate") { return; }
        if (message.type === "clear") { update("", 0); }
        else if (message.type === "caption" && message.value && typeof message.value.text === "string" &&
                typeof message.value.expiresAt === "number" && message.value.expiresAt > Date.now()) {
            update(message.value.text, message.value.expiresAt);
        }
    }

    Messages.subscribe(channel);
    Messages.messageReceived.connect(received);
    var expiry = Script.setInterval(function () {
        if (expiresAt && Date.now() >= expiresAt) { update("", 0); }
    }, 100);
    Script.scriptEnding.connect(function () {
        Messages.messageReceived.disconnect(received);
        Messages.unsubscribe(channel);
        Script.clearInterval(expiry);
        render("");
    });
}());
