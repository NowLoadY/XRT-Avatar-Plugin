# VRChat captions

The caption installer creates an editable mesh and material on the player's
Avatar. XRTranslate composes a header, messages and footer through the existing OSC owner;
VRChat synchronizes a small numeric bus and each viewer's Animator retains the
received glyphs. Other players see the text on the Avatar's surface.

## Ownership

`presentation/unity/` owns the SDK-independent SDF font, shader, mesh and material
helpers. `tools/sync_unity_presentation.py` copies these resources into the VRChat
package. The release builder checks that the copies match their source.

`adapters/vrchat/` owns the expression parameters, Parameter Driver receiver,
FX bindings, capability declaration and reversible installation. The companion
SDK owns board layout, encoding and the latest desired glyph buffer. XRTranslate
owns content selection, message expiry and paced delivery through its shared OSC
transport. Display content is independent of the menu-controlled translation task.

The [caption manifest](../../../adapters/vrchat/protocol/caption.json) defines the
wire fields and layouts. The separate [control protocol](control-protocol.md)
continues to own translation intent and runtime status.

## Display

One mesh contains a background quad and 128 glyph quads. One unlit, transparent
material renders a light rounded surface and dark text; the Board preset starts
with a transparent background for custom signs and artwork. Horizontal uses four
rows; Portrait uses sixteen. Parent, position, size and material colors remain
editable. No head attachment is imposed.

An enabled header occupies the first row and a footer the last. The remaining
rows show the newest messages that fit, in chronological order. Empty banners
release their rows to messages. Each message expires independently; cancelling
a translation task removes its messages while preserving other messages and banners.

The renderer maps Unicode BMP values through a lookup texture to the bundled
Noto Sans CJK SDF atlas. It uses proportional advances for Latin and other narrow
glyphs. Unsupported characters use the replacement glyph. Font coverage,
provenance and texture cost are recorded in
[`XrtCaptionFont.json`](../../../presentation/unity/Runtime/Caption/XrtCaptionFont.json).
The font license is packaged with the textures. This path uses no runtime font
component, script or TextMeshPro dependency.

## Synchronization and retention

The installer selects the largest bus that fits the remaining synchronized
parameter budget:

| Synced bits | Glyphs per chunk | Data bytes |
|---|---|---|
| 72 | 4 | 8 |
| 40 | 2 | 4 |
| 24 | 1 | 2 |

Each profile retains the same 128 glyph slots. The pointer selects a chunk;
each glyph is sent high byte then low byte. Pointer 255 clears the display.
Version, columns and bus width are unsynchronized capability values exposed
through OSCQuery. Cached bytes exist only in the Animator and consume no
synchronized expression memory.

The receiver repeats Parameter Driver Copy operations while the pointer is held.
A second FX layer drives all material bytes from the retained values using a
Direct Blend Tree. The display state enables Write Defaults as recommended for
direct blending; receive states use timed clips instead of zero-length loops.

Each content update replaces the desired buffer. Only changed chunks are sent,
with message rows ahead of banner rows; an unchanged region remains visible.
Pending writes that no longer match the buffer are revoked. Message expiry and
task cancellation update the affected rows. A new binding, an empty display or
disabling display content requests a whole-surface clear.

The host leaves 200 ms between sent chunks and does not accumulate catch-up
bursts. VRChat's state synchronization can skip updates, so text may briefly be
incomplete. There is no history replay or remote delivery acknowledgement.

This addressed-buffer mechanism follows
[VRCBitmapLed](https://github.com/lolosiax/VRCBitmapLed), whose pinned source is
retained in [`third_party/VRCBitmapLed`](../../../third_party/VRCBitmapLed).

## Installation ownership

Caption installation copies the current expression parameters and FX controller,
adds its own assets and records the original references. Default FX uses the SDK's
face controller. Existing controller motions and behaviors are preserved.
Removal restores the recorded references; original assets are never edited.
Caption objects are removed before their translation menu so restoration remains
ordered. Generated assets remain available for Undo and saved scene references.

## Platform support

The target is a PC Avatar with custom shaders enabled. Android Avatar shaders and
viewer safety settings can prevent the display from being rendered. Runtime
acceptance is recorded in [compatibility](compatibility.md).

Official references: [Avatar components](https://creators.vrchat.com/avatars/whitelisted-avatar-components/whitelisted-avatar-components/),
[parameter memory and synchronization](https://creators.vrchat.com/avatars/animator-parameters/),
[Parameter Driver](https://creators.vrchat.com/avatars/state-behaviors/#avatar-parameter-driver),
[FX layers](https://creators.vrchat.com/avatars/playable-layers/#fx),
[Write Defaults](https://creators.vrchat.com/avatars/#write-defaults-on-states).
