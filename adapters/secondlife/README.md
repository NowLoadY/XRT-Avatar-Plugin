# Second Life adapter

A wearable LSL attachment opens translation controls when touched and can show
compact captions above the attachment.
Its relay carries the shared connector messages between the simulator and the
local host bridge. Both ends make outbound connections.

- [`attachment/XRT.lsl`](attachment/XRT.lsl): owner controls and attachment lifecycle.
- [`relay/server.py`](relay/server.py): bounded, in-memory HTTPS exchange service.
- [`relay/host_bridge.mjs`](relay/host_bridge.mjs): relay to local connector transport.

See [installation](../../docs/adapters/secondlife/installation.md) and
[connector behavior](../../docs/adapters/secondlife/README.md).
