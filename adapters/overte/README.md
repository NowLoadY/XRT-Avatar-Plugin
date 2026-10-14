# Overte adapter

The tablet provides translation controls and a caption object editor. Create a
horizontal, portrait or board text object, or bind an existing Text entity. Move,
rotate, resize and style it in Overte's Create app.

[Installation and operation](../../docs/adapters/overte/installation.md)

- [`scripts/xrt-avatar.js`](scripts/xrt-avatar.js) owns connection and controls.
- [`scripts/caption-display.js`](scripts/caption-display.js) binds live text to
  an editable object without replacing its transform, font or appearance.
- [`scripts/designer.html`](scripts/designer.html) provides object creation and
  binding controls.
- [`examples/caption-source.js`](examples/caption-source.js) shows how a custom
  renderer receives the uncropped local caption source.

Source lifecycle, presets and text layout come from
[`presentation/`](../../presentation/). The adapter uses Overte's native Tablet,
Text entity and local Messages APIs.
