# Presentation architecture

A caption source provides the current translation. A display binds that source
to an editable game object. Presets create useful starting objects; users own
placement, shape, typography and decoration after creation.

## Text source

The shared JavaScript and Python `CaptionSource` implementations live under
`presentation/`. They accept normalized connector results and expose current
plain text independently of wrapping, surface dimensions or font choice.
A source snapshot contains `text`, `stream`, `id`, `revision` and `expiresAt` in
milliseconds. A missing snapshot means the source is empty.

`(stream, id, revision)` identifies an update. A bounded history rejects duplicate
and retired results. Clearing a display does not erase this history; session
reset does. Expiry uses the host's remaining lifetime, capped at eight seconds.
A local Stop or observed reset/disconnection clears the source and its consumers.
Native connection loss follows the platform's detection deadline. Relays deduct
buffering time, including cached-response retries.

Text sources preserve line breaks and are not preformatted to fit a bubble.
Native transport budgets can shorten the source; adapters document those budgets.
A view may wrap, clip, orient or decorate the text without changing the source.
One source can drive several user-selected objects. Presentation never starts
translation, acquires audio or infers another player's identity.

## Display binding

An adapter binds to a specific object selected or created by the user. Live
updates change the text field and, when explicitly enabled, visibility. They do
not reset transforms, dimensions, colors, fonts, materials or parentage.

The same object can be mounted on an avatar, a handheld sign, a board or an
editable world object, subject to native permissions. No anchor or head position
is part of the source contract. Platform adapters own native field binding and
object access; the shared layer does not emulate unsupported game APIs.

Bindings to existing objects record the original values they change. Unbinding
restores only fields still owned by that binding and preserves the object.
Objects handed to the user after creation are editable assets rather than
per-message resources. Preview text is explicit authoring state and never enters
the live translation source.

## Presets and appearance

[`presentation/presets.json`](../presentation/presets.json) defines three starting
layouts over [`bubble-style.json`](../presentation/bubble-style.json). The
binding generator publishes the same profiles to JavaScript and Python.

| Preset | Starting object |
|---|---|
| Horizontal | Wide text surface with a light neutral background |
| Portrait | Narrow, taller surface with normal readable line wrapping |
| Board | Transparent lettering that can sit over the user's artwork or board |

Portrait specifies a surface shape, not top-to-bottom glyph writing. Custom
renderers can use the unwrapped source for vertical typesetting, curved lettering
or other arrangements. Board objects use native or user-provided fonts; they do
not promise a bundled handwriting font or paint arbitrary textures by themselves.

Preset creation applies initial dimensions and supported appearance once. A
native renderer may derive a fitted projection from the object's current font
metrics and available area. Shared wrapping supports word/CJK punctuation
boundaries, combined accents and emoji sequences. Limits belong to the selected
view, not a universal three-line source restriction. Plain-text receivers disable
rich-text parsing rather than treating translated text as markup.

The authoring interface follows XRTranslate's light surfaces, dark gray text,
8 px corner radii and restrained blue interaction feedback. Shared browser styles
live in `presentation/designer/theme.css`. Theme changes are scoped to the tool;
they do not change the user's game or editor theme. The
[layout preview](../tools/preview-presentation.html) uses the same source and
profiles and supports positioning without a game client.

## Native integration

| Adapter | Text source and display |
|---|---|
| Overte | Local `XRT/CaptionSource` messages; editable native Text objects and a tablet object picker |
| Resonite | `XRT/Text.Value`; native `ValueCopy<string>` binding to template-based TextRenderer presets or custom objects |
| Second Life | Opt-in linkset messages; separate native hover-text receiver or a creator's renderer |
| VRChat / ChilloutVR | Avatar menu controls; parameter-driven glyph rendering has a platform-specific implementation contract |

Visibility follows the chosen object: Overte presets are local; Resonite objects
follow session permissions; Second Life world attachments are visible nearby
while HUDs are personal. Shared objects require an explicit user selection.
Platform guides own assembly steps, font setup, visibility and native limits.

See [interaction](interaction.md), [connector protocol](connector-protocol.md),
[VRChat rendering](adapters/vrchat/presentation.md) and
[ChilloutVR rendering](adapters/chilloutvr/presentation.md).
