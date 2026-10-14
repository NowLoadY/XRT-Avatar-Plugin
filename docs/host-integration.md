# XRTranslate host integration

This is the integration contract for the thin host adapter. The adapter and
OSCQuery transport are **not implemented** by this repository. The audited host
baseline is XRTranslate commit `14efd87`; verify the current host before changing it.

## Registration and composition

Register `vrc_avatar` as an opt-in built-in plugin with settings and no standalone
navigation page. Its settings contain permission to accept Avatar control and
the actual connection/session status. Audio and language selection remain owned
by existing host settings. Diagnostics distinguish unsupported Avatar, unavailable
transport, conflicting input ownership and startup failure without duplicating
provider configuration.

Compose `avatar-control` in the host adapter. Route decoded Avatar events and
fresh snapshots from the OSC capability, invoke core actions on the host event
loop, and send resulting parameter writes through the same transport owner.
Network I/O and queries stay off the UI thread.

Keep one core controller alive until all of its operations are terminal. Process
one input and all returned effects in order before delivering another input or
callback. `Start` and `Cancel` enqueue work; synchronous host results must be
queued rather than reentering the core while old feedback effects remain.
Discard feedback writes whose generation no longer matches the endpoint binding.
For `ResetEnabled`, write false and initiate a fresh authoritative query associated
with that reset token. Call `confirm_reset` with the token only after observing
false for the same Avatar; an older query or a UDP echo cannot open this barrier.
Reconcile the pending reset even when no further parameter changes arrive.

## Session acquisition

Use `session_coordinator::TranslationTask` with live input, a
`PluginSessionBinding` and an opaque plugin owner carrying the core operation ID.
Capture the configured languages, recognition profiles and audio source once.
Use the shared scheduler and existing output policy; the menu must not silently
turn on Chatbox, TTS or a new audio route.

The initial acquisition policy is exclusive for an overlapping live input:
reject a new Avatar request if an unrelated live owner already consumes that
input. Do not stop, adopt or duplicate the other session. Audio graph overlap,
including `Both`, is resolved by the host. Independent tasks remain usable.
The user can stop the conflicting session or continue controlling it on desktop.

Use owner-scoped cancellation for Off, including the pending-start path. In the
audited host, `stop_task_owner` is the appropriate starting point. The global
`start()` and `stop()` functions are not this contract: they include unrelated
host, meeting, media and other tasks.

The current `start_translation_task` also calls `set_live_input`, which can resume
other owners on the same input. Introduce focused capture acquisition that does
not change another owner's pause state before wiring Avatar start. Merely assigning
a different session owner does not solve this coupling.

Report Started only when capture and the shared session are actually ready.
Report Stopped/Failed only after that operation's pending and active resources
are released. A failure that still needs cleanup remains a cancellation in
progress. Startup/cancel callbacks carry the operation ID, not just the current
Avatar or a global boolean. Persist settings, never an active operation.

## Results and outputs

Consume normalized session events, not raw backend messages or text scraped from
UI history. Preserve result identities, revisions and replacements. The Avatar
session uses the existing host output path; owner cancellation must invalidate
unsent captions, queued TTS and future display updates for that owner.

Mute-follow is an audio admission policy, not a stopped session. It must not
claim that capture resources have been released. The desktop explains any mute
gate; automatic unmuting or changing VRChat input controls is outside this adapter.

## Required host changes

| Current host capability | Integration requirement |
|---|---|
| OSC receiver reads `MuteSelf` | Add bounded typed parameter/avatar-change events and current binding validation |
| OSC transport follows Chatbox enablement | Track receive capability demand separately from text-output preference |
| Fixed receive port | Add shared OSCQuery discovery, snapshots, advertisement and connection health |
| Writer sends Chatbox and typing | Add parameter feedback through the same transport owner |
| Generic owned tasks | Add isolated live-input acquisition and operation-aware startup outcomes |
| Owner result cancellation | Verify every external output drops pending cancelled-owner work |
| Static plugin catalogue | Add opt-in descriptor, settings contribution, lifecycle hooks and dependency pin |

Do not add a public REST control service, emulate desktop hotkeys, consume VRChat
private APIs, inspect process memory or fork translation infrastructure to bypass
these seams. The companion package needs no world-side installation.

Host contracts: [plugin architecture](https://github.com/NowLoadY/XRTranslate/blob/main/docs/plugin-architecture.md),
[platform architecture](https://github.com/NowLoadY/XRTranslate/blob/main/docs/platform-architecture.md).
