# VRChat host integration

XRTranslate implements this integration as an opt-in built-in plugin. The Avatar
package and control core are maintained in this repository; the desktop adapter,
shared transport and session composition are maintained in XRTranslate. A compatible
host build includes **Settings → Plugins → Avatar translation controls**.

## Composition

| Host module | Responsibility |
|---|---|
| `plugins::avatar` | Adapt verified snapshots to the control core, retain binding/reset identity, queue host actions and parameter feedback |
| `avatar_host` | Map operations to owned translation tasks, capture startup intent and report readiness/cleanup outcomes |
| `plugins::osc::transport` | Shared receive owner, local OSCQuery discovery/advertisement, bounded events, fresh queries and epoch-checked parameter writes |
| `session_coordinator` | Neutral task, input, owner and output contracts |
| Shared audio/network/output modules | Capture, recognition, translation, session completion and cancellation of owned output |

The plugin has a settings contribution and no separate navigation page. It does
not receive the root application object. The host adapter composes the existing
capabilities; inference and backend protocols contain no Avatar-specific branch.
The control crate is a pinned source dependency, not a dynamically loaded plugin.

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

The Avatar session publishes through the existing host output path. Its menu
never enables Chatbox, overlay, TTS or a new audio route automatically. Normalized
results retain identity, revisions and replacements; cancellation removes pending
content for that stream without clearing unrelated output.

Language/source selection and recognition profiles apply to the next Avatar
start. Device and prompt configuration are resolved when the shared session is
created; later desktop edits to ordinary Host sessions do not rewrite this
plugin-owned session. Shared output formatting, filtering, visibility and mute
admission continue to follow their existing runtime settings. TTS begins with
the configured enablement and follows the global TTS switch for sessions with
an allocated TTS output capability. Turning playback off takes effect immediately.

Mute-follow suppresses audio admission without releasing capture resources. It
is not an Off acknowledgement and must not change VRChat's mute/input controls.
A future Avatar bubble is a separate result consumer under the
[presentation contract](presentation.md); it does not change this
control or resource ownership.

## Integration status

Host and transport source paths are implemented. Unity import, Avatar upload,
VRChat discovery against a real client and end-to-end gameplay behavior remain
unverified. Use [compatibility](compatibility.md) for release acceptance and
[installation](installation.md) for the paired source workflow.

Host contracts: [plugin architecture](https://github.com/NowLoadY/XRTranslate/blob/401766d02499217a8b9a83c66465eeca589f6f2c/docs/plugin-architecture.md),
[platform architecture](https://github.com/NowLoadY/XRTranslate/blob/main/docs/platform-architecture.md).
