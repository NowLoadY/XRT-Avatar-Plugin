# Connector protocol

Protocol `xrt-avatar/1` carries game-component control and optional text display over
JSON WebSocket messages. XRTranslate serves `ws://127.0.0.1:<port>/xrt` while a
WebSocket connector is enabled. The settings page supplies the current address
and pairing token. A connection serves one selected game connector and one
translation owner. OSC adapters map the same lifecycle to their parameter manifests.

## Pairing and reset

The client sends this as its first frame:

```json
{"type":"hello","protocol":1,"connector":"overte","token":"<pairing token>","client_id":"<unique client ID>","capabilities":["captions"]}
```

`connector` is `overte`, `resonite` or `secondlife`. Client IDs are nonempty strings
up to 128 bytes. `captions` advertises a renderer; XRTranslate's caption output
setting separately controls delivery. Credentials stay in local runtime memory.
The Second Life bridge inserts the local pairing token after relay delivery.

The host responds with `welcome`, then a reset barrier:

```json
{"type":"welcome","protocol":1,"session":"<host session ID>"}
{"type":"reset","session":"<host session ID>","reset":1}
```

The client clears its toggle and pending display before acknowledging:

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

The compatible `caption` message carries plain text from the host-selected
producer. `stream` and `id` are opaque identities, not translation-specific
fields. Lyrics, clock text and other sources use the same message shape.

```json
{"type":"caption","session":"<host session ID>","stream":"42","id":"turn-1","revision":1,"text":"Translated text","expires_ms":15000}
{"type":"clear","session":"<host session ID>"}
```

Each caption replaces the displayed value for its stream. `(stream, id)` identifies
a result and `revision` orders its updates. Consumers reject older revisions,
expire captions within `expires_ms` (at most 15 seconds), and clear on reset or
disconnection. `clear` removes this connection's display. Text is plain Unicode,
up to 256 characters; IDs are bounded to 64 characters. Platform adapters may
shorten content to their display limits. Captions carry no audio and cannot start
sessions. Host output contains only results belonging to the connection's owner.

## Transport bounds

Frames are JSON text, at most 16 KiB inbound. The host rejects browser Origin
headers, validates the selected connector and pairing token, and accepts one
active peer. Queues are bounded; overflow closes the connection and cancels its
operation. Socket and heartbeat work run outside the UI thread. Session IDs fence
late traffic. No token is embedded in shared Avatar assets or public relay frames.

Shared clients live under `connectors/websocket/`. Platform bridges translate
transport and platform interaction. The `xrt-connector-core` SDK exposes the
catalogue, generated native contracts and pure codecs through one host dependency.
For numeric OSC displays, the host supplies a typed parameter lookup and receives
integer frames and their send interval. The same SDK maps payload width and local capability names
from each adapter's manifest; host code does not branch on a game to encode text.
Socket, send-time pacing and cancellation remain with the host transport owner.
