# Install in Overte

Use the tablet to control translation and display captions on objects you can
place and style yourself.

## Install and connect

1. In the [release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases), copy
   the **Overte script URL**. In Overte, open **Edit → Running Scripts → From URL**,
   paste it and load the script. Keep the URL's version unchanged until you update.
2. In XRTranslate, select **Game Plugins → Overte**, turn on **Enable**, and click
   **Copy connection info**.
3. On Overte's tablet, select the **link icon**, paste once and connect.
   Translation starts **Off**; select play when you are ready.

This uses Overte's [native script loader](https://docs.overte.org/en/latest/script/interface-scripts.html).
For offline installation, extract the Overte ZIP and use **From Disk** to load
`adapters/overte/scripts/xrt-avatar.js`. Keep the extracted files together.


## Start and stop

Select the **play icon** to start translation. It becomes a **square** that stops
translation or cancels startup. The square is highlighted while translation is
running.

Hover over an icon to see its action. An hourglass means the control is waiting.
The arrow retries after an error; the link reconnects after a lost connection.
After changing Avatar or visiting another domain, select reconnect and then play.
The running script reuses its connection information when you reconnect. If
XRTranslate restarted or reconnection fails, copy fresh connection info and try again.

## Add captions

Open **Caption objects** using the tablet's display icon. Choose **Wear with
Avatar** for a caption others can see, or **Only me** for a personal object. Select
a card to create and connect it:

![Caption objects authoring page — HTML preview](images/caption-page.jpg)

| Object | Appearance |
| --- | --- |
| Horizontal | A wide, light caption panel. |
| Portrait | A narrow panel with horizontal lines of text. |
| Board | Transparent text to place on your own board or drawing surface. |

The object appears in front of your Avatar. In **Create → Entity List**, choose
it by name; enable **Local entities** to find personal objects. Use Create to
move, rotate and resize it, then edit its font, colors, margins and background.
Avatar objects move with you; personal objects stay where you place them. Work in
a location where Create is available.

To use an existing object, choose it from the **Text objects** list and select the
**link icon**. The list shows editable Text objects within 30 meters and whether
others can see them; a checkmark marks connected objects. Select **refresh** after
adding or moving an object. Connect several objects to arrange your display.

Select an object in the list, then choose **All text**, **Header**, **Messages** or
**Footer** for it. Each object keeps its own source and visibility settings.

Use the **eye icon** to keep sample text visible while editing. Select it again
to end the preview; starting translation also ends it. **Hide when empty** hides
the object between captions. These controls apply to the selected object. The
**broken-link icon** disconnects it and restores its previous text and visibility
while keeping your design and the other connections.

The audience selector applies to new objects. Binding an existing object keeps
its placement and audience. Avatar objects use the domain's Avatar-object
permission. Export from Create to keep a reusable design; personal objects last
for the current Interface session.

## Appearance

Live text preserves the object's position, size, font and colors. It wraps to
fit, with an ellipsis when it is too long. Messages disappear after their display
time in XRTranslate. Stopping translation removes its messages; a configured
header or footer remains until its source is cleared or the connection closes.

New objects use the included Noto caption font, loaded over HTTPS, for common
Chinese, Japanese, Korean, Latin, Greek and Cyrillic text. For a handwritten or
illustrated sign, place Board text on your artwork and choose a compatible font
in Create.

## Remove and restore

Stop the script in **Running Scripts**. Its tablet controls disappear, translation
stops and the bound objects' previous text is restored. Objects you created or
edited remain available.

To use it again, load `xrt-avatar.js` and reconnect. Saved bindings reconnect as
their objects load. An unavailable object stays in the list as **Waiting for
object**; select it and use the broken-link icon to remove that connection.

For Avatar script installation or custom displays, see the
[development guide](https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/overte/development.md).
