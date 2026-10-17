# Set up Resonite

Use an existing native text object for captions. **ResoniteLink requires a session
that you host, Builder permission, and a world that allows it.** It is not a
visitor-mode connection for arbitrary public sessions.
[Official requirements](https://wiki.resonite.com/ResoniteLink).

## Connect a text object

1. Download **XRT-Resonite-win-x64.exe** or **XRT-Resonite-linux-x64** from the
   [release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases) and open it.
   Python is included only in this Resonite executable; no Python installation is needed.
2. Create **Text → Basic** under your avatar using Resonite's Inspector, or use
   a text object you already own.
3. In Resonite's Dash, open **Session**, enable **ResoniteLink**, and paste its
   address into the connector. Click **Open session**, expand the object's
   parents, and select **Text** under it.
4. In XRTranslate, select **Game Plugins → Resonite**, turn on **Enable**, and
   click **Copy connection info**. Paste it into the connector and click
   **Connect**.
5. When the connector shows **Off**, click **Start translation**. Use
   **Stop translation** to stop. Keep the connector open while playing.

Connecting creates an **XRT** child under that text object if it does not already
exist. The connector writes only its text and disables rich-text parsing while
connected. Position, font, material, color and size stay under your control in
Resonite. Browsing the tree does not modify the scene.

On Linux, allow the downloaded file to run as a program in its file properties.
If a standalone download is not available for your system, use the
[command-line setup](command-line.md).

## Use controls in Resonite

The text object's **XRT/Enabled → Value** starts and stops translation, with actual
state in **XRT/Status**. Existing native avatar buttons can target that Value field.
For custom buttons and caption presets, see [command-line setup](command-line.md#2-add-a-caption).

## Adjust or disconnect

Use Resonite's native editor to place and style the text. In XRTranslate, select
**Header**, **Messages** or **Footer** to customize the content. These steps are optional.

Click **Disconnect** or close the connector. It clears its source fields and
restores the text and rich-text setting if they still match its last writes.
User edits are preserved. A forced process exit or unavailable ResoniteLink
connection may require clearing the text manually in Resonite.

The open window keeps connection info in memory for retries. Paste fresh info
after restarting XRTranslate; closing the window discards it.

After loading another avatar/session, open the session again and select the text;
scene IDs are not saved. Reconnection leaves translation **Off**. Save your avatar
with the connector disconnected. To remove XRT, delete its child slot and remove
any custom button references; keep your text object.

[Advanced bindings and compatibility](development.md)
