(function () {
    "use strict";

    var Connector = Script.require(Script.resolvePath("../../../connectors/websocket/javascript/xrt_connector.js"));
    var tablet = Tablet.getTablet("com.highfidelity.interface.tablet.system");
    var button = tablet.addButton({ text: "XRT Pair", sortOrder: 100 });
    var connector = null;
    var requested = false;
    var status = "unavailable";
    var caption = null;
    var captionExpires = 0;
    var revisions = {};
    var ended = false;

    function clearCaption() {
        if (caption) { Entities.deleteEntity(caption); caption = null; }
        captionExpires = 0;
        revisions = {};
    }

    function updateButton() {
        if (ended) { return; }
        var labels = { off: "Off", starting: "Starting", active: "Active", stopping: "Stopping", error: "Error" };
        button.editProperties({
            text: "XRT " + (labels[status] || "Pair"),
            isActive: status === "active",
            hoverText: requested ? "Stop translation" : "Start translation"
        });
    }

    function displayCaption(message) {
        if (!requested || typeof message.text !== "string" || typeof message.revision !== "number" ||
                typeof message.expires_ms !== "number" || !isFinite(message.expires_ms)) { return; }
        var key = JSON.stringify([message.stream, message.id]);
        if (revisions[key] !== undefined && revisions[key] >= message.revision) { return; }
        revisions[key] = message.revision;
        var keys = Object.keys(revisions);
        if (keys.length > 64) { delete revisions[keys[0]]; }
        captionExpires = Date.now() + Math.min(Math.max(message.expires_ms, 0), 15000);
        var text = message.text.slice(0, 4096);
        if (!caption) {
            caption = Entities.addEntity({
                type: "Text", name: "XRT local captions", parentID: MyAvatar.SELF_ID,
                localPosition: { x: 0, y: 1.25, z: -1.2 },
                dimensions: { x: 1.1, y: 0.32, z: 0.01 }, lineHeight: 0.045,
                text: text, textColor: { red: 245, green: 245, blue: 250 },
                backgroundColor: { red: 18, green: 23, blue: 34 }, backgroundAlpha: 0.85,
                leftMargin: 0.025, rightMargin: 0.025, topMargin: 0.025, bottomMargin: 0.025,
                alignment: "center", verticalAlignment: "center", unlit: true,
                billboardMode: "full", collisionless: true, lifetime: 20
            }, "local");
        } else {
            // Reset lifetime as age is measured from creation by the entity API.
            var age = Entities.getEntityProperties(caption, ["age"]).age || 0;
            Entities.editEntity(caption, { text: text, lifetime: age + 20 });
        }
    }

    function pair() {
        var address = Window.prompt("XRTranslate connector address", Settings.getValue("XRT/connectorAddress", "ws://127.0.0.1:"));
        if (address === null) { return; }
        var token = Window.prompt("XRTranslate pairing token (kept for this script session)", "");
        if (token === null || !token.trim()) { return; }
        try {
            connector = new Connector(address.trim(), token.trim(), "overte", {
                reset: function () { requested = false; clearCaption(); },
                state: function (message) { status = message.status; updateButton(); },
                caption: displayCaption,
                clear: clearCaption,
                disconnect: function () {
                    requested = false;
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
            Window.alert("Enter the local ws:// address shown by XRTranslate.");
        }
        token = null;
    }

    function clicked() {
        if (!connector || connector.closed) { pair(); return; }
        if (!connector.ready) { return; }
        var next = !requested;
        if (connector.setEnabled(next)) {
            requested = next;
            if (!requested) { clearCaption(); }
            updateButton();
        }
    }

    function contextChanged() {
        if (connector) { connector.close("Avatar or domain changed"); }
    }

    var expiryTimer = Script.setInterval(function () {
        if (captionExpires && Date.now() >= captionExpires) { clearCaption(); }
    }, 250);
    button.clicked.connect(clicked);
    MyAvatar.skeletonModelURLChanged.connect(contextChanged);
    MyAvatar.sessionUUIDChanged.connect(contextChanged);
    Script.scriptEnding.connect(function () {
        ended = true;
        if (connector) { connector.close(); }
        Script.clearInterval(expiryTimer);
        MyAvatar.skeletonModelURLChanged.disconnect(contextChanged);
        MyAvatar.sessionUUIDChanged.disconnect(contextChanged);
        button.clicked.disconnect(clicked);
        tablet.removeButton(button);
        clearCaption();
    });
}());
