# Big Walk bridge

XRTranslate owns committed translation selection, formatting and its short send
queue. The game bridge owns player readiness and native chat invocation. Chat
entries are append-only; the adapter does not participate in Avatar caption
snapshots, banners or session controls.

## Native binding

The bridge resolves BepInEx's generated interop assemblies at runtime:

`Mirror.NetworkClient.localPlayer` → `GetComponent<PlayerNetworking>()` →
`CmdSendTextChatMessage(string)`.

This is the ordinary local client's command. Mirror and the game continue to own
delivery, visibility and recipient rules. A postfix on
`Mirror.NetworkClient.NetworkEarlyUpdate` executes requests on Unity's main
thread. No game binaries or Unity editor are required to build the bridge.

The integration method was verified against the published
[BigTalk ConversationStarters](https://github.com/Gogogadgetjustice/bigwalkmods/tree/main/The%20Mods/ConversationStarters)
package. This adapter is independently implemented. BigTalk's additional local
grey notification is not part of the native chat command and is not reproduced.

## Local protocol

The game listens on **127.0.0.1:38647**, accepting one client. Frames are UTF-8 JSON
objects terminated by a newline; a frame is limited to 16 KiB. Version 1:

```json
{"type":"state","protocol":1,"ready":true,"session":"…","max_utf16":256,"error":null}
{"type":"chat","id":"…","session":"…","text":"The entrance is on the right."}
{"type":"result","id":"…","status":"sent"}
```

State is sent on connection and every second. `session` changes when the local
player identity changes. The host echoes it, clears pending text on a change,
and sends only while ready. `max_utf16` is the bridge's 256-code-unit readability
cap; it is not a claimed native game limit.

Results are `sent`, `not_ready`, `busy`, `too_long` or `failed`. `sent` means that
the normal native command returned successfully, not a remote delivery receipt.
IDs are at most 80 characters. The bridge remembers the last 256 request results
and returns the same result for a repeated ID. The host never retries a send
after a timeout or connection loss.

There is at most one pending native invocation. It expires after two seconds and
is revoked when its connection or player session ends. Native sends are spaced
by at least two seconds. Network reads, JSON processing and writes run away from
the main thread; native binding and invocation run on the main thread.

## Build

With the .NET 8 SDK and Python 3:

```sh
python3 tools/build_bigwalk.py
```

The script restores BepInEx's compile references, builds the DLL and writes one
installable ZIP to `dist/bigwalk/`. BepInEx and game assemblies are not bundled.
To use an existing BepInEx installation's core references instead of NuGet:

```sh
python3 tools/build_bigwalk.py --bepinex-core /path/to/BepInEx/core
```

The package targets .NET 6, the runtime used by BepInEx IL2CPP. Its default
reference is **6.0.0-be.755**, matching BigTalk's documented mod-loader package.
Successful compilation verifies assembly/API references; native chat delivery,
guest-player sending and game updates require an actual game session.

References: [BepInEx IL2CPP plugin development](https://docs.bepinex.dev/master/articles/dev_guide/plugin_tutorial/2_plugin_start.html),
[Mirror client loop](https://github.com/MirrorNetworking/Mirror/blob/master/Assets/Mirror/Core/NetworkClient.cs).
