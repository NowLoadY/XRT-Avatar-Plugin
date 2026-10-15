# ChilloutVR captions

XRTranslate composes a header, messages and footer on an Avatar caption object through its
existing OSC connection. ChilloutVR synchronizes a small numeric bus; a
`CVRLuaClientBehaviour` on each viewer retains the text in an instance material.

## Ownership

`presentation/unity/` owns the shared SDF atlas, glyph lookup, shader and mesh.
`authoring/unity/CaptionAuthoring.cs` owns preset selection and previews. The
release builder checks the adapter's copies against these sources.

`adapters/chilloutvr/` owns the CCK installer, Lua receiver and
[caption manifest](../../../adapters/chilloutvr/protocol/caption.json).
The companion SDK owns board layout, packed-word encoding and the latest desired
glyph buffer. XRTranslate owns content selection, message expiry and paced
delivery through its shared OSC transport. Display content is independent of
the menu-controlled translation task.

## Rendering

Each text child uses one mesh, one material and 128 glyph slots. Several children
can show the same text on separate props using a single bus and column layout. Horizontal
and Board use four rows; Portrait uses sixteen. Board starts with a transparent
background. Attach it beneath a user-authored prop to add live lettering; the
prop keeps its own mesh and materials. Placement, scale and colors remain editable. The same SDF assets and
proportional glyph spacing are used by the VRChat adapter; font coverage and
resource cost are recorded in
[`XrtCaptionFont.json`](../../../presentation/unity/Runtime/Caption/XrtCaptionFont.json).

An enabled header occupies the first row and a footer the last. The remaining
rows show the newest messages that fit, in chronological order. Empty banners
release their rows to messages. Each message expires independently; cancelling
a translation task removes its messages while preserving other messages and banners.

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

Each content update replaces the desired buffer. Only changed chunks are sent;
the shared scheduler favors messages while reserving writes for pending header
and footer changes. An unchanged region remains visible.
Pending writes that no longer match the buffer are revoked. Message expiry and
task cancellation update the affected rows. A new binding, an empty display or
disabling display content requests a whole-surface clear.

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

Renaming or reparenting a text child within the avatar preserves its direct Lua
binding. Removal strips only the selected object's owned components. A customized
object, replacement mesh or material, foreign components and children are retained.
Removing the last text object removes the exact caption parameters; removing the
control removes its exact parameters and Advanced Avatar Settings entry. Other
controller and override edits remain in place. Generated assets remain for Undo
and saved scene references. Editor sample text is not stored in uploaded materials.

Runtime acceptance is recorded in [compatibility](compatibility.md).
