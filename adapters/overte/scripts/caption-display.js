/* Native Text bindings. Authoring changes the object; live output changes its text. */
"use strict";

function TextBinding(runtime, Bubble) {
    this.runtime = runtime;
    this.Bubble = Bubble;
    this.target = null;
    this.available = false;
    this.retryAt = 0;
    this.name = "Text";
    this.shared = false;
    this.lastText = null;
    this.lastVisible = null;
    this.original = null;
    this.hideWhenEmpty = false;
    this.profile = "horizontal";
    this.region = "full";
    this.preview = false;
    this.fitKey = null;
    this.fitted = "";
}

TextBinding.prototype.properties = function (id) {
    return this.runtime.Entities.getEntityProperties(id, [
        "type", "name", "text", "locked", "visible", "entityHostType", "owningAvatarID",
        "position", "dimensions", "font", "lineHeight", "leftMargin", "rightMargin", "topMargin", "bottomMargin", "userData"
    ]);
};

TextBinding.prototype.usable = function (properties) {
    if (!properties || properties.type !== "Text" || properties.locked) { return false; }
    if (properties.entityHostType === "avatar") {
        return properties.owningAvatarID === this.runtime.MyAvatar.SELF_ID ||
            properties.owningAvatarID === this.runtime.MyAvatar.sessionUUID;
    }
    return properties.entityHostType === "local" || (properties.entityHostType === "domain" &&
        (this.runtime.Entities.canRez() || this.runtime.Entities.canRezTmp()));
};

TextBinding.prototype.unbind = function () {
    if (this.target && this.original) {
        var properties = this.properties(this.target);
        var restore = {};
        if (properties.text === this.lastText) { restore.text = this.original.text; }
        if (this.lastVisible !== null && properties.visible === this.lastVisible) { restore.visible = this.original.visible; }
        if (Object.keys(restore).length) { this.runtime.Entities.editEntity(this.target, restore); }
    }
    this.target = null;
    this.available = false;
    this.retryAt = 0;
    this.original = null;
    this.lastText = null;
    this.lastVisible = null;
    this.preview = false;
    this.fitKey = null;
};

TextBinding.prototype.bind = function (id, hideWhenEmpty) {
    if (this.target !== id) { this.unbind(); }
    this.target = id;
    this.hideWhenEmpty = !!hideWhenEmpty;
    return this.resolve(this.properties(id));
};

TextBinding.prototype.resolve = function (properties) {
    this.available = this.usable(properties);
    if (!this.available) { return false; }
    this.name = properties.name || "Text";
    this.shared = properties.entityHostType !== "local";
    if (!this.original) {
        this.original = { text: properties.text || "", visible: properties.visible !== false };
        try {
            var metadata = JSON.parse(properties.userData || "{}").xrt_caption;
            if (metadata && ["horizontal", "vertical", "board"].indexOf(metadata.profile) !== -1) {
                this.profile = metadata.profile;
            }
        } catch (error) { /* User-authored metadata does not have to be JSON. */ }
    }
    return true;
};

TextBinding.prototype.fit = function (text, properties, probe) {
    var Bubble = this.Bubble;
    var r = this.runtime;
    var width = properties.dimensions.x - properties.leftMargin - properties.rightMargin - 0.008;
    var height = properties.dimensions.y - properties.topMargin - properties.bottomMargin - 0.008;
    if (!probe || !isFinite(probe.width) || !isFinite(probe.height) || probe.width <= 0 || probe.height <= 0) { return null; }
    if (width <= 0 || height < probe.height) { return ""; }
    var columns = Math.max(1, Math.min(84, Math.floor(width / probe.width)));
    var lines = Math.max(1, Math.min(12, Math.floor(height / probe.height)));
    var layout;
    for (var attempt = 0; attempt < 8; attempt += 1) {
        layout = Bubble.layout(text, { profile: this.profile, maxColumns: columns, maxLines: lines });
        var metrics = r.Entities.textSize(this.target, layout.text);
        if (!metrics || !isFinite(metrics.width) || !isFinite(metrics.height) || metrics.height <= 0) { return null; }
        if (metrics.width <= width && metrics.height <= height) { return layout.text; }
        if (metrics.height > height && lines > 1) {
            lines = Math.max(1, Math.min(lines - 1, Math.floor(lines * height / metrics.height)));
        } else {
            columns = Math.max(1, Math.min(columns - 1, Math.floor(columns * width / metrics.width)));
        }
    }
    var ellipsis = r.Entities.textSize(this.target, "\u2026");
    return ellipsis && ellipsis.width <= width && ellipsis.height <= height ? "\u2026" : "";
};

