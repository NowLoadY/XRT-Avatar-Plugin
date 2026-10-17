# Set up Second Life

Use a wearable attachment for translation controls and floating captions.
**A working HTTPS relay is required.** Ask its operator for separate **client**
and **host** connection info. Self-hosting is covered in [relay setup](relay-setup.md);
there is no hosted relay supplied by this repository.

## Add the attachment

1. Make a dedicated prim or a copy of an attachment you own. In its **Contents**,
   create a script and paste the supplied **XRT.lsl** from the
   [release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases). Save with **Mono** selected.
   You do not need to edit settings or enter an avatar UUID.
2. Wear it and touch it. Paste the relay's **client** connection
   info. Only the owner can pair or operate the attachment.
3. Choose **Show text** in the menu that opens if this prim should display captions.
   Use a dedicated prim whose hover text is free.

These are the viewer's normal [script editing steps](https://wiki.secondlife.com/wiki/Help:Getting_started_with_LSL).
Use a location where scripts are allowed.

## Connect and start

1. Open **XRT-SecondLife-win-x64.exe** or **XRT-SecondLife-linux-x64** from the release.
   Its Node.js runtime is included; Python is not included or required on the player's computer.
2. Paste the relay's **host** connection info when prompted.
3. In XRTranslate, select **Game Plugins → Second Life**, turn on **Enable**, and
   click **Copy connection info**. Paste it into the connector's second prompt.
   Input is hidden; leave the connector window open.
4. Touch the attachment. When it shows **Off**, choose **Start**. Touch it again
   and choose **Stop** to stop translation.

After teleporting or reconnecting, choose **Start** in a fresh dialog. Restarting
XRTranslate requires copying fresh connection info into the connector. On Linux,
allow the executable to run in file properties and launch it in a terminal.

## Adjust the display

World attachments show floating text to nearby residents; a HUD shows it only to
you. Move the prim with the viewer's Build tools. Native hover text uses the viewer's
font and a 254-byte text budget; it does not draw letters onto a board surface.
[Official hover-text behavior](https://wiki.secondlife.com/wiki/LlSetText).

In XRTranslate, select **Header**, **Messages** or **Footer** to change content.
Stopping translation removes its messages while independent banners remain.
**Display content** turns the entire display off. To hide this prim's text, stop
translation and choose **Hide text** in its dialog.

For another prim in the same attachment, enable `PUBLISH_TEXT_SOURCE` in the
controller, add **XRT-TextDisplay.lsl** to the display prim, and choose its `REGION`
(`full`, `header`, `messages` or `footer`). See [custom displays](development.md#custom-displays).

## Stop or remove

Choose **Stop**, then **Unpair** if retiring the attachment. Detach it and press
Ctrl+C in the connector. Pairing lives in script state, not in the source; changing
owners clears it. Recompiling/resetting an unconfigured script requires pairing again.
Keep configured copies private and revoke their relay tokens when retiring them.

Choose **Hide text** before deleting the script from a prim you keep: hover text
otherwise remains after script deletion. You may instead delete the dedicated prim.

For source execution with Node.js 22.4+, run `node adapters/secondlife/relay/host_bridge.mjs`
from a source checkout. It uses the same prompts; environment variables remain
available for advanced setups. [Developer details](development.md).
