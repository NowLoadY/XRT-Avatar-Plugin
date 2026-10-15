# Connector protocol

Protocol `xrt-avatar/1` carries game-component control and optional text display over
JSON WebSocket messages. XRTranslate serves `ws://127.0.0.1:<port>/xrt` while a
WebSocket connector is enabled. The settings page supplies the current address
and pairing token. A connection serves one selected game connector and its display; translation requests retain their own operation owner. OSC adapters map the same lifecycle to their parameter manifests.

## Pairing and reset

The client sends this as its first frame:

```json
{"type":"hello","protocol":1,"connector":"overte","token":"<pairing token>","client_id":"<unique client ID>","capabilities":["captions","display-v1"]}
```

`connector` is `overte`, `resonite` or `secondlife`. Client IDs are nonempty strings
up to 128 bytes. `display-v1` advertises structured display support; `captions`
advertises the legacy plain-text receiver. The host selects one format per
connection. XRTranslate's display setting separately controls delivery. Credentials stay in local runtime memory.
The Second Life bridge inserts the local pairing token after relay delivery.

The host responds with `welcome`, then a reset barrier:

```json
{"type":"welcome","protocol":1,"session":"<host session ID>"}
{"type":"reset","session":"<host session ID>","reset":1}
```

The client clears its toggle before acknowledging. Structured display receivers
retain independent banner content across control resets; legacy receivers clear
their single caption. A new connection always begins with an empty display:

```json
{"type":"reset_ack","session":"<host session ID>","reset":1,"enabled":false}
```

Only the matching reset acknowledgement opens the request gate. Each connection,
completed operation and cancellation may establish a new reset. A client applies
reset only to its current session. Pending requests from earlier resets are
discarded; a later user action is required to start again.

## Requests and feedback

```json
{"type":"set_enabled","session":"<host session ID>","sequence":1,"enabled":true}
{"type":"state","session":"<host session ID>","status":"starting","enabled":true}
```

Request sequence numbers increase strictly within one connection, starting at 1,
and remain within JavaScript's safe integer range. A duplicated, old or malformed
request closes the connection. Status is `unavailable`, `off`, `starting`, `active`,
`stopping` or `error`; `enabled` is accepted intent, not evidence of active capture.
Clients display status separately from local intent. A delayed state must never
turn a newer local Off action back On. `reset` is the authoritative toggle clear.
An optional `message` is informational and must be rendered as text.

Every three seconds the client sends `{"type":"ping","session":"…"}`; the host
replies with `pong` and the same session. Fifteen seconds without valid messages
ends the connection. Clients apply the same deadline to host messages. Disconnect,
context replacement and plugin disable clear local intent and cancel owned work.
Reconnection starts a fresh pairing/reset lifecycle; it never restores On.

## Text display

`display` replaces the complete header, visible messages and footer:

```json
{"type":"display","session":"<session>","revision":42,"header":"Artist · Song","messages":[{"id":"42:7","text":"Meet at the entrance.","ttl_ms":6000}],"footer":"21:30"}
{"type":"clear","session":"<session>"}
```

The revision increases within a connection. Receivers reject older or duplicate
snapshots. `header` and `footer` are nullable plain-text values, each at most
128 characters. `messages` contains up to eight entries, each with an opaque
ID of at most 64 characters, at most 256 text characters and a remaining lifetime
of at most 15,000 ms. Empty regions remove their previous contents.

Each message expires independently. Queues and relays subtract their elapsed
buffering time; updating a banner cannot renew an older message. Banners remain
until their source is cleared or the connection ends. Control Stop removes the
operation's messages through a subsequent snapshot. `clear`, disconnect and
plugin disable remove all regions. Content never starts translation sessions.

The host selects content and formats text. Game-side bindings select the full
display or one region; their objects determine placement, fonts and appearance.
The protocol contains no lyric, clock or translation message variants.

Older clients receive one `caption` projection from the connector SDK:

```json
{"type":"caption","session":"<session>","stream":"display","id":"current","revision":42,"text":"Meet at the entrance.","expires_ms":6000}
```

The projection prioritizes the latest message within the existing 256-character
budget. Legacy receivers retain their existing identity, expiry and reset rules.

## Transport bounds

Frames are JSON text, at most 16 KiB inbound. The host rejects browser Origin
headers, validates the selected connector and pairing token, and accepts one
active peer. Control queues are bounded; overflow closes the connection and cancels its
operation. Display output uses a single replaceable pending snapshot. Socket and heartbeat work run outside the UI thread. Session IDs fence
late traffic. No token is embedded in shared Avatar assets or public relay frames.

Shared clients live under `connectors/websocket/`. Platform bridges translate
transport and platform interaction. The `xrt-connector-core` SDK exposes the
catalogue, generated native contracts and pure codecs through one host dependency.
For numeric OSC displays, the host supplies a typed parameter lookup and receives
integer frames and their send interval. The same SDK maps payload width and local capability names
from each adapter's manifest; host code does not branch on a game to encode text.
The SDK maps regions into the glyph buffer and selects changed chunks. Updates
replace the desired buffer and revoke pending writes that no longer match it.
A new binding, an empty display or disabling display content requests a full clear.
A present header/footer uses the first/last row; messages use the remaining rows,
with recent messages taking precedence. Layout and payload width come from the
installed capability. Cached chunks are confirmed only after actual transmission.
Messages receive up to three consecutive writes before a pending banner update.
Both groups rotate through changed chunks, so continuous messages or clock
updates cannot starve other regions. Updates preserve unaffected regions,
including during static-content renewal.
Socket, send-time pacing and cancellation remain with the host transport owner.
