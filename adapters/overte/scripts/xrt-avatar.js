(function () {
    "use strict";

    var Connector = Script.require(Script.resolvePath("../../../connectors/websocket/javascript/xrt_connector.js"));
    var Bubble = Script.require(Script.resolvePath("../../../presentation/javascript/bubble.js"));
    var style = Bubble.style;
    var tablet = Tablet.getTablet("com.highfidelity.interface.tablet.system");
    var button = tablet.addButton({ text: "", sortOrder: 100 });
    var connector = null;
    var requested = false;
    var status = "unavailable";
    var pendingSince = 0;
    var pairing = false;
    var attemptedPairing = false;
    var buttonState = "";
    var caption = null;
    var captionExpires = 0;
    var captionCreated = 0;
    var captionText = "";
    var captionLayout = null;
    var measurePending = false;
    var opacity = 0;
    var fadeStarted = 0;
    var fadeFrom = 0;
    var revisions = {};
    var currentIdentity = null;
    var ended = false;

    function clearCaption() {
        if (caption) { Entities.deleteEntity(caption); caption = null; }
        captionExpires = 0;
        captionText = "";
        captionLayout = null;
        measurePending = false;
        opacity = 0;
        // Keep revision fences after expiry and clear; only reset opens a new session.
    }

    function control() {
        if (pairing || (connector && !connector.closed && !connector.ready)) {
            return { icon: "waiting", hint: "Connecting to XRTranslate", enabled: false };
        }
        if (!connector || connector.closed) {
            return { icon: "connect", hint: attemptedPairing ? "Reconnect XRTranslate" : "Connect XRTranslate",
                enabled: true, action: "pair" };
        }
        if (status === "error" && !pendingSince) {
            return { icon: "retry", hint: "Retry translation", enabled: true, action: "start" };
        }
        if (requested && status !== "stopping") {
            return { icon: "stop", hint: status === "active" ? "Stop translation" : "Cancel starting",
                enabled: true, active: status === "active", action: "stop" };
        }
        if (pendingSince || status === "stopping" || status === "starting" || status === "active") {
            return { icon: "waiting", hint: "Stopping translation", enabled: false };
        }
        if (status === "off") {
            return { icon: "start", hint: "Start translation", enabled: true, action: "start" };
        }
        return { icon: "waiting", hint: "Open XRTranslate to finish setup", enabled: false };
    }

    function updateButton() {
        if (ended) { return; }
        var state = control();
        var key = JSON.stringify(state);
        if (key === buttonState) { return; }
        buttonState = key;
        var icon = Script.resolvePath("icons/" + state.icon + ".svg");
        button.editProperties({
            text: "", activeText: "", icon: icon, hoverIcon: icon,
            activeIcon: Script.resolvePath("icons/stop-active.svg"),
            activeHoverIcon: Script.resolvePath("icons/stop-active.svg"),
            isActive: !!state.active, buttonEnabled: state.enabled,
            hoverText: state.hint, activeHoverText: state.hint
        });
    }

    function captionPosition() {
        // The default eye position follows avatar height without following head jitter.
        var eye = Vec3.multiplyQbyV(Quat.inverse(MyAvatar.orientation),
            Vec3.subtract(MyAvatar.getDefaultEyePosition(), MyAvatar.position));
        return { x: 0, y: eye.y - 0.28, z: -1.5 };
    }

    function fitCaption() {
        if (!caption || !captionLayout) { return false; }
        var layout = captionLayout;
        var columns = style.maxColumns;
        var contentWidth = style.maxWidth - style.padding * 2 - 0.008;
        var width = 0;
        // Native font metrics can differ from the shared visual-column estimate.
        for (var attempt = 0; attempt < 6; attempt += 1) {
            var lines = layout.text.split("\n");
            width = 0;
            for (var index = 0; index < lines.length; index += 1) {
                var size = Entities.textSize(caption, lines[index]);
                if (!size || !isFinite(size.width) || !isFinite(size.height) || size.height <= 0) { return false; }
                width = Math.max(width, size.width);
            }
            if (width <= contentWidth) { break; }
            columns = Math.max(1, Math.min(columns - 1, Math.floor(columns * contentWidth / width)));
            layout = Bubble.layout(captionText, { maxColumns: columns });
        }
        if (width > contentWidth) {
            layout = Bubble.layout("\u2026");
            var ellipsis = Entities.textSize(caption, layout.text);
            if (!ellipsis || ellipsis.width > contentWidth || ellipsis.height <= 0) { return false; }
            width = ellipsis.width;
        }
        var fullSize = Entities.textSize(caption, layout.text);
        if (!fullSize || !isFinite(fullSize.height) || fullSize.height <= 0) { return false; }
        Entities.editEntity(caption, {
            text: layout.text,
            dimensions: {
                x: Math.max(style.minWidth, Math.min(style.maxWidth, width + style.padding * 2 + 0.008)),
                y: Math.max(layout.height, fullSize.height + style.padding * 2 + 0.008), z: 0.01
            }
        });
        measurePending = false;
        fadeFrom = opacity;
        fadeStarted = Date.now();
        return true;
    }

    function displayCaption(message) {
        if (!requested || status !== "active" || typeof message.text !== "string" ||
                typeof message.stream !== "string" || typeof message.id !== "string" ||
                typeof message.revision !== "number" || !isFinite(message.revision) ||
                message.revision < 0 || message.revision % 1 !== 0) { return; }
        var key = JSON.stringify([message.stream, message.id]);
        if (revisions[key] !== undefined &&
                (key !== currentIdentity || revisions[key] >= message.revision)) { return; }
        revisions[key] = message.revision;
        currentIdentity = key;
        var keys = Object.keys(revisions);
        if (keys.length > 64) { delete revisions[keys[0]]; }
        var lifetime = Bubble.lifetime(message);
        captionLayout = Bubble.layout(message.text);
        if (!lifetime || !captionLayout.text) { clearCaption(); return; }
        var now = Date.now();
        captionText = message.text;
        captionExpires = now + lifetime;
        measurePending = true;
        if (!caption) {
            captionCreated = now;
            caption = Entities.addEntity({
                type: "Text", name: "XRT captions", parentID: MyAvatar.SELF_ID,
                localPosition: captionPosition(), registrationPoint: { x: 0.5, y: 1, z: 0.5 },
                dimensions: { x: style.maxWidth, y: captionLayout.height, z: 0.01 },
                lineHeight: style.lineHeight, text: "", textColor: style.textColor, textAlpha: 0,
                backgroundColor: style.backgroundColor, backgroundAlpha: 0,
                leftMargin: style.padding, rightMargin: style.padding,
                topMargin: style.padding, bottomMargin: style.padding,
                alignment: "left", verticalAlignment: "center", unlit: true,
                billboardMode: "yaw", collisionless: true, lifetime: lifetime / 1000
            }, "local");
        } else {
            var age = Entities.getEntityProperties(caption, ["age"]).age;
            if (!isFinite(age)) { age = (now - captionCreated) / 1000; }
            Entities.editEntity(caption, { lifetime: age + lifetime / 1000 });
            fitCaption();
        }
    }

    function pair() {
        if (pairing) { return; }
        pairing = true;
        updateButton();
        var address = Window.prompt("Paste the connection address from XRTranslate.",
            Settings.getValue("XRT/connectorAddress", ""));
        if (address === null) { pairing = false; updateButton(); return; }
        var token = Window.prompt("Paste the pairing token from XRTranslate.", "");
        if (token === null || !token.trim()) { pairing = false; updateButton(); return; }
        attemptedPairing = true;
        try {
            connector = new Connector(address.trim(), token.trim(), "overte", {
                reset: function () {
                    requested = false;
                    pendingSince = 0;
                    status = "unavailable";
                    revisions = {};
                    currentIdentity = null;
                    clearCaption();
                    updateButton();
                },
                state: function (message) {
                    if (["unavailable", "off", "starting", "active", "stopping", "error"].indexOf(message.status) === -1) { return; }
                    status = message.status;
                    if (status === "error") {
                        requested = false;
                        pendingSince = 0;
                    }
                    if (message.enabled === requested &&
                            ((requested && (status === "starting" || status === "active")) ||
                            (!requested && (status === "off" || status === "error" || status === "unavailable")))) {
                        pendingSince = 0;
                    }
                    if (status !== "active") { clearCaption(); }
                    updateButton();
                },
                caption: displayCaption,
                clear: clearCaption,
                disconnect: function () {
                    requested = false;
                    pendingSince = 0;
                    status = "unavailable";
                    clearCaption();
                    updateButton();
                }
            }, {
                WebSocket: WebSocket,
                clientId: String(Uuid.generate()),
                setInterval: function (callback, delay) { return Script.setInterval(callback, delay); },
                clearInterval: function (timer) { Script.clearInterval(timer); }
            });
            Settings.setValue("XRT/connectorAddress", address.trim());
        } catch (error) {
            Window.alert("Could not connect. Open Avatar controls in XRTranslate and copy its address and pairing token again.");
        }
        token = null;
        pairing = false;
        updateButton();
    }

    function clicked() {
        var state = control();
        if (!state.enabled) { return; }
        if (state.action === "pair") { pair(); return; }
        var next = state.action === "start";
        if (connector.setEnabled(next)) {
            requested = next;
            pendingSince = Date.now();
            if (!requested) { clearCaption(); }
            updateButton();
        }
    }

    function contextChanged() {
        if (connector) { connector.close("Avatar or domain changed"); }
    }

    var displayTimer = Script.setInterval(function () {
        var now = Date.now();
        if (pendingSince && now - pendingSince >= 10000 && connector) {
            connector.close("XRTranslate did not respond");
        }
        updateButton();
        if (!caption) { return; }
        if (now >= captionExpires) { clearCaption(); return; }
        if (measurePending) {
            // Text measurements become available after the native entity is created.
            if (!fitCaption()) { return; }
        }
        var fadeIn = fadeFrom + (1 - fadeFrom) * Math.max(0, Math.min(1, (now - fadeStarted) / style.fadeInMs));
        var fadeOut = Math.min(1, (captionExpires - now) / style.fadeOutMs);
        var nextOpacity = Math.min(fadeIn, fadeOut);
        if (Math.abs(nextOpacity - opacity) >= 0.01) {
            opacity = nextOpacity;
            Entities.editEntity(caption, {
                textAlpha: style.textAlpha * opacity,
                backgroundAlpha: style.backgroundAlpha * opacity
            });
        }
    }, 40);
    button.clicked.connect(clicked);
    MyAvatar.skeletonModelURLChanged.connect(contextChanged);
    MyAvatar.sessionUUIDChanged.connect(contextChanged);
    updateButton();
    Script.scriptEnding.connect(function () {
        ended = true;
        if (connector) { connector.close(); }
        Script.clearInterval(displayTimer);
        MyAvatar.skeletonModelURLChanged.disconnect(contextChanged);
        MyAvatar.sessionUUIDChanged.disconnect(contextChanged);
        button.clicked.disconnect(clicked);
        tablet.removeButton(button);
        clearCaption();
    });
}());
