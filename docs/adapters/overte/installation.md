# Overte

Add a translation control to the tablet and show captions below eye level.

## Install

1. Download and extract [the Overte script bundle](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.2/XRT-Avatar-Plugin-Overte-0.1.0-preview.2.zip).
   Start an [XRTranslate development build](https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md) with Avatar controls (planned for XRTranslate 0.3.4).
   In Avatar translation controls, select **Overte**, enable the
   plugin, and copy its connection address and pairing token. Enable **Send
   captions to the game component** to display translated text.
2. In Overte, open **Edit → Running Scripts → From Disk** and select
   `adapters/overte/scripts/xrt-avatar.js` from the extracted bundle. Keep its directory structure intact.
3. Open the tablet and select the **link icon**. Paste the address and pairing
   token from XRTranslate. Connecting does not start translation.
4. Select the **play icon** to start. The control becomes a **square**; select it
   to stop, including while translation is starting.

Hover over the icon to see its action. The square is highlighted once translation
is running. An hourglass means the control is waiting; repeated clicks are ignored.
After an error, the arrow offers **Retry translation**. After a lost connection,
use the link icon to connect again.

To bind the script to an Avatar, host `adapters/overte/scripts/`,
`connectors/websocket/javascript/` and `presentation/javascript/` with the repository's relative directory
layout, then add `script = <xrt-avatar.js URL>` to its FST. The script loads the
shared connector module with `Script.require`. Use one installation method at a
time. Avatar scripts stop when their Avatar is removed. Interface scripts
also disconnect when the Avatar or domain session changes; pair again to use the
new session.

Run XRTranslate on the same computer. Pairing tokens are not saved in the script
or Avatar; copy a fresh token from XRTranslate when needed.

## Captions and removal

Captions appear in a dark, translucent bubble below eye level. The bubble grows
to fit up to three lines and fades away within eight seconds. Long text ends with
an ellipsis. New text replaces the current caption without stacking bubbles.

Only you can see these captions. Stopping translation or changing Avatar removes
them immediately. There is no empty panel while translation is idle.

Stop the script in **Running Scripts**, or remove its FST entry. The adapter
closes its connection, removes its tablet button and deletes its caption entity.
Translation started from this control stops when the script closes.

## Compatibility

The implementation follows the current official JavaScript APIs listed below.
Game-client installation and VR interaction still require an Overte acceptance
run, including different Avatar heights, multilingual font coverage, caption
size and controller hover states.

- [Avatar script lifecycle and FST installation](https://docs.overte.org/en/latest/script/avatar-scripts.html)
- [Interface script installation](https://docs.overte.org/en/latest/script/interface-scripts.html)
- [WebSocket API](https://apidocs.overte.org/WebSocket.html)
- [Native WebSocket implementation](https://github.com/overte-org/overte/blob/master/libraries/script-engine/src/WebSocketClass.cpp)
- [Tablet API](https://apidocs.overte.org/TabletProxy.html)
- [Tablet button icons and states](https://apidocs.overte.org/TabletButtonProxy.html)
- [Text entity API](https://apidocs.overte.org/Entities.html#EntityProperties-Text)
- [Native text measurement](https://apidocs.overte.org/Entities.html#.textSize)
