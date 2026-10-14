# Host integration

XRTranslate owns translation sessions and their resources. A platform adapter
turns supported interactions into session requests and reports the host's actual
state back to the platform. The adapter's protocol defines how requests and
feedback travel between them.

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

## Implementations

[VRChat host integration](adapters/vrchat/host-integration.md) describes its
implemented host modules, control lifecycle, transport and settings behavior.
Platform-specific compatibility is recorded alongside each adapter.
