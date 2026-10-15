# ChilloutVR compatibility

| Surface | Implementation |
| --- | --- |
| Avatar menu | CCK Advanced Avatar Settings Bool toggle |
| Caption object | Shared SDF mesh and material, 128 glyph slots |
| Caption receiver | `CVRLuaClientBehaviour`, local and remote Avatar copies |
| Caption synchronization | Pointer + two packed Int32 words, 96 synced bits |
| Connector | Shared OSCQuery discovery, typed snapshots and OSC writes |
| Peer selection | Native `ChilloutVR-` service prefix and HOST_INFO name |
| Lifecycle | Avatar and Avatar-profile changes retire the current binding |

The installer targets public **CCK 4.0.1** and its selected **Unity 2022.3.58f1**,
from the [official CCK download service](https://api.chilloutvr.net/1/public/cck/info).
Its SDK types, fields, synchronization accounting and Lua importer were checked
against that archive. The receiver's packed-word decoding, replacement, clearing
and inactivity expiry passed a Lua 5.2 simulation. Unity compilation, Avatar
upload and in-game acceptance have not been performed.

Use the platform's [current setup guidance](https://docs.chilloutvr.net/cck/setup/)
for your authoring environment. SDK code is not included in the package. XRT's
installation scripts compile in the project's Editor assembly alongside CCK's
public types; the uploaded object uses CCK's Lua component.

Editor acceptance includes duplicating an installed Avatar, saving a scene copy
with the original closed, and adding/removing controls and text on either copy.
Verify independent assets, Undo/Redo and failed-operation rollback. Replace both
mesh and material before copying, then confirm removing XRT keeps the customized
object and removes its Lua receiver.

Automatic discovery uses the native OSCQuery prefix. Restore it if a custom
`--osc-query-prefix` prevents connection. Custom shader and Avatar script content
settings affect caption visibility.
