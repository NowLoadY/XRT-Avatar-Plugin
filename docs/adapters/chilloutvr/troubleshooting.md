# ChilloutVR setup help

Return to the [installation guide](installation.md) after completing these steps.

## Animator setup

If XRT asks for an animator, select the avatar's **CVR Avatar** component. For a
generated Advanced Avatar Settings setup, use **Create Animator** and then
**Attach created Override to Avatar**. Keep an avatar author's custom controller
unless their instructions call for regeneration. See the
[CCK avatar settings](https://docs.chilloutvr.net/cck/components/cvr-avatar/#advanced-avatar-setting).

Remove the XRT caption and translation control before regenerating an installed
avatar's animator. Attach the new controller, save the scene and add XRT again.

## Caption is blank

- Enable **Show captions** on XRTranslate's **Game Plugins** page. Select **Messages**
  in the preview to check its content and audio-source settings. For menu-controlled
  translation, turn on **XRT Translation** in the game's Advanced Avatar Settings.
- Confirm you uploaded and are wearing the avatar with the caption object.
- Allow the avatar's scripts and custom shaders in the viewer's content settings.
- In Unity, use the magnifier in Caption Objects to find the object and check its
  position and scale. The window's sample text is a preview, not a saved caption.

A stopped translation task removes its messages; the header and footer stay
visible. If the connection disappears, the object clears after 20 seconds without
updates. A square replacement glyph means the bundled font does
not contain that character; the shared [font manifest](../../../presentation/unity/Runtime/Caption/XrtCaptionFont.json)
lists its coverage.

## Missing setup assets

Controller and override edits do not require reinstalling XRT. If a generated
installation record or asset was deleted, restore its XRT folder from the project
backup. Keep that folder while saved scenes or Undo still reference it.

If a text object was deleted directly from the Hierarchy, use **−** in Caption
Objects to remove its remaining bus, or **＋** to create a new text child. Removing
the editor package alone does not remove objects or controls from the avatar.

## Connection or upload problems

- **XRT is missing in Unity:** finish importing the package and resolve Console
  errors. Check the [target CCK version](compatibility.md).
- **Not enough synced bits:** free 96 bits in the avatar's controller. Local
  parameters beginning with `#` do not consume the synchronized capacity.
- **Connection unavailable:** enable OSC, select ChilloutVR on XRTranslate's
  **Game Plugins** page, and turn on **Enable**. Restore the default OSCQuery service prefix if
  you changed it in launch settings.
- **Audio source busy:** stop the other XRTranslate task using that input.
- **Cannot sign in to the CCK:** use the CCK Master Key from
  [Community Hub → Profile settings → Access Key Management](https://docs.chilloutvr.net/cck/setup/#signing-in-to-the-cck).
- **Upload blocked:** resolve the errors in the CCK Builder tab. See the
  [official validation guide](https://docs.chilloutvr.net/cck/validations/).
