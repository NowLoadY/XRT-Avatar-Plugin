# VRChat setup

Add captions and **XRT → Translation** to an avatar you can edit and upload.
Run VRChat and XRTranslate on the same PC.

## Install XRT

### VCC or ALCOM

1. In VCC, open **Settings → Packages → Add Repository**; in ALCOM, open
   **Packages → Add Repository**. Paste:

   ```text
   https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/main/vpm/index.json
   ```

2. Open **Manage Project** and add
   **XRT-Avatar-Plugin (VRChat)**. In ALCOM, click **Apply**.
3. Open the project in Unity and load your avatar's saved scene. Keep Play mode off.

For a new project, follow [VCC setup](https://vcc.docs.vrchat.com/guides/getting-started/)
and your avatar creator's instructions. Uploading requires **New User** rank or
higher and a VRChat account; see the [official requirements](https://creators.vrchat.com/avatars/creating-your-first-avatar/#requirements).

For a manual install, import the VRChat `.unitypackage` from the
[release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases) using
**Assets → Import Package → Custom Package**. Use one installation method per project.

## Add captions

1. Open **Tools → XRT-Avatar-Plugin → Caption Objects** and select your avatar.
2. Choose **Attach to**, then click **+**. The default **Board** is a 3:2 panel,
   0.32 m wide, with a background and border. **Horizontal** and **Portrait** are transparent alternatives.
3. Move and rotate in the Scene view. Resize X/Y to change the text area.
   Board automatically fits and balances the text; **Max text size** limits its size.
   Use **Appearance** for text size, colors, outline and border width; **Layout**
   adjusts natural layout, line spacing, padding and rounded corners.
4. Save, then upload for PC through **VRChat SDK → Show Control Panel → Builder**.
   Wear the uploaded avatar. [First upload](https://creators.vrchat.com/avatars/creating-your-first-avatar/).

For a handheld board, select the caption and enable **Hold in left hand**.
It keeps your placement and size; move, rotate or resize the caption as usual.
The right hand aligns the board when close. Turn the option off to remove the
effect and keep the board. A humanoid avatar with both hands is required.

The **XRT → Caption panel** switch shows or hides all in-game caption panels.
It starts off and remembers your choice. **Translation** separately starts or stops translation.

<a id="3-add-the-menu"></a>
For controls alone, open **Tools → XRT-Avatar-Plugin → Avatar Controls** and
click **+** beside **Translation menu**, then save and upload. Existing controls
are kept; a full root menu moves into an **Avatar** submenu beside **XRT**.

## Start translation

To test before uploading, open **Tools → XRT-Avatar-Plugin → Local Test**, select
the avatar and click **Start test**. This uses Gesture Manager in Play mode.
Enable **Game Plugins → VRChat** in XRTranslate, then use **XRT → Translation**
in Gesture Manager's menu. Watch the status and scene captions. **Stop test**
closes the connection and exits Play mode if the test started it.

Gesture Manager is optional and can be installed through ALCOM. Keep VRChat's
OSC off during this test so XRTranslate sees one endpoint. This checks local
communication and rendering; upload is still needed to test remote players.

1. In XRTranslate, set your microphone and languages. Open **Game Plugins**,
   choose **VRChat**, and turn on **Enable**.
2. In VRChat's Action Menu, turn on **OSC → Enabled**.
3. When XRTranslate shows **Ready**, turn on **Expressions → XRT → Translation**.
   **Translating** means it is running. Turn on **XRT → Caption panel** to show the results.

Use the same switch to stop. After changing avatars or reconnecting, wait for
**Ready** and start again. Other players see captions when your PC avatar and
shaders are visible. [OSC help](https://docs.vrchat.com/docs/osc-overview#enabling-it).

## Adjust captions

In XRTranslate, click **Header**, **Messages** or **Footer** in the preview to
edit it. **Show captions** shows or hides the display; the trash icon clears
messages. Stopping clears that task's messages and keeps independent header/footer content.

In Unity, **+** adds another object; **Text object** selects one and the magnifier
locates it. Objects share text and layout, with their own placement and material.
The installer fits the transfer size to available parameters; smaller transfers
fill the same 128-character display more slowly. [Caption details](presentation.md).

## Update or remove

Update through your original install method. Keep the generated XRT folders;
existing objects and placement are retained.
Existing caption setups gain the panel switch on the next upload. To update it
before testing in Unity, click **+** beside **Avatar Controls → Translation menu**.

In **Caption Objects**, select an object and click **−**. After removing all
caption objects, use **Avatar Controls → Translation menu → −** to remove the
control. Save and upload again. Remove XRT from each avatar before uninstalling
the package. **Edit → Undo / Redo** reverses these operations; your added children,
components and replacement meshes/materials are kept.

[Setup help](troubleshooting.md) · [Compatibility](compatibility.md)
