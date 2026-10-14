# Overte

The adapter uses an Avatar or Interface script, an XRT tablet button and a local
caption panel. XRTranslate provides capture, translation and session ownership.

## Install

1. In XRTranslate Avatar translation controls, select **Overte**, enable the
   plugin, and copy its connection address and pairing token. Enable **Send
   captions to the game component** to display translated text.
2. In Overte, open **Edit → Running Scripts → From Disk** and select
   `adapters/overte/scripts/xrt-avatar.js` from this repository.
3. Open the tablet and press **XRT Pair**. Enter the XRTranslate address and
   token. The token remains in script memory and is cleared when the script ends.
4. Press **XRT Off** to start translation. **XRT Starting** indicates startup;
   **XRT Active** indicates that the host capture and backend are ready. Press
   again to stop, including during startup.

To bind the script to an Avatar, host `adapters/overte/scripts/` and
`connectors/websocket/javascript/` with the repository's relative directory
layout, then add `script = <xrt-avatar.js URL>` to its FST. The script loads the
shared connector module with `Script.require`. Use one installation method at a
time. Avatar scripts stop when their Avatar is removed. Interface scripts
also disconnect when the Avatar or domain session changes; pair again to use the
new session.

The pairing token belongs to the local XRTranslate installation. Keep it out of
the script source and Avatar files. The address must use a loopback WebSocket
URL on the same computer.

## Captions and removal

Captions appear in a local text entity in front of the wearer. They are visible
only in that user's Interface, expire after a short interval and clear when the
session stops. An entity lifetime also removes the panel if its script stops
unexpectedly. The script does not publish captions to the domain.

Stop the script in **Running Scripts**, or remove its FST entry. The adapter
closes its connection, removes its tablet button and deletes its caption entity.
XRTranslate cancels only the session owned by that connection. Pairing never
starts translation automatically.

## Compatibility

The implementation follows the current official JavaScript APIs listed below.
Game-client installation and VR interaction still require an Overte acceptance
run; source and simulated protocol checks do not establish game compatibility.

- [Avatar script lifecycle and FST installation](https://docs.overte.org/en/latest/script/avatar-scripts.html)
- [Interface script installation](https://docs.overte.org/en/latest/script/interface-scripts.html)
- [WebSocket API](https://apidocs.overte.org/WebSocket.html)
- [Native WebSocket implementation](https://github.com/overte-org/overte/blob/master/libraries/script-engine/src/WebSocketClass.cpp)
- [Tablet API](https://apidocs.overte.org/TabletProxy.html)
- [Text entity API](https://apidocs.overte.org/Entities.html#EntityProperties-Text)
