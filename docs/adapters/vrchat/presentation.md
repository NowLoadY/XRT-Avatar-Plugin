# VRChat captions

The caption installer creates an editable mesh and material on the player's
Avatar. XRTranslate composes a header, messages and footer through the existing OSC owner;
VRChat synchronizes a small numeric bus and each viewer's Animator retains the
received glyphs. Other players see the text on the Avatar's surface.

## Ownership

The [shared Unity renderer](../../unity-caption-rendering.md) owns the SDF font,
shader, mesh, material helpers and package synchronization.

`adapters/vrchat/` owns the expression parameters, Parameter Driver receiver,
FX bindings, capability declaration and reversible installation. The companion
SDK owns board layout, encoding and the latest desired glyph buffer. XRTranslate
owns content selection, message expiry and paced delivery through its shared OSC
transport. Display content is independent of the menu-controlled translation task.

The [caption manifest](../../../adapters/vrchat/protocol/caption.json) defines the
wire fields and layouts. The separate [control protocol](control-protocol.md)
continues to own translation intent and runtime status.

## Display

Each text object uses the shared 128-glyph mesh and material. Horizontal and Board
receive four source rows; Portrait receives sixteen. Horizontal and Portrait are
transparent by default; Board has a background, border and natural text fitting.
Resize X/Y to change the text area, or scale XYZ uniformly to enlarge the whole display.
Appearance controls edit each object's material. Objects mirror one receiver and layout
without adding synchronized parameters; each retains its own editable mesh,
material and transform. They can follow a prop, bone or animated parent.

The [shared presentation contract](../../presentation-architecture.md) defines
region layout, message selection and expiry. The Unity renderer documents font
coverage, resource costs and appearance controls.

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

The connector SDK uses the manifest's optional begin/commit commands to stage
changed chunks in local Animator parameters, then replace the visible buffer.
Streaming updates coalesce behind the frame in flight. Clear resets both buffers
and returns to direct writes, preserving compatibility with older hosts. Older
receivers ignore these commands and continue displaying chunks directly. Hosts
must adopt the updated connector SDK to use complete-frame refresh; no additional
synchronized expression bits are needed.

The [shared numeric display contract](../../connector-protocol.md#transport-bounds)
owns changed-chunk selection, region scheduling and cancellation.

The host leaves 200 ms between sent chunks and does not accumulate catch-up
bursts. VRChat's state synchronization can skip updates, so text may briefly be
incomplete. There is no history replay or remote delivery acknowledgement.

This addressed-buffer mechanism follows
[VRCBitmapLed](https://github.com/lolosiax/VRCBitmapLed), whose pinned source is
retained in [`third_party/VRCBitmapLed`](../../../third_party/VRCBitmapLed).

## Installation ownership

The first text object copies the current expression parameters and FX controller,
then adds one receiver and display layer. Default FX uses the SDK's face controller.
The installation record tracks the added parameters, layer references, generated
clips and each text renderer. Later objects add bindings to those clips. Operations
follow the [shared asset ownership rules](../../architecture.md).

Renderer identity, owned mesh/material and local paths resolve text objects.
Bindings are derived from the current hierarchy during authoring operations and
SDK preprocessing; moving objects alone does not write assets or add Undo steps.
Preprocessing saves a separate controller and clips for the build Avatar, then
postprocessing removes them. The original scene need not be open. Older records
are read without saving during inspection and persisted by an authoring operation.

Removal deletes the selected object's bindings and matching generated components.
The final object also removes the recorded caption parameters and layers. User
menu controls, animator additions and customized objects remain. Remove caption
objects before their translation menu.

## Platform support

The target is a PC Avatar with custom shaders enabled. Android Avatar shaders and
viewer safety settings can prevent the display from being rendered. Runtime
acceptance is recorded in [compatibility](compatibility.md).

Official references: [Avatar components](https://creators.vrchat.com/avatars/whitelisted-avatar-components/whitelisted-avatar-components/),
[parameter memory and synchronization](https://creators.vrchat.com/avatars/animator-parameters/),
[Parameter Driver](https://creators.vrchat.com/avatars/state-behaviors/#avatar-parameter-driver),
[FX layers](https://creators.vrchat.com/avatars/playable-layers/#fx),
[Write Defaults](https://creators.vrchat.com/avatars/#write-defaults-on-states).
