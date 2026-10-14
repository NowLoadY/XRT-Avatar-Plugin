/* XRT WebSocket v1. UI, game lifecycle and presentation belong to the adapter. */
"use strict";

function Connector(address, token, connector, callbacks, runtime) {
    if (!/^ws:\/\/(127\.0\.0\.1|localhost|\[::1\]):[0-9]+(?:\/[^?#]*)?$/.test(address)) {
        throw new Error("Use the local ws:// address shown in XRTranslate");
    }
    this.ready = false;
    this.session = null;
    this.sequence = 0;
    this.resetSequence = 0;
    this.receivedAt = Date.now();
    this.callbacks = callbacks;
    this.runtime = runtime;
    this.socket = new runtime.WebSocket(address);
    this.timer = null;
    this.closed = false;
    var self = this;
    this.socket.onopen = function () {
        if (self.closed) { token = null; return; }
        try {
            self.socket.send(JSON.stringify({
                type: "hello", protocol: 1, connector: connector, token: token,
                client_id: runtime.clientId, capabilities: ["captions"]
            }));
        } catch (error) { self.close("XRTranslate connection failed"); }
        finally { token = null; }
    };
    this.socket.onmessage = function (event) {
        if (self.closed) { return; }
        try { self.receive(event.data); }
        catch (error) { self.close("Incompatible connector response"); }
    };
    this.socket.onerror = function () { self.close("XRTranslate connection failed"); };
    this.socket.onclose = function () { self.close("XRTranslate disconnected"); };
    this.timer = runtime.setInterval(function () {
        if (Date.now() - self.receivedAt >= 15000) {
            self.close("XRTranslate connection timed out");
        } else if (self.session && !self.closed) {
            self.send("ping", {});
        }
    }, 3000);
}

Connector.prototype.send = function (type, fields) {
    if (this.closed || this.socket.readyState !== 1) { return false; }
    var message = { type: type, session: this.session };
    Object.keys(fields).forEach(function (key) { message[key] = fields[key]; });
    try {
        this.socket.send(JSON.stringify(message));
        return true;
    } catch (error) {
        this.close("XRTranslate connection failed");
        return false;
    }
};

Connector.prototype.receive = function (raw) {
    if (typeof raw !== "string" || raw.length > 65536) { throw new Error("Invalid frame"); }
    var message = JSON.parse(raw);
    if (!this.session) {
        if (message.type !== "welcome" || message.protocol !== 1 ||
                typeof message.session !== "string" || !message.session) {
            throw new Error("Invalid handshake");
        }
        this.session = message.session;
    } else {
        if (message.session !== this.session) { throw new Error("Session changed"); }
        if (message.type === "reset") {
            if (typeof message.reset !== "number" || message.reset % 1 !== 0) {
                throw new Error("Invalid reset");
            }
            if (message.reset > this.resetSequence) {
                this.ready = false;
                this.callbacks.reset(message);
                if (this.send("reset_ack", { reset: message.reset, enabled: false }) && !this.closed) {
                    this.resetSequence = message.reset;
                    this.ready = true;
                }
            }
        } else if (message.type === "state" || message.type === "caption" || message.type === "clear") {
            this.callbacks[message.type](message);
        } else if (message.type !== "pong") {
            throw new Error("Unknown message");
        }
    }
    this.receivedAt = Date.now();
};

Connector.prototype.setEnabled = function (enabled) {
    if (!this.ready || this.closed) { return false; }
    this.sequence += 1;
    return this.send("set_enabled", { sequence: this.sequence, enabled: !!enabled });
};

Connector.prototype.close = function (reason) {
    if (this.closed) { return; }
    this.closed = true;
    this.ready = false;
    this.runtime.clearInterval(this.timer);
    // Clear platform state synchronously; transport shutdown may complete later.
    try { this.callbacks.disconnect(reason || "Disconnected"); }
    finally {
        try { this.socket.close(); } catch (error) { /* Already closed by the runtime. */ }
    }
};

module.exports = Connector;
