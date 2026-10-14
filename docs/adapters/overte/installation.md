# Overte

Control translation from the tablet and place captions on a Text object of your
choice.

## Install

1. Download and extract [the Overte script bundle](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.3/XRT-Avatar-Plugin-Overte-0.1.0-preview.3.zip).
   Start an [XRTranslate development build](https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md) with Avatar controls (planned for XRTranslate 0.3.4).
   In Avatar translation controls, select **Overte**, enable the plugin, and copy
   its connection address and pairing token. Enable **Send captions to the game
   component**.
2. In Overte, open **Edit → Running Scripts → From Disk** and select
   `adapters/overte/scripts/xrt-avatar.js` from the extracted bundle. Keep its
   directory structure intact.
3. Open the tablet and select the **link icon**. Paste the address and pairing
   token from XRTranslate. Connecting does not start translation.
4. Select the **play icon** to start. The control becomes a **square**; select it
   to stop, including while translation is starting.

Hover over an icon to see its action. The square is highlighted while translation
is running. An hourglass means the control is waiting. The arrow retries after an
error; the link reconnects after a lost connection.

Run XRTranslate on the same computer. Pairing tokens are not saved in the script
or Avatar; copy a fresh token from XRTranslate when needed.

## Choose a caption object

Open **Caption objects** using the tablet's display icon. Select one of the three
cards to create and bind a Text object:

| Object | Appearance |
| --- | --- |
| Horizontal | A wide, light caption panel. |
| Portrait | A narrow panel with ordinary horizontal lines. |
| Board | Transparent text to place on a board or drawing surface. |

The object starts in front of you. Open **Create → Entity List**, enable its
**Local entities** filter and choose the object by name. Move, rotate and resize
it with the normal Create tools. Adjust its font, colors, margins and background
in Properties. You can attach it to a prop or Avatar using the normal parenting
controls. Work in a location where Create is available.

To use your own Text object, select it from the **Text object** list and press the
**link icon**. The list includes editable objects within 30 meters. Its label
shows whether the object is visible only to you or to others. Press **refresh**
after adding or moving an object.

Use the **eye icon** to hold a sample caption while editing. Select it again to
stop the preview; starting translation also ends the sample preview. **Hide when empty** controls whether
the whole object disappears between captions. The **broken-link icon** unbinds the
object and restores the text and visibility it had before binding. The object and
its design remain in your scene.

New presets are local and visible only to you. Binding an existing shared object
makes captions visible wherever that object is visible. Export objects from Create
to keep a reusable copy. Local objects last for the current Interface session.

## Text and placement

Translation updates the bound object's text. Position, rotation, size, font and
colors stay as you set them. Long text wraps to the available space and ends in an
ellipsis when it cannot fit. Custom renderers can read the current plain text
without display wrapping. Captions clear within eight seconds and when translation stops.

The Board preset adds transparent text; use your own board artwork and font to
make a handwritten or illustrated sign. No preset follows your head or changes
its position when a new caption arrives.

## Custom rendering

The script publishes the current plain-text caption through Overte's local Messages API
on `XRT/CaptionSource`. It is independent of the selected Text object and is not
saved in entity metadata or settings.

```json
{"type":"caption","source":"XRTranslate","value":{"text":"Translated text","stream":"42","id":"turn-1","revision":1,"expiresAt":1790000000000}}
{"type":"clear","source":"XRTranslate"}
```

`expiresAt` is a Unix timestamp in milliseconds. Consumers clear at that deadline
and on `clear`, and accept only local messages. Use
[`caption-source.js`](../../../adapters/overte/examples/caption-source.js) as a
small starting point for a custom renderer. Apply visual wrapping in the renderer
instead of changing the shared source.

## Avatar installation and removal

To bind the script to an Avatar, host `adapters/overte/scripts/`,
`connectors/websocket/javascript/` and `presentation/` with the repository's
relative directory layout, then add `script = <xrt-avatar.js URL>` to its FST.
Use one installation method at a time. Avatar scripts stop when their Avatar is
removed. Interface scripts disconnect when the Avatar or domain session changes;
pair again to use the new session.

Stop the script in **Running Scripts**, or remove its FST entry. The adapter stops
translation, removes its tablet controls and restores the bound object's text.
Objects you created or edited remain available. The last binding is reused if the
same object still exists when the script restarts.

## Compatibility

Game-client and VR acceptance still require a real Overte run, including Create's
local-object editing, multilingual font coverage, native text measurements,
parented objects and controller hover states.

- [Avatar script lifecycle and FST installation](https://docs.overte.org/en/latest/script/avatar-scripts.html)
- [Interface script installation](https://docs.overte.org/en/latest/script/interface-scripts.html)
- [WebSocket API](https://apidocs.overte.org/WebSocket.html)
- [Tablet API](https://apidocs.overte.org/TabletProxy.html)
- [Tablet button icons and states](https://apidocs.overte.org/TabletButtonProxy.html)
- [Text entities and editable properties](https://apidocs.overte.org/Entities.html#EntityProperties-Text)
- [Native text measurement](https://apidocs.overte.org/Entities.html#.textSize)
- [Local Messages API](https://apidocs.overte.org/Messages.html)
- [Create entity list and local-object filter](https://github.com/overte-org/overte/blob/master/scripts/system/create/entityList/entityList.js)
