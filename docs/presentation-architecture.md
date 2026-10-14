# Presentation architecture

Presentation adapters turn XRTranslate results into platform-supported displays,
including Avatar translation bubbles. They consume the host's results and remain
separate from session control. Overte supplies a local caption entity; Resonite
and Second Life render through their native text components. Platform guides
declare visibility and installation requirements.

## Contract

A presentation adapter preserves the session and result identities needed for
updates, replacements and cancellation. Cancelling one session removes its
pending display work without clearing unrelated content. Display buffering and
update rates are bounded by the target platform's capabilities.

Each adapter declares where content appears, who can see it and which platform
features it requires. Visibility follows the user's configured output choices.
Rendering consumes existing translation results; it does not acquire audio or
start a translation session.

Platform-specific transport, encoding, text rendering and asset installation
belong to the presentation adapter. Its compatibility documentation records the
supported behavior and runtime verification.

WebSocket presentation follows the [connector protocol](connector-protocol.md).
See [VRChat presentation](adapters/vrchat/presentation.md) for that platform's
display capabilities and constraints.
