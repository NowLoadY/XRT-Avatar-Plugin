# Host integration

XRTranslate owns translation sessions and their resources. A platform adapter
turns supported interactions into session requests and reports the host's actual
state back to the platform. The adapter's protocol defines how requests and
feedback travel between them. The companion host implementation is maintained
on [XRTranslate main](https://github.com/NowLoadY/XRTranslate/tree/main).
Host requirements and the integration revision are recorded in
[`release/compatibility.json`](../release/compatibility.json).

## Session ownership

Each operation has an identity and an explicit owner. Startup, cancellation and
completion retain that identity across asynchronous work. The host validates
input availability and acquires resources through its session coordinator.
Conflicting input ownership is reported to the requesting adapter; acquiring a
session does not resume or take over unrelated work.

The host acknowledges an active session only after its required capture and
backend resources are ready. Cancellation includes pending startup and affects
only the operation's resources and pending output. Ownership remains retained
until cleanup completes. Late callbacks cannot reactivate a retired operation.

Adapter disablement, connection changes and application shutdown follow the
adapter's documented lifecycle and the same host cleanup contract. Platform
identity validation, reconnection and request reconciliation are defined by the
platform adapter.

## Capabilities and results

Host adapters compose existing audio, recognition, translation and output
capabilities. Shared transports retain their own resource owner and track
consumer demand. Transport availability, user enablement and session activity
are distinct states.

Sessions use XRTranslate's configured translation options and publish through
its existing result contracts. Adapter documentation states when settings are
captured and which changes apply during a session. Platform interaction does
not implicitly enable additional output destinations.

Presentation consumers receive results through a separate
[presentation contract](presentation-architecture.md). They preserve session
identity and follow owner-scoped cancellation.

## Connector composition

XRTranslate groups game components on its always-visible **Game Plugins** page.
The page's **Enable** switch is off by default and activates the selected game.
Changing games retires the previous output. Big Walk consumes
committed results for one-shot native chat delivery; avatar connectors compose
structured captions and handle game-controlled translation sessions.

The companion `xrt-connector-core` SDK owns the connector catalogue, generated
platform contracts, pure text codecs and shared lifecycle. XRTranslate's
`plugins::avatar` owns settings and validated connector intent. `avatar_host`
composes that intent into existing owned tasks. OSC profiles share the OSC plugin's transport; WebSocket connectors
share one local worker and one bounded message contract. Switching connectors
invalidates the old binding and cancels its operation before another can start.
Existing `vrc_avatar` settings identities migrate to `avatar` at deserialization.

`AvatarPlugin` composes a `DisplaySnapshot` from selected header/footer sources
and normalized host output events. The internal host `TextOutputPlugin` provides
shared format controls and one owned system sampler to OSC and Avatar. The host
fans out lyrics from one worker, activated by either destination's demand.

Message identities and remaining lifetimes survive snapshot replacement.
Native text accepts previews; buffered surfaces prefer completed captions.
Stopping translation removes only the affected stream's messages. Independent
header/footer sources remain visible until disabled or disconnected. Connection
changes revoke the entire pending display. The connector SDK owns projection,
region layout and changed-chunk selection; host transports own actual delivery.

See the [connector protocol](connector-protocol.md) and platform guides for
transport mappings and installation. The [VRChat host guide](adapters/vrchat/host-integration.md)
records its OSCQuery mapping and setting lifetimes.
