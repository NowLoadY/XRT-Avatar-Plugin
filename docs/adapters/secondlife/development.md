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
| [`host_bridge.mjs`](../../../adapters/secondlife/relay/host_bridge.mjs) | Local XRTranslate connection, caption projection and relay exchange |
| [`server.py`](../../../adapters/secondlife/relay/server.py) | Authenticated, bounded in-memory exchange for one pair |

The local bridge needs Node.js 22.4 or later and the relative
`presentation/javascript/bubble.js` dependency. The relay needs Python 3.10 or
later. LSL scripts use the viewer's Mono compilation target.

## Custom displays

Enable `PUBLISH_TEXT_SOURCE` in the controller. A custom script in the same linkset
can consume the linked text messages described in
[text source](README.md#text-source). Use unwrapped `text` for custom lettering or
`display_text` for the native hover projection. The receiver owns its geometry,
font, layout and independent expiry.

Only the configured controller should supply text to a receiver. Handle empty
text, remaining expiry, controller reset, detach and region changes. Do not retain
a caption after its source has cleared. Keep native hover text on a dedicated
prim, and clear it before removing its script.

## In-world checks

With a working audio input and translation provider in XRTranslate:

- Wear the object, open its controls and verify Off. Choose Start and verify
  Starting then On. Choose Stop and verify translation and captions stop.
- Confirm another resident cannot open the controls or start translation.
- Open a dialog, reconnect, then press its old Start button. It must stay Off until
  Start is selected in a fresh dialog.
- Detach, teleport and restart the bridge during translation. Each path must
  stop the old operation and return to Off after reconnecting.
- Interrupt the relay connection. The host must retire the session after the
  heartbeat deadline; restoring connectivity must not resume translation.
- Check Unicode wrapping, intended visibility, expiry and stop clearing. Duplicate
  or replaced results must not restore expired captions.
- Enable text sharing with `SHOW_CAPTIONS` off. The receiver must display captions
  while existing hover text on the control prim remains unchanged. Stop or reset
  while text is visible and verify that the receiver clears.
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
