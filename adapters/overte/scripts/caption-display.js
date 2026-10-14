/* Native Text bindings. Authoring changes the object; live output changes its text. */
"use strict";

function CaptionDisplay(runtime, Bubble) {
    this.runtime = runtime;
    this.Bubble = Bubble;
    this.target = null;
    this.lastText = null;
    this.lastVisible = null;
    this.original = null;
    this.hideWhenEmpty = false;
    this.profile = "horizontal";
    this.preview = false;
    this.message = "";
}

CaptionDisplay.prototype.properties = function (id) {
    return this.runtime.Entities.getEntityProperties(id, [
        "type", "name", "text", "locked", "visible", "entityHostType", "owningAvatarID",
        "position", "dimensions", "lineHeight", "leftMargin", "rightMargin", "topMargin", "bottomMargin", "userData"
    ]);
};

CaptionDisplay.prototype.usable = function (properties) {
    if (!properties || properties.type !== "Text" || properties.locked) { return false; }
    if (properties.entityHostType === "avatar") {
        return properties.owningAvatarID === this.runtime.MyAvatar.SELF_ID ||
            properties.owningAvatarID === this.runtime.MyAvatar.sessionUUID;
    }
    return properties.entityHostType === "local" || (properties.entityHostType === "domain" &&
        (this.runtime.Entities.canRez() || this.runtime.Entities.canRezTmp()));
};

CaptionDisplay.prototype.list = function () {
    var self = this;
    var r = this.runtime;
    var ids = r.Entities.findEntitiesByType("Text", r.MyAvatar.position, 30,
        r.Picks.PICK_LOCAL_ENTITIES | r.Picks.PICK_AVATAR_ENTITIES | r.Picks.PICK_DOMAIN_ENTITIES |
        r.Picks.PICK_INCLUDE_INVISIBLE | r.Picks.PICK_INCLUDE_VISIBLE);
    if (this.target && ids.indexOf(this.target) === -1) { ids.push(this.target); }
    return ids.slice(0, 128).map(function (id) {
        var properties = self.properties(id);
        if (!self.usable(properties)) { return null; }
        return { id: id, name: properties.name || "Text", shared: properties.entityHostType !== "local" };
    }).filter(function (item) { return item !== null; });
};

CaptionDisplay.prototype.unbind = function (keepSelection) {
    if (this.target && this.original) {
        var properties = this.properties(this.target);
        var restore = {};
        if (properties.text === this.lastText) { restore.text = this.original.text; }
        if (this.lastVisible !== null && properties.visible === this.lastVisible) { restore.visible = this.original.visible; }
        if (Object.keys(restore).length) { this.runtime.Entities.editEntity(this.target, restore); }
    }
    this.target = null;
    this.original = null;
    this.lastText = null;
    this.lastVisible = null;
    this.preview = false;
    if (!keepSelection) { this.runtime.Settings.setValue("XRT/captionTarget", ""); }
};

CaptionDisplay.prototype.bind = function (id, hideWhenEmpty) {
    var properties = this.properties(id);
    if (!this.usable(properties)) { this.message = "Choose an unlocked Text object you can edit."; return false; }
    if (this.target === id) { this.hideWhenEmpty = !!hideWhenEmpty; return true; }
    this.unbind();
    this.target = id;
    this.original = { text: properties.text || "", visible: properties.visible !== false };
    this.hideWhenEmpty = !!hideWhenEmpty;
    this.profile = "horizontal";
    try {
        var metadata = JSON.parse(properties.userData || "{}").xrt_caption;
        if (metadata && ["horizontal", "vertical", "board"].indexOf(metadata.profile) !== -1) {
            this.profile = metadata.profile;
        }
    } catch (error) { /* User-authored metadata does not have to be JSON. */ }
    this.runtime.Settings.setValue("XRT/captionTarget", id);
    this.message = "";
    return true;
};

CaptionDisplay.prototype.create = function (profileName) {
    if (["horizontal", "vertical", "board"].indexOf(profileName) === -1) { return false; }
    var profile = this.Bubble.profiles[profileName];
    if (!profile) { return false; }
    var r = this.runtime;
    var position = r.Vec3.sum(r.MyAvatar.getDefaultEyePosition(),
        r.Vec3.multiplyQbyV(r.MyAvatar.orientation, { x: 0, y: -0.25, z: -1.5 }));
    var id = r.Entities.addEntity({
        type: "Text", name: "XRT " + profileName.charAt(0).toUpperCase() + profileName.slice(1),
        position: position, rotation: r.MyAvatar.orientation,
        dimensions: { x: profile.width, y: profile.height, z: 0.01 },
        lineHeight: profile.lineHeight, text: "", textColor: profile.textColor,
        textAlpha: profile.textAlpha, backgroundColor: profile.backgroundColor,
        backgroundAlpha: profile.backgroundAlpha,
        leftMargin: profile.padding, rightMargin: profile.padding,
        topMargin: profile.padding, bottomMargin: profile.padding,
        alignment: "left", verticalAlignment: "center", unlit: true,
        billboardMode: "none", collisionless: true,
        userData: JSON.stringify({ xrt_caption: { profile: profileName } })
    }, "local");
    if (!id || id === r.Uuid.NONE) { this.message = "Could not create the Text object."; return false; }
    if (!this.bind(id, true)) { return false; }
    this.preview = true;
    return true;
};

CaptionDisplay.prototype.fit = function (text, properties) {
    var Bubble = this.Bubble;
    var r = this.runtime;
    var width = properties.dimensions.x - properties.leftMargin - properties.rightMargin - 0.008;
    var height = properties.dimensions.y - properties.topMargin - properties.bottomMargin - 0.008;
    var probe = r.Entities.textSize(this.target, "M");
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

CaptionDisplay.prototype.update = function (snapshot, now) {
    if (!this.target) { return; }
    var properties = this.properties(this.target);
    if (!this.usable(properties)) { this.unbind(); this.message = "The Text object is no longer available."; return; }
    var text = snapshot ? snapshot.text : (this.preview ? "Your translation appears here." : "");
    var rendered = text ? this.fit(text, properties) : "";
    if (rendered === null) { return; }
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

CaptionDisplay.prototype.state = function () {
    var properties = this.target ? this.properties(this.target) : {};
    return { target: this.target || "", name: properties.name || "", profile: this.profile,
        hideWhenEmpty: this.hideWhenEmpty, preview: this.preview, shared: this.target && properties.entityHostType !== "local", message: this.message };
};

module.exports = CaptionDisplay;
