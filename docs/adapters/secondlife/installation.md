# Set up Second Life

Add translation controls to an attachment or HUD you own, then choose where its
captions appear. This setup uses the viewer's script editor and a local Node.js
bridge.

## Before you start

Prepare these items first:

- XRTranslate with your audio input and translation provider configured.
- An editable attachment or HUD, and permission to run scripts where you use it.
- Node.js 22.4 or later on the computer running XRTranslate.
- A working HTTPS relay address and its two separate **client** and **host** tokens.
  If you run the relay yourself, complete [relay setup](relay-setup.md) first.
- Your avatar's UUID, used to pair the attachment with you.

Download **Source code (zip)** from the
[plugin release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases) and extract
it. Keep the `adapters` and `presentation` folders together. The commands below
run from the extracted folder.

## 1. Add the attachment script

1. Make a copy of your attachment. Right-click the copy, choose **Edit**, open
   **Contents**, and choose **New Script**. Replace the default script with the
   contents of `adapters/secondlife/attachment/XRT.lsl`.
2. Fill in the three values at the top:

   | Setting | Value |
   |---|---|
   | `RELAY_URL` | Your relay's HTTPS address, without a trailing slash |
   | `RELAY_CLIENT_TOKEN` | The relay's client token |
   | `PAIRED_OWNER` | Your avatar UUID, in quotes |

3. Save and compile with **Mono** selected. Keep this configured script private
   and remove it before transferring the object.

If you need your UUID, save this temporary script in an object you own and copy
the value it prints to you; remove the temporary script afterward:

```lsl
default { state_entry() { llOwnerSay((string)llGetOwner()); } }
```

The official [script editor walkthrough](https://wiki.secondlife.com/wiki/Help:Getting_started_with_LSL)
explains adding and saving scripts. [`llGetOwner`](https://wiki.secondlife.com/wiki/LlGetOwner)
returns the object's owner UUID.

## 2. Connect XRTranslate

Open **Game Plugins** in XRTranslate's sidebar, select **Second Life**, and
turn on **Enable**. Copy its connector address and pairing token. In a terminal, replace the example
values below and start the bridge.

**Windows PowerShell**

```powershell
$env:XRT_RELAY_URL = 'https://relay.example.net'
$env:XRT_RELAY_HOST_TOKEN = 'your-host-token'
$env:XRT_CONNECTOR_URL = 'ws://127.0.0.1:PORT/xrt'
$env:XRT_CONNECTOR_TOKEN = 'the-token-shown-by-XRTranslate'
node adapters/secondlife/relay/host_bridge.mjs
```

**Linux**

```sh
export XRT_RELAY_URL='https://relay.example.net'
export XRT_RELAY_HOST_TOKEN='your-host-token'
export XRT_CONNECTOR_URL='ws://127.0.0.1:PORT/xrt'
export XRT_CONNECTOR_TOKEN='the-token-shown-by-XRTranslate'
node adapters/secondlife/relay/host_bridge.mjs
```

Leave the terminal open. Update the connector address and pairing token after
restarting XRTranslate.

Wear the attachment and touch it. When the dialog shows **Off**, choose **Start**.
Touch it again and choose **Stop** to stop translation. **Close** dismisses the
dialog. Only you can use these controls. After teleporting, reattaching or
reconnecting, open a fresh dialog and choose **Start** again.

## 3. Choose a caption display

The included displays use native text floating above a prim. Letters drawn on a
board surface use a custom renderer connected to the dynamic text source.

Enable **Display content** on XRTranslate's **Game Plugins** page. Select the header,
messages or footer in its preview to choose the content. Then choose a display:

| Display | Setup |
|---|---|
| Above the control prim | Set `SHOW_CAPTIONS = TRUE` in `XRT.lsl`. Use a prim whose hover text is free. |
| Above another prim in the same attachment | Set `PUBLISH_TEXT_SOURCE = TRUE` and leave `SHOW_CAPTIONS = FALSE`. Add `adapters/secondlife/attachment/XRT-TextDisplay.lsl` to that display prim and compile with Mono. |

For the second option, keep both prims in the same linkset. Move the display prim
in Build to place the text. In `XRT-TextDisplay.lsl`, choose what it shows:

| `REGION` | Content |
|---|---|
| `"full"` | Header, messages and footer together |
| `"header"` | Header only |
| `"messages"` | Messages only |
| `"footer"` | Footer only |

Use separate display prims to arrange the regions independently. Text sharing
supplies the same regions to custom scripts in the linkset. World attachment
text is visible to nearby residents; use a HUD for a personal display. Font and
hover-text appearance follow the viewer. Custom lettering can use the
[dynamic text source](README.md#text-source).

Messages disappear when their lifetime ends. **Stop** ends the translation task;
configured lyrics, time and other banner content remain visible. Disconnecting
or disabling **Display content** clears the display.

## Stop or remove

Choose **Stop**, detach the object and press Ctrl+C in the bridge terminal.
When removing the installation, delete the dedicated display prim or restore the
original object. If retaining a prim, clear its hover text before deleting the
script with `llSetText("", ZERO_VECTOR, 0.0);` in a temporary script's `state_entry`.
Revoke the relay tokens when retiring the pairing.

For custom integrations and in-world checks, see the [developer guide](development.md).
