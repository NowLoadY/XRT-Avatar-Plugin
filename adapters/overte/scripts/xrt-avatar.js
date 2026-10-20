(function () {
    "use strict";

    var Connector = Script.require(Script.resolvePath("../../../connectors/websocket/javascript/xrt_connector.js"));
    var Bubble = Script.require(Script.resolvePath("../../../presentation/javascript/bubble.js"));
    var CaptionDisplay = Script.require(Script.resolvePath("caption-display.js"));
    var source = new Bubble.CaptionSource();
    var display = new CaptionDisplay({ Entities: Entities, MyAvatar: MyAvatar, Vec3: Vec3, Quat: Quat,
        Settings: Settings, Picks: Picks, Uuid: Uuid,
        fontUrl: "https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/v0.1.0/adapters/overte/fonts/XrtNotoCaption.arfont" }, Bubble);
    var tablet = Tablet.getTablet("com.highfidelity.interface.tablet.system");
    var button = tablet.addButton({ text: "", sortOrder: 100 });
    var connector = null;
    var requested = false;
    var status = "unavailable";
    var pendingSince = 0;
    var pairing = false;
    var connectionInfo = null;
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
    }

    function clearCaption(force) {
        if (source.displayMode && force !== true) { return; }
        source.clear();
        if (!ended) { display.stopPreviews(); display.update(null, Date.now()); }
        publishSource(true);
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

    function displayText(message) {
        if (message.type === "caption" && (!requested || status !== "active")) { return; }
        if (!source.accept(message, Date.now())) { return; }
        publishSource();
        display.update(source, Date.now());
    }

    function sendPanelState() {
        if (ended || !tablet.isPathLoaded(panelUrl)) { return; }
        var binding = display.current();
        tablet.emitScriptEvent(JSON.stringify({ app: "xrt-caption-designer", type: "state", items: panelItems,
            binding: display.state(), control: control(), caption: source.snapshot(Date.now(), binding ? binding.region : "full") }));
    }

    function panelEvent(raw) {
        if (ended || !tablet.isPathLoaded(panelUrl) || typeof raw !== "string" || raw.length > 4096) { return; }
        var message;
        try { message = JSON.parse(raw); } catch (error) { return; }
        if (message.app !== "xrt-caption-designer") { return; }
        var binding = display.current();
        if (message.action === "create") {
            display.create(message.profile, message.audience === "local" ? "local" : "avatar");
        } else if (message.action === "select") {
            display.select(message.id);
        } else if (message.action === "bind") {
            var allowed = display.list().some(function (item) { return item.id === message.id; });
            if (allowed) { display.bind(message.id, message.hideWhenEmpty === true); }
        } else if (message.action === "unbind") { display.unbind(); }
        else if (message.action === "visibility" && binding) {
            binding.hideWhenEmpty = message.hideWhenEmpty === true;
        } else if (message.action === "region" && binding && ["full", "header", "messages", "footer"].indexOf(message.region) !== -1) {
            binding.region = message.region;
        } else if (message.action === "preview" && binding) { binding.preview = !binding.preview; }
        else if (message.action === "control") { clicked(); }
        else if (message.action !== "ready" && message.action !== "refresh") { return; }
        display.save();
        display.update(source, Date.now());
        panelItems = display.list();
        sendPanelState();
    }

    function openPanel() {
        tablet.gotoWebScreen(panelUrl);
    }

    function pair() {
        if (pairing) { return; }
        pairing = true;
        updateButton();
        // Consume the cached pairing; only a successful handshake may retain it.
        var info = connectionInfo;
        connectionInfo = null;
        if (!info) {
            var pasted = Window.prompt("In XRTranslate, choose Overte and Copy connection info. Paste it here.", "");
            if (pasted === null) { pairing = false; updateButton(); return; }
            try { info = Connector.parsePairing(pasted, "overte"); }
            catch (error) {
                Window.alert(error.message);
                pairing = false; updateButton(); return;
            }
            pasted = null;
        }
        attemptedPairing = true;
        sourceGeneration = String(Uuid.generate());
        source.reset();
        clearCaption(true);
        try {
            connector = new Connector(info.address, info.token, "overte", {
                reset: function () {
                    connectionInfo = info;
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
                caption: displayText,
                display: displayText,
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
        } catch (error) {
            connectionInfo = null;
            Window.alert("Could not connect. Copy fresh connection info from Game Plugins in XRTranslate.");
        }
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
            if (requested) { display.stopPreviews(); }
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
        publishSource();
        display.update(source, now);
        var currentDisplay = JSON.stringify(display.state());
        if (currentDisplay !== displayedState) { displayedState = currentDisplay; panelItems = display.list(); sendPanelState(); }
    }, 100);
    button.clicked.connect(clicked);
    displayButton.clicked.connect(openPanel);
    tablet.webEventReceived.connect(panelEvent);
    MyAvatar.skeletonModelURLChanged.connect(contextChanged);
    MyAvatar.sessionUUIDChanged.connect(contextChanged);
    updateButton();
    Script.scriptEnding.connect(function () {
        ended = true;
        connectionInfo = null;
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
        display.dispose();
    });
}());
