# ChilloutVR presentation

The current Avatar package installs translation controls. It does not install a
caption receiver, a text renderer or a text-bearing preset. Presentation follows
the [shared appearance and lifecycle contract](../../presentation-architecture.md)
and uses the existing XRTranslate connector owner.

## Runtime capabilities

ChilloutVR's documented Avatar OSC route controls numeric animator parameters.
The string argument accepted by `/chatbox/input` belongs to the game's Chatbox;
it does not assign text to an arbitrary Avatar object. Chatbox output remains
in XRTranslate's OSC plugin. See the [official OSC endpoints](https://docs.chilloutvr.net/chilloutvr/game/osc/).

The [Avatar component list](https://docs.chilloutvr.net/cck/avatar/avatar-component-whitelist/)
includes `CVRLuaClientBehaviour` and `CVRMaterialDriver`. Public CCK 4.0.1 exposes
Lua's `localOnly` setting and named object bindings. The official
[Lua Avatar API](https://docs.chilloutvr.net/cck/lua/api/avatar-api/) reads and writes
Bool, Int and Float animator values, so a Lua receiver may consume an explicitly
encoded numeric caption channel. That channel must define framing, replacement,
expiry and reset before it can carry a dynamic text source.

The generic [Lua text recipe](https://docs.chilloutvr.net/cck/lua/recipes/outputting-text/)
shows TextMeshPro assignment, but the documented Avatar component list does not
list TextMeshPro or Canvas. This recipe alone does not establish an uploadable
Avatar text binding. A native text backend requires confirmation for the actual
Avatar components and client version. The documented
[Lua runtime mesh example](https://docs.chilloutvr.net/cck/lua/examples/runtime-mesh-gen/)
is restricted to world scripts and cannot serve as an Avatar mesh backend.

The separate [C# WASM scripting system](https://docs.chilloutvr.net/scripting/getting-started/overview/)
is documented as closed testing. It is not a dependency of the public Avatar
package. Its [FileStorage API](https://docs.chilloutvr.net/scripting/apis/file-storage/)
is world-only and does not provide an Avatar connection to a desktop text file.

## Installed rendering assets

A renderer using the established Avatar surface bakes glyph geometry and a font
atlas in the editor, then drives material properties through supported animator
bindings or [CVRMaterialDriver](https://docs.chilloutvr.net/cck/components/cvr-material-driver/).
It can share glyph mapping, layout profiles, textures and shader policy with
other numeric Avatar backends. ChilloutVR-specific parameter and material binding
remain in this adapter.

The artifact declares its renderer identity, font coverage, glyph capacity,
texture/material cost and numeric channel. Horizontal, vertical and board
presets select layout and placement over that same renderer. A preset is offered
for installation together with a working host encoder and receiver; static
sample text is an editor preview, not a connected subtitle object.

A native string renderer, once established for uploaded Avatars, implements the
same source binding and lifetime contract. Its text component and script bindings
remain a separate backend so glyph encoding is not forced onto platforms with a
working native text route.

## Ownership and visibility

Each display belongs to the chosen Avatar and receives only its selected
translation output. Editing a display records the created assets and prior
references, and removal restores only values still owned by the installation.
Placement under an Avatar object does not alter that object's other components.

Local captions use local parameters and a wearer-only receiver. Shared output
requires its own explicit synchronization path and client acceptance. Changing
the display preset or placement never implicitly changes the audience. Stop,
Avatar change and connection reset clear both buffered and visible captions.
