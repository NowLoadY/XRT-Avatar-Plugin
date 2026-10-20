# ChilloutVR captions

XRTranslate composes a header, messages and footer on an Avatar caption object through its
existing OSC connection. ChilloutVR synchronizes a small numeric bus; a
`CVRLuaClientBehaviour` on each viewer retains the text in an instance material.

## Ownership

The [Unity rendering guide](../../unity-caption-rendering.md) describes the shared
SDF assets, mesh, authoring helpers and package synchronization.

`adapters/chilloutvr/` owns the CCK installer, Lua receiver and
[caption manifest](../../../adapters/chilloutvr/protocol/caption.json).
The companion SDK owns board layout, packed-word encoding and the latest desired
glyph buffer. XRTranslate owns content selection, message expiry and paced
delivery through its shared OSC transport. Display content is independent of
the menu-controlled translation task.

## Rendering

Each text child uses the shared 128-glyph mesh and material. Several objects can
mirror one bus and layout on separate editable props. Horizontal and Board use
four source rows; Portrait uses sixteen. Horizontal and Portrait are transparent;
Board has a background and border. X/Y resizing wraps text and clips overflow.
The [shared presentation contract](../../presentation-architecture.md) defines
region layout, message selection and expiry.

The Lua script writes only changed material bytes and runs on local and remote
copies of the avatar. It uses the documented
[Avatar parameter API](https://docs.chilloutvr.net/cck/lua/api/avatar-api/)
and [renderer material access](https://docs.chilloutvr.net/cck/lua/examples/player-profile-picture/).
The renderer and Lua component are on the
[Avatar component list](https://docs.chilloutvr.net/cck/avatar/avatar-component-whitelist/).
No caption Animator layers or runtime mesh generation are needed.

## Synchronization

The bus contains three synchronized Int parameters: a chunk pointer and two
packed words. Each signed 32-bit word holds two BMP codepoints, high codepoint
first. Four glyphs travel per chunk using **96 synced bits** in total. The
installer checks the avatar's remaining CCK synchronization capacity.

Local `#XRT/Caption/` parameters declare version, columns and eight decoded bytes
per chunk. The host shares its text layout and paced delivery with other numeric
caption adapters, then packs the bytes into ChilloutVR words. Pointer 255 clears
the display. The Lua receiver re-reads a held pointer so late word updates can
complete that chunk.

The [shared numeric display contract](../../connector-protocol.md#transport-bounds)
owns changed-chunk selection, region scheduling and cancellation.

The host spaces chunks by 200 ms and renews an unchanged nonempty display before
the receiver's 20-second idle timeout. After that timeout, the receiver stays
blank until new data arrives and the host rebuilds the current buffer. State
synchronization can skip intermediate updates; there is no history replay or
remote delivery acknowledgement.

## Installation

The control installer copies the current controller and preserves its override
chain. Captions add one parameter bus to that working controller. Each new text
child gets its own generated mesh, material and directly bound Lua receiver;
additional objects reuse the bus. Editor records track the bus and each object's
ownership. Existing single-object records are read without reinstalling.
Assigned controllers and Lua bindings identify installations; edits follow the
[shared asset ownership rules](../../architecture.md).

Renaming or reparenting a text child within the avatar preserves its direct Lua
binding. Removal strips only the selected object's owned components. A customized
object, replacement mesh or material, foreign components and children are retained.
Removing the last text object removes the exact caption parameters; removing the
control removes its exact parameters and Advanced Avatar Settings entry. Other
controller and override edits remain in place. Editor sample text is not stored
in uploaded materials.

Runtime acceptance is recorded in [compatibility](compatibility.md).
