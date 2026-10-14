# Second Life adapter

A wearable LSL attachment opens translation controls when touched. Show compact
captions above it, or add the text receiver to your own linked display prim.
Its relay carries the shared connector messages between the simulator and the
local host bridge. Both ends make outbound connections.

- [`attachment/XRT.lsl`](attachment/XRT.lsl): owner controls and attachment lifecycle.
- [`attachment/XRT-TextDisplay.lsl`](attachment/XRT-TextDisplay.lsl): hover captions
  on a dedicated prim in the same linkset. Enable `PUBLISH_TEXT_SOURCE` in `XRT.lsl`
  to supply it with translated text.
- [`relay/server.py`](relay/server.py): bounded, in-memory HTTPS exchange service.
- [`relay/host_bridge.mjs`](relay/host_bridge.mjs): relay to local connector transport.

See [installation](../../docs/adapters/secondlife/installation.md) and
[connector behavior](../../docs/adapters/secondlife/README.md).
