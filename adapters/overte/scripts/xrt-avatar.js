(function () {
    "use strict";

    var Connector = Script.require(Script.resolvePath("../../../connectors/websocket/javascript/xrt_connector.js"));
    var Bubble = Script.require(Script.resolvePath("../../../presentation/javascript/bubble.js"));
    var CaptionDisplay = Script.require(Script.resolvePath("caption-display.js"));
    var source = new Bubble.CaptionSource();
    var display = new CaptionDisplay({ Entities: Entities, MyAvatar: MyAvatar, Vec3: Vec3, Quat: Quat,
        Settings: Settings, Picks: Picks, Uuid: Uuid,
        fontUrl: "https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/v0.1.0-preview.7/adapters/overte/fonts/XrtNotoCaption.arfont" }, Bubble);
    var tablet = Tablet.getTablet("com.highfidelity.interface.tablet.system");
    var button = tablet.addButton({ text: "", sortOrder: 100 });
    var connector = null;
    var requested = false;
    var status = "unavailable";
    var pendingSince = 0;
    var pairing = false;
    var attemptedPairing = false;
    var buttonState = "";
    var ended = false;
    var panelUrl = Script.resolvePath("designer.html");
    var panelItems = [];
    var publishedSource = "";
    var sourceGeneration = String(Uuid.generate());
    var displayedState = "";
    var sourceChannel = "XRT/CaptionSource";
    var displayButton = tablet.addButton({ text: "", icon: Script.resolvePath("icons/display.svg"),
        hoverIcon: Script.resolvePath("icons/display.svg"), hoverText: "Caption objects", sortOrder: 101 });

    function publishSource(clear) {
        var snapshot = source.snapshot(Date.now());
        var value = clear !== true && snapshot;
        var message = JSON.stringify({ type: value ? (source.displayMode ? "display" : "caption") : "clear",
            source: "XRTranslate", generation: sourceGeneration, value: value || undefined });
        if (message !== publishedSource) {
            publishedSource = message;
            Messages.sendLocalMessage(sourceChannel, message);
            sendPanelState();
        }
        return snapshot;
    }

    function clearCaption(force) {
        if (source.displayMode && force !== true) { return; }
        source.clear();
        display.preview = false;
        publishSource(true);
        display.update(null, Date.now());
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
        sendPanelState();
    }

    function displayCaption(message) {
        if (!requested || status !== "active" || !source.accept(message, Date.now())) { return; }
        publishSource();
        display.update(source.snapshot(Date.now(), display.region), Date.now());
    }

    function displaySnapshot(message) {
        if (!source.accept(message, Date.now())) { return; }
        publishSource();
        display.update(source.snapshot(Date.now(), display.region), Date.now());
    }

    function sendPanelState() {
        if (ended || !tablet.isPathLoaded(panelUrl)) { return; }
        tablet.emitScriptEvent(JSON.stringify({ app: "xrt-caption-designer", type: "state", items: panelItems,
            binding: display.state(), control: control(), caption: source.snapshot(Date.now()) }));
    }

    function panelEvent(raw) {
        if (ended || !tablet.isPathLoaded(panelUrl) || typeof raw !== "string" || raw.length > 4096) { return; }
        var message;
        try { message = JSON.parse(raw); } catch (error) { return; }
        if (message.app !== "xrt-caption-designer") { return; }
        if (message.action === "ready" || message.action === "refresh") { panelItems = display.list(); }
        else if (message.action === "create") {
            display.create(message.profile, message.audience === "local" ? "local" : "avatar");
            panelItems = display.list();
        } else if (message.action === "bind") {
            var allowed = display.list().some(function (item) { return item.id === message.id; });
            if (allowed) { display.bind(message.id, message.hideWhenEmpty === true); }
        } else if (message.action === "unbind") { display.unbind(); }
        else if (message.action === "visibility" && display.target) {
            display.hideWhenEmpty = message.hideWhenEmpty === true;
        } else if (message.action === "region" && ["full", "header", "messages", "footer"].indexOf(message.region) !== -1) {
            display.region = message.region;
            Settings.setValue("XRT/captionRegion", display.region);
        } else if (message.action === "preview" && display.target) { display.preview = !display.preview; }
        else if (message.action === "control") { clicked(); }
        else { return; }
        display.update(source.snapshot(Date.now(), display.region), Date.now());
        sendPanelState();
    }

    function openPanel() {
        tablet.gotoWebScreen(panelUrl);
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
        sourceGeneration = String(Uuid.generate());
        source.reset();
        clearCaption(true);
        try {
            connector = new Connector(address.trim(), token.trim(), "overte", {
                reset: function () {
                    requested = false;
                    pendingSince = 0;
                    status = "unavailable";
                    if (!source.displayMode) { source.reset(); clearCaption(); }
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
                display: displaySnapshot,
                clear: function () { clearCaption(true); },
                disconnect: function () {
                    requested = false;
                    pendingSince = 0;
                    status = "unavailable";
                    source.reset();
                    clearCaption(true);
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
            if (requested) { display.preview = false; }
            pendingSince = Date.now();
            if (!requested) { clearCaption(); }
            updateButton();
        }
    }

    function contextChanged() {
        if (connector) { connector.close("Avatar or domain changed"); }
    }

    var savedTarget = Settings.getValue("XRT/captionTarget", "");
    if (savedTarget) { display.bind(savedTarget, Settings.getValue("XRT/captionHideWhenEmpty", false)); }
    var displayTimer = Script.setInterval(function () {
        var now = Date.now();
        if (pendingSince && now - pendingSince >= 10000 && connector) {
            connector.close("XRTranslate did not respond");
        }
        updateButton();
        publishSource();
        display.update(source.snapshot(now, display.region), now);
        var currentDisplay = JSON.stringify(display.state());
        if (currentDisplay !== displayedState) { displayedState = currentDisplay; sendPanelState(); }
    }, 100);
    button.clicked.connect(clicked);
    displayButton.clicked.connect(openPanel);
    tablet.webEventReceived.connect(panelEvent);
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
        displayButton.clicked.disconnect(openPanel);
        tablet.webEventReceived.disconnect(panelEvent);
        tablet.removeButton(displayButton);
        clearCaption(true);
        Settings.setValue("XRT/captionHideWhenEmpty", display.hideWhenEmpty);
        display.unbind(true);
    });
}());