TextBinding.prototype.update = function (snapshot, now) {
    if (!this.target || now < this.retryAt) { return; }
    var properties = this.properties(this.target);
    if (!this.resolve(properties)) { this.retryAt = now + 1000; return; }
    this.retryAt = 0;
    var text = snapshot && snapshot.text ? snapshot.text : (this.preview ? "Your text appears here." : "");
    // Native font loading is asynchronous; include a small metric probe so the
    // full layout is refreshed when a new font finishes loading.
    var probe = text ? this.runtime.Entities.textSize(this.target, "M") : null;
    var fitKey = JSON.stringify([text, probe, this.profile, properties.font, properties.lineHeight,
        properties.dimensions, properties.leftMargin, properties.rightMargin,
        properties.topMargin, properties.bottomMargin]);
    if (fitKey !== this.fitKey) {
        var fitted = text ? this.fit(text, properties, probe) : "";
        if (fitted === null) { return; }
        this.fitted = fitted;
        this.fitKey = fitKey;
    }
    var rendered = this.fitted;
    var changes = {};
    if (properties.text !== rendered) { changes.text = rendered; }
    this.lastText = rendered;
    if (this.hideWhenEmpty) {
        if (properties.visible !== !!rendered) { changes.visible = !!rendered; }
        this.lastVisible = !!rendered;
    } else if (this.lastVisible !== null) {
        if (properties.visible === this.lastVisible) { changes.visible = this.original.visible; }
        this.lastVisible = null;
    }
    if (Object.keys(changes).length) { this.runtime.Entities.editEntity(this.target, changes); }
};

TextBinding.prototype.state = function () {
    return { target: this.target || "", name: this.name, profile: this.profile, region: this.region,
        hideWhenEmpty: this.hideWhenEmpty, preview: this.preview, shared: this.shared, available: this.available };
};

function CaptionDisplay(runtime, Bubble) {
    this.runtime = runtime;
    this.Bubble = Bubble;
    this.fontUrl = runtime.fontUrl;
    this.bindings = [];
    this.target = null;
    this.message = "";
    var saved = null, raw = runtime.Settings.getValue("XRT/captionBindings", "");
    try { saved = raw ? JSON.parse(raw) : null; } catch (error) { /* Ignore invalid preferences. */ }
    if (!saved || !Array.isArray(saved.objects)) {
        var previous = runtime.Settings.getValue("XRT/captionTarget", "");
        saved = { selected: previous, objects: previous ? [{ id: previous,
            region: runtime.Settings.getValue("XRT/captionRegion", "full"),
            hideWhenEmpty: runtime.Settings.getValue("XRT/captionHideWhenEmpty", false) }] : [] };
    }
    var self = this;
    saved.objects.slice(0, 128).forEach(function (item) {
        if (!item || typeof item.id !== "string" || !item.id || self.binding(item.id)) { return; }
        var binding = new TextBinding(runtime, Bubble);
        binding.name = typeof item.name === "string" ? item.name.slice(0, 128) : "Text " + item.id.slice(0, 8);
        binding.bind(item.id, item.hideWhenEmpty === true);
        if (["full", "header", "messages", "footer"].indexOf(item.region) !== -1) { binding.region = item.region; }
        binding.preview = item.preview === true;
        self.bindings.push(binding);
    });
    this.target = typeof saved.selected === "string" && saved.selected &&
        (this.binding(saved.selected) || this.usable(this.properties(saved.selected))) ? saved.selected :
        (this.bindings.length ? this.bindings[0].target : null);
    this.save();
}

CaptionDisplay.prototype.properties = TextBinding.prototype.properties;
CaptionDisplay.prototype.usable = TextBinding.prototype.usable;

CaptionDisplay.prototype.list = function () {
    var self = this;
    var r = this.runtime;
    var ids = r.Entities.findEntitiesByType("Text", r.MyAvatar.position, 30,
        r.Picks.PICK_LOCAL_ENTITIES | r.Picks.PICK_AVATAR_ENTITIES | r.Picks.PICK_DOMAIN_ENTITIES |
        r.Picks.PICK_INCLUDE_INVISIBLE | r.Picks.PICK_INCLUDE_VISIBLE);
    ids = this.bindings.map(function (binding) { return binding.target; }).concat(this.target ? [this.target] : [], ids)
        .filter(function (id, index, values) { return values.indexOf(id) === index; });
    return ids.slice(0, 128).map(function (id) {
        var properties = self.properties(id), binding = self.binding(id), available = self.usable(properties);
        if (!available && !binding) { return null; }
        return { id: id, name: properties.name || (binding ? binding.name : "Text"),
            shared: available ? properties.entityHostType !== "local" : false, bound: !!binding, available: available };
    }).filter(function (item) { return item !== null; });
};

