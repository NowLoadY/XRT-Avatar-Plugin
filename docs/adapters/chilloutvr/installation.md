# Install for ChilloutVR

Add a caption object to your avatar and control translation from Advanced Avatar
Settings. Use ChilloutVR and XRTranslate on the same PC.

## Prepare the project

Open your avatar's saved scene in a **CCK 4** Unity project. Follow the official
[Unity and CCK setup](https://docs.chilloutvr.net/cck/setup/) and your avatar
creator's import instructions. Finish its normal animation setup first; see
[animator setup](troubleshooting.md#animator-setup) if needed.

## Add captions

1. Download [XRT-Avatar-Plugin for ChilloutVR](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0/XRT-Avatar-Plugin-ChilloutVR-0.1.0.unitypackage).
2. In Unity, choose **Assets → Import Package → Custom Package** and import it.
3. Open **Tools → XRT-Avatar-Plugin → ChilloutVR → Caption Objects**. Select your
   avatar in the Hierarchy or drag it into **Avatar**.
4. Use **Attach to** to choose a hand, chest or another object within the avatar,
   then press **＋**. The default **Board** is a 3:2 panel, 0.32 m wide.
5. Select the text with the magnifier icon. Move, rotate and resize it in the
   Scene view. Horizontal and Portrait are transparent; Board has a background and
   border. Resize X/Y to wrap text; use **Appearance** for size, colors, outline and
   border. **Layout** adjusts line spacing, padding and rounded corners.
   Use **＋** again to place the same text on another prop; select an object in
   the list to inspect or remove it.
6. Save the scene and upload the avatar through the CCK **Builder** panel. Wear
   that uploaded version in ChilloutVR.

The preview text stays in the preview window. Adding captions also adds
**XRT Translation** to Advanced Avatar Settings. For the translation control
alone, use **Tools → XRT-Avatar-Plugin → ChilloutVR → Avatar Controls**.

## Start translation

1. Configure your audio input and languages in XRTranslate.
2. Open **Game Plugins** in the sidebar, select **ChilloutVR**, and turn on
   **Enable**. The default display shows your translations.
3. In ChilloutVR, enable **Settings → Implementation → OSC**. Wait for XRTranslate
   to show **Ready**, then turn on **XRT Translation** in **Advanced Avatar Settings**.

Completed translations appear on your caption object. Turning **XRT Translation**
off stops that task and removes its messages; the header and footer keep displaying
their selected content. The trash icon clears messages, and **Show captions**
turns the entire display on or off. After changing avatars or reconnecting, wait
for **Ready** and turn translation on again. Chatbox output is configured separately
in XRTranslate's OSC plugin.

The avatar uses ChilloutVR's official Lua component and a custom material. Your
avatar's scripts and shaders must be allowed by the viewer's content settings.
See [ChilloutVR OSC](https://docs.chilloutvr.net/chilloutvr/game/osc/) and
[Avatar components](https://docs.chilloutvr.net/cck/avatar/avatar-component-whitelist/).

## Adjust the display

In XRTranslate, select **Header**, **Messages** or **Footer** in the preview to
change content, audio sources or formatting. In Unity, use the caption material
for colors and the Scene view for placement. These changes are optional.

## Remove or update

Select a text object and use **−** in Caption Objects to remove it. After removing
all text objects, use **−** in Avatar Controls to remove the translation control
if desired. Save and upload again.
Unity's **Edit → Undo / Redo** reverses either operation.

Import a newer package to update XRT. Keep the generated XRT folders in your
project. Your controller, animation overrides and other avatar settings remain
editable. Removing XRT keeps those edits. Remove its objects and control before
replacing the avatar's animator, then add them again after attaching the new one.

[Setup help](troubleshooting.md) · [Compatibility](compatibility.md)
