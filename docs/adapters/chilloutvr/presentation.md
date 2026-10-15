# ChilloutVR captions

XRTranslate sends completed translations to an Avatar caption object through its
existing OSC connection. ChilloutVR synchronizes a small numeric bus; a
`CVRLuaClientBehaviour` on each viewer retains the text in an instance material.

## Ownership

`presentation/unity/` owns the shared SDF atlas, glyph lookup, shader and mesh.
`authoring/unity/CaptionAuthoring.cs` owns preset selection and previews. The
release builder checks the adapter's copies against these sources.

`adapters/chilloutvr/` owns the CCK installer, Lua receiver and
[caption manifest](../../../adapters/chilloutvr/protocol/caption.json).
XRTranslate owns sentence formatting, transmission and session cancellation.
The translation control and caption output use the same session and OSC owner.

## Rendering

The installed surface uses one mesh, one material and 128 glyph slots. Horizontal
and Board use four rows; Portrait uses sixteen. Board starts with a transparent
background. Placement, scale and colors remain editable. The same SDF assets and
proportional glyph spacing are used by the VRChat adapter; font coverage and
resource cost are recorded in
[`XrtCaptionFont.json`](../../../presentation/unity/Runtime/Caption/XrtCaptionFont.json).

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

A completed sentence replaces the pending transfer. Stop, avatar changes and
host expiry clear the display. The receiver also clears after 20 seconds without
a bus change, keeping the old packet blank until new data arrives. The host
spaces chunks by 200 ms. State synchronization can skip intermediate updates;
the next sentence starts a fresh transfer without history replay.

## Installation

The installer copies the current controller and preserves its override chain,
then adds the caption parameters and object. An editor-only record tracks the
original controller, generated assets and renderer identity. Material reuse on
another object does not transfer ownership. Renaming and reparenting the caption
within the avatar preserve its direct Lua binding.

Removal restores the previous controller and removes only owned components.
Remove captions before their translation control. Generated assets remain for
Undo and saved scene references. Editor sample text is not stored in uploaded
materials.

Runtime acceptance is recorded in [compatibility](compatibility.md).