CaptionDisplay.prototype.create = function (profileName, audience) {
    if (["horizontal", "vertical", "board"].indexOf(profileName) === -1) { return false; }
    var profile = this.Bubble.profiles[profileName];
    if (!profile) { return false; }
    var r = this.runtime;
    var wearable = audience === "avatar";
    if (wearable && !r.Entities.canRezAvatarEntities()) {
        this.message = "Avatar objects are unavailable here. Choose Only me or visit another domain.";
        return false;
    }
    var position = r.Vec3.sum(r.MyAvatar.getDefaultEyePosition(),
        r.Vec3.multiplyQbyV(r.MyAvatar.orientation, { x: 0, y: -0.25, z: wearable ? -0.6 : -1.5 }));
    var properties = {
        type: "Text", name: "XRT " + profileName.charAt(0).toUpperCase() + profileName.slice(1),
        position: position, rotation: wearable ? r.Quat.multiply(r.MyAvatar.orientation,
            r.Quat.fromPitchYawRollDegrees(0, 180, 0)) : r.MyAvatar.orientation,
        dimensions: { x: profile.width, y: profile.height, z: 0.01 },
        lineHeight: profile.lineHeight, font: this.fontUrl, text: "", textColor: profile.textColor,
        textAlpha: profile.textAlpha, backgroundColor: profile.backgroundColor,
        backgroundAlpha: profile.backgroundAlpha,
        leftMargin: profile.padding, rightMargin: profile.padding,
        topMargin: profile.padding, bottomMargin: profile.padding,
        alignment: "left", verticalAlignment: "center", unlit: true,
        billboardMode: "none", collisionless: true,
        userData: JSON.stringify({ xrt_caption: { profile: profileName } })
    };
    if (wearable) { properties.parentID = r.MyAvatar.SELF_ID; }
    var id = r.Entities.addEntity(properties, wearable ? "avatar" : "local");
    if (!id || id === r.Uuid.NONE) { this.message = "Could not create the Text object."; return false; }
    if (!this.bind(id, true)) { r.Entities.deleteEntity(id); return false; }
    this.current().preview = true;
    this.save();
    return true;
};

CaptionDisplay.prototype.binding = function (id) {
    return this.bindings.filter(function (binding) { return binding.target === id; })[0] || null;
};

CaptionDisplay.prototype.current = function () { return this.binding(this.target); };

CaptionDisplay.prototype.save = function () {
    this.runtime.Settings.setValue("XRT/captionBindings", JSON.stringify({ selected: this.target,
        objects: this.bindings.map(function (binding) { return { id: binding.target, name: binding.name, region: binding.region,
            hideWhenEmpty: binding.hideWhenEmpty, preview: binding.preview }; }) }));
};

CaptionDisplay.prototype.select = function (id) {
    this.target = typeof id === "string" && id && (this.binding(id) || this.usable(this.properties(id))) ? id : null;
    this.message = "";
    this.save();
};

CaptionDisplay.prototype.bind = function (id, hideWhenEmpty) {
    this.select(id);
    if (!this.target) { this.message = "Choose an unlocked Text object you can edit."; return false; }
    if (!this.current()) {
        var binding = new TextBinding(this.runtime, this.Bubble);
        if (!binding.bind(id, hideWhenEmpty)) { this.message = "Choose an unlocked Text object you can edit."; return false; }
        this.bindings.push(binding);
    }
    this.save();
    return true;
};

CaptionDisplay.prototype.unbind = function () {
    var binding = this.current();
    if (!binding) { return; }
    binding.unbind();
    this.bindings = this.bindings.filter(function (item) { return item !== binding; });
    if (!this.usable(this.properties(this.target))) { this.target = null; }
    this.save();
};

CaptionDisplay.prototype.stopPreviews = function () {
    var changed = false;
    this.bindings.forEach(function (binding) {
        if (binding.preview) { binding.preview = false; changed = true; }
    });
    if (changed) { this.save(); }
};

CaptionDisplay.prototype.update = function (source, now) {
    this.bindings.forEach(function (binding) {
        binding.update(source ? source.snapshot(now, binding.region) : null, now);
    });
};

CaptionDisplay.prototype.state = function () {
    var binding = this.current(), properties = !binding && this.target ? this.properties(this.target) : {};
    var state = binding ? binding.state() : { target: this.target || "", name: properties.name || "", region: "full",
        hideWhenEmpty: false, preview: false, available: this.usable(properties), shared: this.target && properties.entityHostType !== "local" };
    state.bound = !!binding;
    state.message = this.message || state.message || "";
    return state;
};

CaptionDisplay.prototype.dispose = function () {
    this.save();
    this.bindings.forEach(function (binding) { binding.unbind(); });
    this.bindings = [];
};

module.exports = CaptionDisplay;
