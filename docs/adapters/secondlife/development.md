# Second Life development

Use the [attachment setup](installation.md) for player installation and
[relay setup](relay-setup.md) for server deployment. The
[platform architecture](README.md) defines transport, lifecycle and linked text
messages.

## Entry points

| File | Responsibility |
|---|---|
| [`XRT.lsl`](../../../adapters/secondlife/attachment/XRT.lsl) | Owner controls, attachment lifecycle and optional text publication |
| [`XRT-TextDisplay.lsl`](../../../adapters/secondlife/attachment/XRT-TextDisplay.lsl) | Native hover text on a dedicated linked prim |
| [`host_bridge.mjs`](../../../adapters/secondlife/relay/host_bridge.mjs) | Local XRTranslate connection, display validation and relay exchange |
| [`server.py`](../../../adapters/secondlife/relay/server.py) | Authenticated, bounded in-memory exchange for one pair |

The local bridge needs Node.js 22.4 or later and the relative
`presentation/javascript/bubble.js` dependency. The relay needs Python 3.10 or
later. LSL scripts use the viewer's Mono compilation target.

## Custom displays

Enable `PUBLISH_TEXT_SOURCE` in the controller. A custom script in the same linkset
can consume the linked text messages described in
[text source](README.md#text-source). Bind `header`, `messages` or `footer` to
separate objects, or use combined `text` for custom lettering and `display_text`
for the native hover projection. The receiver owns its geometry, font and layout.
Expire each message by `ttl_ms` and clear all regions when the `expires_ms`
receiver lease ends.

Only the configured controller should supply text to a receiver. Handle empty
text, message expiry, detach and region changes. Control resets preserve display
regions; a source clear removes them. Keep native hover text on a dedicated
prim, and clear it before removing its script.

## In-world checks

With a working audio input and translation provider in XRTranslate:

- Wear the object, open its controls and verify Off. Choose Start and verify
  Starting then On. Choose Stop and verify the task's messages clear while
  configured header and footer content remain.
- Confirm another resident cannot open the controls or start translation.
- Open a dialog, reconnect, then press its old Start button. It must stay Off until
  Start is selected in a fresh dialog.
- Detach, teleport and restart the bridge during translation. Each path must
  stop the old operation and return to Off after reconnecting.
- Interrupt the relay connection. The host must retire the session after the
  heartbeat deadline; restoring connectivity must not resume translation.
- Check Unicode text, intended visibility and independent message expiry. An old
  snapshot must not restore cleared messages or reset their remaining lifetime.
- Enable text sharing with `SHOW_CAPTIONS` off. The receiver must display captions
  while existing hover text on the control prim remains unchanged. Check each
  `REGION` value, then stop translation and verify banners remain. Disconnect or
  stop the controller script and verify all receiver text clears.
- Verify that `true`, quotes, backslashes and line breaks remain text in captions.

These checks require the viewer and simulator. Source and protocol checks alone
do not establish a passed in-world compatibility result.

## Official references

- [Adding and saving LSL scripts](https://wiki.secondlife.com/wiki/Help:Getting_started_with_LSL)
- [llHTTPRequest](https://wiki.secondlife.com/wiki/LlHTTPRequest)
- [http_response](https://wiki.secondlife.com/wiki/Http_response)
- [attach](https://wiki.secondlife.com/wiki/Attach)
- [changed](https://wiki.secondlife.com/wiki/Changed)
- [llDialog](https://wiki.secondlife.com/wiki/LlDialog)
- [llMessageLinked](https://wiki.secondlife.com/wiki/LlMessageLinked)
- [llSetText](https://wiki.secondlife.com/wiki/LlSetText)
