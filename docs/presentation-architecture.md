# Presentation architecture

Presentation adapters turn XRTranslate caption results into game-supported text.
The shared presentation policy owns layout, color and lifetime. Each platform
adapter owns placement, visibility, rendering and cleanup of its native objects.

## Shared appearance

[`presentation/bubble-style.json`](../presentation/bubble-style.json) is the
style source. JavaScript and Python helpers apply the same plain-text layout.
`tools/generate_presentation.py` updates the JavaScript style binding.
Open [`tools/preview-presentation.html`](../tools/preview-presentation.html) to
inspect wrapping, colors and timing without a game client. The preview uses the
same layout helper; native fonts and placement are verified in each platform.

| Property | Default |
|---|---|
| Content | One current translation, up to three lines |
| Text | Soft white, regular weight; left-aligned where supported |
| Surface | Dark neutral background at 72% opacity where supported |
| Spacing | 1.25 line spacing and even padding |
| Width | 0.44–0.90 m reference width; native font measurement may reduce line length |
| Placement | Stable, below the main sightline; no following other players |
| Motion | 160 ms fade in, 220 ms fade out where supported; no bounce or scale animation |
| Lifetime | No longer than the host expiry or eight seconds |
| Idle | No visible surface, logo, status label or placeholder text |

Text wraps at word or CJK punctuation boundaries when possible. Wide characters count as two visual
columns; combining accents, surrogate pairs and emoji sequences stay together.
Overflow ends with an ellipsis rather than scrolling, shrinking the font or
adding pages. Full translation history remains in XRTranslate. Platform byte
limits are applied without cutting UTF-8 characters.

Adapters that control surface dimensions use native font metrics to fit text
without shrinking its size or exceeding the reference width. Adapters that edit
a selected native text component preserve its font, bounds and placement. Background, rounding and alpha animation are applied
only through supported native features. A native text-only display uses the
same text treatment and expiry without simulating a separate panel.

## Result lifetime

`(stream, id, revision)` identifies an update. Older or duplicate revisions cannot
replace visible text or restart its lifetime. Expiry and display clearing retain
a bounded revision history; a new session/reset retires that history. One result
replaces the previous display, with no stacked queue of bubbles.

A local Stop clears visible and pending content. Observed resets, disconnections
and avatar changes also clear the display. Normal expiry may fade; cancellation
skips the fade. Native connection loss is handled within the platform's detection
or heartbeat deadline. Relay adapters deduct buffering time from the remaining
lifetime. Reconnection cannot restore a previous caption.

## Ownership and visibility

Presentation consumes results through the [connector protocol](connector-protocol.md).
It does not request audio, start translation or reinterpret text from another UI.
Platform objects are created or selected explicitly and never attached to
unrelated players. Editing an existing text component records its original
properties and restores only values still owned by the adapter.

| Adapter | Display | Visibility |
|---|---|---|
| Overte | Locally owned text entity with background and alpha fade | Local wearer |
| Resonite | Selected native TextRenderer | Follows the selected object's session visibility |
| Second Life | Native attachment hover text | Nearby residents for world attachments; wearer for HUD placement |
| VRChat / ChilloutVR | Menu control packages | Avatar bubble renderer requires a platform-specific implementation |

Shared output requires an explicit display choice. Captions contain only results
from the selected translation owner; mixed audio does not identify individual
speakers. Platform setup guides own their placement and visibility instructions.

[Interaction](interaction.md) defines control behavior. Platform details remain
in each adapter's documentation, including [VRChat presentation](adapters/vrchat/presentation.md).
