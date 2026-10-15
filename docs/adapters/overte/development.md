# Overte development

The adapter uses Overte's native Tablet, Text entity, local Messages and WebSocket
APIs. Follow the [installation guide](installation.md) for normal setup.

## Source

| File | Responsibility |
| --- | --- |
| [`xrt-avatar.js`](../../../adapters/overte/scripts/xrt-avatar.js) | Connection, controls and caption source publication |
| [`caption-display.js`](../../../adapters/overte/scripts/caption-display.js) | Native Text binding, measurement and object creation |
| [`designer.html`](../../../adapters/overte/scripts/designer.html) | Object creation and binding interface |
| [`caption-source.js`](../../../adapters/overte/examples/caption-source.js) | Custom renderer example |

The shared [`presentation/`](../../../presentation/) layer owns caption source
lifecycle, layouts and presets. The adapter creates editable native objects and
updates their text. Fonts, transforms and appearance remain on those objects.
See [presentation architecture](../../presentation-architecture.md) and the
[connector protocol](../../connector-protocol.md) for shared contracts.

## Avatar scripts

Host these directories with their repository-relative layout:

- `adapters/overte/scripts/`
- `connectors/websocket/javascript/`
- `presentation/`

Add the entry below to the Avatar's FST, replacing the URL with the hosted script:

```text
script = <xrt-avatar.js URL>
```

Use either an Avatar script or an Interface script for one installation. Avatar
scripts stop when their Avatar is removed. Interface scripts disconnect when the
Avatar or domain session changes. Pairing and enabling translation are fresh user
actions after those changes.

Do not embed pairing tokens in hosted scripts or Avatar files. The running script
receives them from the local XRTranslate instance.

## Local caption source

The script publishes the current plain-text caption through
[`Messages.sendLocalMessage`](https://apidocs.overte.org/Messages.html) on
`XRT/CaptionSource`. Publication is independent of the selected Text object.
Captions are not saved in entity metadata or settings.

```json
{"type":"caption","source":"XRTranslate","value":{"text":"Translated text","stream":"42","id":"turn-1","revision":1,"expiresAt":1790000000000}}
{"type":"clear","source":"XRTranslate"}
```

`text` is the current source text before display wrapping. `expiresAt` is a Unix
timestamp in milliseconds. Consumers accept only local messages, replace the
previous value and clear at that deadline or on `clear`. Apply visual wrapping in
the renderer instead of changing the shared source.

The [custom renderer example](../../../adapters/overte/examples/caption-source.js)
subscribes, handles clearing and expires its display. Implement its `render(text)`
callback to update a custom surface. Authoring previews belong to the selected
object and are not published as translations.

## Native authoring

The designer lists nearby editable Text entities with `Entities.findEntitiesByType`
and binds the selected ID. It does not depend on Create's internal selection state.
Create's Entity List includes local objects when its **Local entities** filter is
enabled. Users can then edit normal entity properties or export a reusable object.

Live updates write the bound object's text and, when selected, its visibility.
Native `Entities.textSize` measurements determine wrapping without resizing the
object or changing its font. Unbinding restores fields still controlled by the
adapter and leaves the object in the scene. The Board preset uses transparent
text; its native font and any board artwork are authored separately.

## Verification

Source and simulated checks do not establish client compatibility. An Overte
acceptance run must cover loading and pairing, controller hover states, Create's
local-object editing, multilingual font coverage, native text measurements,
parented objects, restoration and Avatar/domain changes.

Official references:

- [Avatar script lifecycle and FST installation](https://docs.overte.org/en/latest/script/avatar-scripts.html)
- [Interface script installation](https://docs.overte.org/en/latest/script/interface-scripts.html)
- [WebSocket API](https://apidocs.overte.org/WebSocket.html)
- [Tablet API](https://apidocs.overte.org/TabletProxy.html)
- [Tablet button icons and states](https://apidocs.overte.org/TabletButtonProxy.html)
- [Text entities and editable properties](https://apidocs.overte.org/Entities.html#EntityProperties-Text)
- [Native text measurement](https://apidocs.overte.org/Entities.html#.textSize)
- [Create entity list and local-object filter](https://github.com/overte-org/overte/blob/master/scripts/system/create/entityList/entityList.js)
