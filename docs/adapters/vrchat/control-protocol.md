# VRChat control protocol

[`avatar-parameters.json`](../../../adapters/vrchat/protocol/avatar-parameters.json) owns names, wire
types, defaults, directions and status codes. Generated bindings must stay in
sync through `adapters/vrchat/tools/generate_protocol.py`. A change to parameter meaning or
required fields requires a new protocol version. Unknown versions are unavailable.

## Parameters and interaction

The Avatar provides one `Translation` toggle beneath `XRT`. It changes requested
enablement. The host may reset that request on cancellation, rejection or a new
binding. Only host outcomes determine actual status. The version parameter
identifies compatible assets; it is not a software release number or credential.

All control parameters are unsaved and unsynchronized. They consume no network
sync bits but still count toward the Avatar's total parameter count. Installers
must preserve the exact names, validate collisions, and use the declared types.
No contact, gesture or world script writes these parameters in the standard package.

VRChat does not enforce writer identity on custom parameters. The adapter ignores
incoming status values as commands; it validates the version and enabled value
from the currently bound local Avatar. OSC endpoints are not authentication.

The menu shows **requested enablement**, not a read-only actual-state widget.
The lifecycle reports Unavailable, Off, Starting, Active, Stopping or Error.
XRTranslate shows connection and translation state on its Game Plugins page.
Startup acceptance alone is not Active. A future Avatar status
indicator consumes host status through an explicitly installed renderer; no such
indicator is shipped in this baseline.

## Binding and state reconciliation

1. The user explicitly permits Avatar control in XRTranslate. Default is off.
   The host locates one local VRChat endpoint and confirms the expected parameter
   names, types and version through a fresh OSCQuery snapshot.
2. The host binds the core to a new generation, sends an enablement reset, and
   confirms that reset only after a fresh current-Avatar snapshot reads off.
   A value cached before binding or an arbitrary UDP false is not confirmation.
3. After confirmation, a new on request creates one operation. Its language,
   input source and recognition settings are captured before startup. It uses
   the existing host output configuration. Repeated on values do not create
   duplicate work. Resource or configuration failure resets the request and
   reports Error through the host.
4. Off cancels only that operation, including pending startup and its messages.
   Other display content retains its own lifetime.
   Stopping remains visible until the host confirms release. An on request during
   cancellation is rejected; the user can retry after Off.
5. Completion, failure and desktop stop force a new reset and require its fresh
   off confirmation before another on is accepted. Rejecting on during cleanup
   has the same reset barrier. A still-on snapshot after a lost reset write cannot
   silently start a replacement operation.

Binding generations, reset tokens and operation IDs are internal host identities,
not extra Avatar parameters. Each reset token identifies one required reset within
a binding; a query started before that reset cannot confirm it. Retrying a pending
reset uses the same token. Host callbacks carry operation identity. A start completion
that arrives after cancellation is cancelled again, never accepted as a new session.
An old generation's input cannot affect the new Avatar. Since UDP packets contain
no generation, the transport must not simply stamp delayed packets with the current
generation: establish a fresh authoritative snapshot after rebinding, discard queued
input and reconcile against that binding before issuing core requests.

The host can reconcile a valid post-reset snapshot when UDP changes were lost.
The snapshot used to authorize a request must belong to the current endpoint and
Avatar. It must never replay a pre-reconnect on value as a new user request.
If endpoint/Avatar identity cannot be established, keep the binding unavailable.

## Transport and loss of connection

Use the host's shared OSC capability. Prefer OSCQuery discovery and an advertised
receiver on an available port so other OSC applications can coexist. Do not
introduce a second fixed-port 9001 listener. Advanced manual ports remain a host
transport concern and must report conflicts without taking over another process.

Poll fresh endpoint snapshots to reconcile lost UDP updates and connection health;
an unchanged toggle is normal and is not a heartbeat. Target reconciliation once
per second while control is permitted. After five seconds without a successful
query, cancel the owned operation and require a new binding. These are host policy
defaults, not promises about VRChat network timing. Ambiguous or incompatible
snapshots fail closed immediately. The core contains no timer.

On `/avatar/change`, missing required parameters, version mismatch, revoked
permission, transport loss or shutdown, retire the current binding and cancel its
operation. Switching worlds alone needs no world integration: a session continues
when the Avatar and verified connection remain valid. If the client reloads the
Avatar or loses the connection, the binding rules apply even for the same Avatar ID.

While connected, retry reset/status writes through bounded reconciliation; do not
assume UDP send success means application or display success. Coalesce parameter
values, bound parsing and queues, and rate-limit diagnostics. False and connection
loss cannot be discarded behind a stream of unrelated tracking messages.

Only the chosen local endpoint is accepted by default. Do not expose a network
control server, infer identity from display names, or overwrite other OSC tools'
configuration. OSCQuery's Windows query endpoint is local to the VRChat machine;
remote or standalone-headset control is outside the first integration target.

Official references: [Avatar parameters](https://docs.vrchat.com/docs/osc-avatar-parameters),
[OSCQuery](https://github.com/vrchat-community/osc/wiki/OSCQuery),
[parameter types and sync](https://creators.vrchat.com/avatars/animator-parameters/).
