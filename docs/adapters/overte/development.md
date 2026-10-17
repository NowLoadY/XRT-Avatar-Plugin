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
Avatar or domain session changes. The user clicks Reconnect after those changes;
the still-running script reuses its last successful pairing in memory, including
after a temporary connection loss. A failed handshake discards that cached pairing.
Restarting the script or XRTranslate requires fresh connection info.
Translation always starts Off and requires a new Start action.

Do not embed pairing tokens in hosted scripts or Avatar files. The running script
receives them from the local XRTranslate instance.

## Local text source

The script publishes on `XRT/CaptionSource` using
[`Messages.sendLocalMessage`](https://apidocs.overte.org/Messages.html), independently
of the selected Text object. The value contains separate regions and their
combined plain-text projection:

```json
{"type":"display","source":"XRTranslate","generation":"local-connection-id","value":{"text":"Header\nTranslated text\nFooter","header":"Header","messages":[{"id":"42:1","text":"Translated text","expiresAt":1790000000000}],"footer":"Footer","revision":1}}
{"type":"clear","source":"XRTranslate","generation":"local-connection-id"}
```

Message `expiresAt` values are Unix milliseconds. Headers and footers persist
until replaced, cleared or disconnected. Consumers accept local messages, retain
line breaks and apply wrapping in their renderer. Empty display snapshots retain
their revision. A `clear` removes content while preserving that revision; a new
`generation` starts a new connection. The shared source also accepts legacy
single-caption frames.

The [custom renderer example](../../../adapters/overte/examples/caption-source.js)
uses `CaptionSource.snapshot(now, region)` with `full`, `header`, `messages` or
`footer`. Implement its `render(text)` callback for a custom surface. Authoring
preview text stays on the selected object and does not enter the live source.

## Native authoring

The tablet's **Caption objects** button opens `designer.html` through
`tablet.gotoWebScreen()`. The page exchanges actions and state with the script
through `EventBridge`. Each connected object selects the complete display or one
region and retains its own preview and empty-state visibility settings.

The designer lists nearby editable Text entities with `Entities.findEntitiesByType`
and binds selected IDs to one shared `CaptionSource`. Selecting another object
changes the editor's target while existing bindings continue updating. It does
not depend on Create's internal selection state.
Create's Entity List includes local objects when its **Local entities** filter is
enabled. Users can then edit normal entity properties or export a reusable object.

New objects use an avatar host and `MyAvatar.SELF_ID` parent when **Wear with
Avatar** is selected. The adapter checks `Entities.canRezAvatarEntities()` before
creation; **Only me** creates a local entity. Existing bindings never change the
entity's host, parent or transform.

`CaptionDisplay` owns the binding collection; `TextBinding` owns one renderer's
text, optional visibility and restoration. Local preferences retain object IDs
and authoring settings; the previous single-object preference migrates on load.
Unresolved objects retain those preferences and retry once per second. The
binding captures original fields on its first successful resolution and retains
them across temporary unavailability. Only explicit unbinding removes the saved
connection; availability changes do not write preferences.

Live updates write each bound object's text and, when selected, its visibility.
Native `Entities.textSize` measurements determine wrapping without resizing the
object or changing its font. Shared `layoutDisplay` reserves a row for each
banner when space permits and fills the body with the newest complete messages
that fit. At least one body row is available while messages are present; an
oversized latest message is shortened with an ellipsis. Region-only bindings use
the whole object. Raw local text-source messages retain all candidate content.
Full layout measurements are cached until text, font, dimensions
or margins change. A small metric probe detects asynchronous font loading; an
unloaded font is retried on the next update. Unbinding restores fields still controlled by the
adapter and leaves the object in the scene. Translation control resets preserve
independent display regions; connection loss clears the source. The Board preset
uses transparent text; its native font and any board artwork are authored separately.

## Font resource

New caption objects use [`XrtNotoCaption.arfont`](../../../adapters/overte/fonts/XrtNotoCaption.arfont),
a native PNG MTSDF font generated from the pinned OFL Noto source. The native Text
renderer supports built-in fonts and HTTPS `.arfont` URLs; it does not provide an
operating-system font fallback. The script uses a versioned repository URL so
other viewers can load the same resource. Changing an existing object's font is
an authoring action, never a caption update.

[`font.json`](../../../adapters/overte/fonts/font.json) records exact coverage,
atlas dimensions, GPU bytes and source/output hashes. The build uses the shared
font repertoire in `tools/build_caption_font.py`. To regenerate with
[msdf-atlas-gen 1.4](https://github.com/Chlumsky/msdf-atlas-gen/tree/v1.4):

```sh
python adapters/overte/fonts/build_font.py --font /path/to/NotoSansCJKsc-Regular.otf --generator /path/to/msdf-atlas-gen
```

The generator and original font are development inputs; players receive the
native font and its OFL notice. The selected repertoire covers common CJK text
and the font's Latin, Greek and Cyrillic glyphs. Missing glyphs use Overte's native
replacement glyph.

## Verification

Source and simulated checks do not establish client compatibility. An Overte
acceptance run must cover loading and pairing, controller hover states, Create's
local/Avatar-object editing and visibility, multilingual font coverage, native text measurements,
parented objects, restoration and Avatar/domain changes.

Official references:

- [Avatar script lifecycle and FST installation](https://docs.overte.org/en/latest/script/avatar-scripts.html)
- [Interface script installation](https://docs.overte.org/en/latest/script/interface-scripts.html)
- [WebSocket API](https://apidocs.overte.org/WebSocket.html)
- [Tablet API](https://apidocs.overte.org/TabletProxy.html)
- [Tablet button icons and states](https://apidocs.overte.org/TabletButtonProxy.html)
- [Text entities and editable properties](https://apidocs.overte.org/Entities.html#EntityProperties-Text)
- [Avatar entity creation and permissions](https://apidocs.overte.org/Entities.html#.canRezAvatarEntities)
- [Native font loading and glyph fallback](https://github.com/overte-org/overte/blob/master/libraries/render-utils/src/text/Font.cpp)
- [Native text measurement](https://apidocs.overte.org/Entities.html#.textSize)
- [Create entity list and local-object filter](https://github.com/overte-org/overte/blob/master/scripts/system/create/entityList/entityList.js)
