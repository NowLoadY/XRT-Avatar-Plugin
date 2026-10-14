# Install the Second Life attachment

Use an editable attachment or HUD owned by your avatar. The script occupies one
dedicated prim so its hover text does not replace text maintained by another
script. Keep a copy of the original object before adding the adapter.

## 1. Configure the relay

The relay runs on a server reachable from Second Life and your computer. It
requires Python 3.10 or later and an HTTPS reverse proxy with a valid certificate.
The proxy forwards to `127.0.0.1:8788` and preserves the `Authorization` header.
The relay binds to loopback by default; no XRTranslate port is exposed publicly.

Generate two independent tokens with `python3 -c 'import secrets; print(secrets.token_urlsafe(32))'`.
Set them in the relay service's environment, then run:

```sh
export XRT_RELAY_CLIENT_TOKEN='your-client-token'
export XRT_RELAY_HOST_TOKEN='your-different-host-token'
python3 adapters/secondlife/relay/server.py
```

Configure the proxy's request body limit to at most 16 KiB and disable body and
authorization logging. The provided server serves one pair, retains no data on
disk and accepts only authenticated exchange requests. A separate process and
token pair provide a separate user's relay. Stop the service to clear all queues.

## 2. Connect XRTranslate

Enable the Avatar plugin, select **Second Life**, and copy the local connector
WebSocket URL and pairing token shown by XRTranslate. Run the bridge on the same
computer using Node.js 22.4 or later:

```sh
export XRT_RELAY_URL='https://relay.example.net'
export XRT_RELAY_HOST_TOKEN='your-different-host-token'
export XRT_CONNECTOR_URL='ws://127.0.0.1:PORT/xrt'
export XRT_CONNECTOR_TOKEN='the-token-shown-by-XRTranslate'
node adapters/secondlife/relay/host_bridge.mjs
```

`XRT_RELAY_URL` is the HTTPS origin without a path. The local connector URL and
token belong to the running XRTranslate instance. Update them after restarting
the host. The bridge adds the local token when opening the local WebSocket; it
does not send that token to the relay.

## 3. Add the attachment

1. Copy [`XRT.lsl`](../../../adapters/secondlife/attachment/XRT.lsl) into a new LSL
   script in the dedicated prim and compile it with Mono.
2. Set `RELAY_URL`, `RELAY_CLIENT_TOKEN`, and `PAIRED_OWNER` to the HTTPS relay
   origin, the relay's client token, and your avatar UUID. The URL has no trailing
   slash. Keep this configured script private and remove it before transferring
   the object.
3. Wear the attachment. Wait for **XRT: off**, then touch it to start translation.
   Touch again to stop. Only owner touches are accepted.
4. To show translated text above the attachment, enable connector captions in
   XRTranslate and set `SHOW_CAPTIONS = TRUE` in the script. World attachment text
   is visible to nearby residents; a HUD is the appropriate placement for a
   personal display.

Connection reconciliation always clears the requested switch. Wearing the
attachment, crossing a region or reconnecting therefore requires a new touch to
start translation. **starting**, **active**, **stopping** and **error** reflect
the host's actual state; the touch request itself does not report success.

## Remove

Turn translation off, detach the object and stop the local bridge. Before
removing the script from a retained prim, clear its hover text with
`llSetText("", ZERO_VECTOR, 0.0);`. Delete the dedicated prim or restore the
original object. Remove the relay's tokens when retiring the pairing.

## In-world acceptance

With XRTranslate configured for a working input and translation provider:

- Wear the object, verify Off, touch once and verify Starting then Active only
  when XRTranslate is ready. Touch again and verify the owned session stops.
- Confirm another resident's touch cannot start a session.
- Detach, teleport and restart the bridge during capture. Each path must stop the
  old operation and return to Off after reconnection.
- Interrupt the relay connection and confirm the host retires its session after
  the heartbeat deadline. Restoring connectivity must not resume translation.
- With captions enabled, verify Unicode text, expiration, stop clearing and the
  intended visibility of your attachment or HUD.

These steps require the Second Life viewer and simulator. The repository does
not record them as passed until that environment has been exercised.
