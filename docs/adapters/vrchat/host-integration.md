# VRChat host integration

XRTranslate implements this integration as an opt-in built-in plugin. The Avatar
package and control core are maintained in this repository; the desktop adapter,
shared transport and session composition are maintained in XRTranslate. A compatible
host build includes the **Avatar** plugin and its display page.

## Composition

| Host module | Responsibility |
|---|---|
| `plugins::avatar` | Bind game connections, adapt menu intent to the control core, compose display content and deliver snapshots |
| `avatar_host` | Map operations to owned translation tasks, capture startup intent and report readiness/cleanup outcomes |
| `plugins::osc::transport` | Shared receive owner, local OSCQuery discovery/advertisement, bounded events, fresh queries and epoch-checked parameter writes |
| `plugins::text_output` | Shared message formatting, banner selection, lyrics inputs and system metrics |
| `session_coordinator` | Neutral task, input, owner and host-output subscription contracts |
| `xrt_connector_core` | Companion SDK: connector contracts, control lifecycle, display snapshots and board encoding |
| Shared audio/network/output modules | Capture, recognition, translation, session completion and cancellation of owned output |

The plugin has connection settings and an **Avatar** page for Header, Messages
and Footer. It does not receive the root application object. The host adapter composes the existing
capabilities; inference and backend protocols contain no Avatar-specific branch.
The companion SDK is a pinned source dependency.

The OSC owner serves Avatar control and existing mute-follow. Receive demand is
separate from Chatbox-output enablement. Avatar control advertises an available
local UDP port through OSCQuery; it does not compete for a second fixed listener
on 9001. Existing Chatbox output retains its configured destination and formatting.
Discovery, queries and sockets run off the UI thread. Invalid, ambiguous or stale
endpoint identity cannot authorize startup.

## Control lifecycle

One controller remains alive until its operations are terminal. The host event
loop processes effects in order and queues callbacks for a later poll. A Start
superseded by Cancel in the same poll acquires no capture resources. Feedback
writes are discarded when their binding generation or transport epoch is stale.

`ResetEnabled` writes false and requests a fresh authoritative snapshot tagged
with the reset token. Only a false snapshot for that token and the current Avatar
confirms the reset. UDP values are hints, not user-intent authority. Regular fresh
snapshots reconcile lost updates and retry pending reset/status feedback even
when the toggle is unchanged. Confirmation opens the request gate; it never
starts translation by itself. The [control protocol](control-protocol.md) owns
state meanings, reset rules and connection-loss behavior.

Disabling the plugin, endpoint loss, Avatar changes and shutdown retire the
binding and cancel its operation. Disabling Chatbox output alone does not retire
a control binding. No active operation or prior On request is persisted.

## Session acquisition and cleanup

The host creates `TranslationTask` with `TranslationInput::ExclusiveLive`, a
`PluginSessionBinding` and an opaque owner derived from the operation ID. This
uses the normal live translation pipeline and scheduler. Language selection,
audio-source choice and recognition profiles are captured when the request is
accepted, including while the shared backend is starting.

An exclusive input lease rejects overlap with another live task, including
pending and paused tasks. Ordinary live requests also cannot acquire an existing
exclusive lease. `Both` overlaps either constituent source. The host neither
adopts nor cancels the conflicting owner, and acquisition does not resume another
owner's input. Text, file and non-overlapping live tasks remain independent.

Off uses owner-scoped cancellation, including pending startup. It does not call
the global application Start/Stop controls. Capture groups stop, queued results
are invalidated and session-owned TTS is cancelled. The owner and input lease
remain retained until the network workers have actually exited. Only then is
Stopped or Failed delivered to the core. A late startup callback cannot resurrect
a cancelled operation.

Started is reported only when all requested channels are capturing and their
backend sessions are ready. The desktop Stop control still stops the application
as a whole; its Avatar operation follows the same cleanup acknowledgement.
Closing an Avatar-owned input on desktop also ends that operation. Unrelated
session completion does not alter Avatar state.

## Settings and output

Display composition subscribes to the existing host output path, including tasks
started from the desktop and the Avatar menu. Message settings select audio
sources, formatting, count and lifetime. The numeric board receives completed
text; transient recognition previews do not replace a visible translation.
Cancelling a stream removes its messages without clearing other streams or the
header and footer. Display enablement is independent of menu translation status.

The menu never enables Chatbox, overlay, TTS or a new audio route automatically.

Language/source selection and recognition profiles apply to the next Avatar
start. Device and prompt configuration are resolved when the shared session is
created; later desktop edits to ordinary Host sessions do not rewrite this
plugin-owned session. Shared output formatting, filtering, visibility and mute
admission continue to follow their existing runtime settings. TTS begins with
the configured enablement and follows the global TTS switch for sessions with
an allocated TTS output capability. Turning playback off takes effect immediately.

Mute-follow suppresses audio admission without releasing capture resources. It
is not an Off acknowledgement and must not change VRChat's mute/input controls.
Headers and footers use the shared text-output service for custom text, time,
system metrics or lyrics. They remain active while menu translation is off.
OSCQuery advertises the installed board's capacity. The companion SDK maps the
composed snapshot to its rows and retains the latest desired chunks; the host
transport sends changed chunks through paced, revocable writes. Message expiry
updates only the affected content. Disabling display or retiring the connection
requests a full clear. See the
[presentation contract](presentation.md) for the renderer and wire format.

## Integration status

Host and transport source paths are implemented. Unity import, Avatar upload,
VRChat discovery against a real client and end-to-end gameplay behavior remain
unverified. Use [compatibility](compatibility.md) for release acceptance and
[installation](installation.md) for the paired source workflow.

Host contracts: [plugin architecture](https://github.com/NowLoadY/XRTranslate/blob/main/docs/plugin-architecture.md),
[platform architecture](https://github.com/NowLoadY/XRTranslate/blob/main/docs/platform-architecture.md).
