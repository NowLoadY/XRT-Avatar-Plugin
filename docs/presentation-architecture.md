# Presentation architecture

A text source provides the current host-selected content. A display binds that
source to an editable game object. Presets create useful starting objects; users own
placement, shape, typography and decoration after creation.

## Text source

The shared JavaScript and Python `CaptionSource` implementations live under
`presentation/`. They accept `display-v1` snapshots and legacy captions. A
structured source exposes `header`, `messages`, `footer`, combined `text` and
`revision`; messages retain `id`, `text` and local `expiresAt` in milliseconds.
`snapshot(now, region)` selects `full`, `header`, `messages` or `footer`.

Full snapshots replace all regions and reject duplicate or older revisions.
Messages expire individually using the remaining wire lifetime. Banner updates
do not extend them. Clearing retains the revision fence; resetting a connection
starts a new fence. Control resets and translation Stop preserve structured
banners; explicit display clear and disconnect clear all regions.

Sources preserve line breaks between regions. Views wrap individual lines and
may clip, orient or decorate text without changing its source. One source can
drive several user-selected objects. Native transport budgets can shorten the
source; adapters document those budgets. Presentation never starts translation
or acquires audio.

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
or other arrangements. Board objects use the selected renderer's fonts. Users can combine transparent
lettering with their own artwork.

Preset creation applies initial dimensions and supported appearance once. A
native renderer may derive a fitted projection from the object's current font
metrics and available area. Shared wrapping supports word/CJK punctuation
boundaries, combined accents and emoji sequences. Limits belong to the selected
view, not a universal three-line source restriction. Plain-text receivers disable
rich-text parsing rather than treating translated text as markup.

The [Unity renderer](unity-caption-rendering.md) supplies one shared font and
material surface for VRChat and ChilloutVR. The connector SDK owns its bounded
glyph layout; the host owns the transmission queue; each adapter owns its native receiver. Overte
uses a native MTSDF font built from the same source and repertoire. Resonite
uses native font collections, and Second Life uses the viewer's hover-text font.

The authoring interface follows XRTranslate's light surfaces, dark gray text,
8 px corner radii and restrained blue interaction feedback. Shared browser styles
live in `presentation/designer/theme.css`. Theme changes are scoped to the tool;
they do not change the user's game or editor theme. The
[layout preview](tools/layout-preview.md) uses the same source and
profiles and supports positioning without a game client.

## Native integration

| Adapter | Text source and display |
|---|---|
| Overte | Local `XRT/CaptionSource` messages; editable native Text objects and a tablet object picker |
| Resonite | `XRT/Text`, `Header`, `Messages` and `Footer` string fields; native `ValueCopy<string>` binding to template-based TextRenderer presets or custom objects |
| Second Life | Opt-in linkset messages; separate native hover-text receiver or a creator's renderer |
| VRChat | Shared Unity SDF renderer; Avatar-specific buffered parameter receiver and editable mesh presets |
| ChilloutVR | Shared Unity SDF renderer; Avatar Lua buffer fed by packed numeric parameters |

Visibility follows the chosen object: VRChat caption boards are part of the
shared PC Avatar; ChilloutVR uses shared Avatar parameters and per-viewer Lua;
Overte creates wearable Avatar Text objects or local objects according to the
audience selection; Resonite objects follow session permissions. Second Life
world attachments show native hover text nearby while HUDs are personal.
Platform guides own assembly steps, font setup, visibility and native limits.

See [interaction](interaction.md), [connector protocol](connector-protocol.md),
[VRChat rendering](adapters/vrchat/presentation.md) and
[ChilloutVR rendering](adapters/chilloutvr/presentation.md).
