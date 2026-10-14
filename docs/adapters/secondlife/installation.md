# Install the Second Life attachment

Use an editable attachment or HUD owned by your avatar. Keep a copy of the
original object before adding the adapter. Each hover display needs a dedicated
prim so it does not replace text maintained by another script.

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
   script in the attachment and compile it with Mono.
2. Set `RELAY_URL`, `RELAY_CLIENT_TOKEN`, and `PAIRED_OWNER` to the HTTPS relay
   origin, the relay's client token, and your avatar UUID. The URL has no trailing
   slash. Keep this configured script private and remove it before transferring
   the object.
3. Wear the attachment and touch it to open its controls. When the dialog shows
   **Off**, choose **Start**. Touch it again and choose **Stop** to stop translation.
   **Close** dismisses the controls. Only the owner can use them.

Connection reconciliation always clears the requested switch. Wearing the
attachment, crossing a region or reconnecting therefore requires choosing **Start** again.
The dialog shows **Starting…**, **On**, **Stopping…**, or **Check XRTranslate**.
Open it again to see the current state. Connection messages stay in the controls;
only translated text appears above the attachment.

## 4. Add captions

Enable connector captions in XRTranslate, then choose a display:

| Display | Setup |
| --- | --- |
| Above the control prim | Set `SHOW_CAPTIONS = TRUE` in `XRT.lsl`. Use a dedicated prim. |
| Above your own display prim | Set `PUBLISH_TEXT_SOURCE = TRUE` and keep `SHOW_CAPTIONS = FALSE`. Add [`XRT-TextDisplay.lsl`](../../../adapters/secondlife/attachment/XRT-TextDisplay.lsl) to a dedicated prim in the same linkset. Compile with Mono and position the prim in Build. |

Text sharing is off by default. Enabling `PUBLISH_TEXT_SOURCE` makes translated
text available to scripts throughout that linkset. Add the receiver before
starting translation. Captions stay empty between results and clear when stopped
or expired. World attachment text is visible to nearby residents; choose a HUD
for a personal display.

The default caption layout is horizontal. To choose a narrower vertical layout,
set this before starting the bridge:

```sh
export XRT_CAPTION_PRESET=vertical
```

The available values are `horizontal`, `vertical` and `board`. These change the
hover text's wrapping; its font and background remain native to the viewer.
The board layout does not place handwriting on an object's surface. To connect
your own lettering renderer, see the [text source](README.md#text-source).

## Remove

Turn translation off, detach the object and stop the local bridge. Before
removing either display script from a retained prim, clear its hover text with
`llSetText("", ZERO_VECTOR, 0.0);`. Delete the dedicated prim or restore the
original object. Remove the relay's tokens when retiring the pairing.

## In-world acceptance

With XRTranslate configured for a working input and translation provider:

- Wear the object, open the controls and verify Off. Choose Start, reopen the
  controls and verify Starting then On when XRTranslate is ready. Choose Stop
  and verify translation stops.
- Confirm another resident's touch cannot open the controls or start translation.
- Open the dialog, reconnect, then press its old Start button. Translation must
  remain Off until Start is chosen from a fresh dialog.
- Detach, teleport and restart the bridge during capture. Each path must stop the
  old operation and return to Off after reconnection.
- Interrupt the relay connection and confirm the host retires its session after
  the heartbeat deadline. Restoring connectivity must not resume translation.
- With captions enabled, verify Unicode wrapping, expiration, stop clearing and
  the intended visibility of your attachment or HUD. Repeated or replaced results
  must not reappear after they expire.
- With text sharing enabled and `SHOW_CAPTIONS` disabled, verify the receiver
  shows new results while existing hover text on the control prim stays intact.
  Stop translation and reset the controller while a caption is visible; the
  receiver must clear in both cases. Confirm captions containing `true`, quotes,
  backslashes and line breaks remain text.

These steps require the Second Life viewer and simulator. The repository does
not record them as passed until that environment has been exercised.
