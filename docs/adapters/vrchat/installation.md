# Install for VRChat

Add a caption board and **XRT → Translation** to your avatar. Use VRChat and
XRTranslate on the same PC, with an avatar project you can edit and upload.

## Install XRT

### VCC or ALCOM

In XRTranslate, select **Game Plugins → VRChat → Add to VCC / ALCOM**. Confirm
adding the repository in your package manager. Enable **Show Pre-Release
Packages**, open **Manage Project**, and add **XRT-Avatar-Plugin (VRChat)**.
In ALCOM, click **Apply**. Open the project in Unity.

<details>
<summary>Add the repository manually, or import a Unity package</summary>

In VCC, use **Settings → Packages → Add Repository**. In ALCOM, use
**Packages → Add Repository**. Paste:

```text
https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/main/vpm/index.json
```

Alternatively, download the VRChat `.unitypackage` from the
[release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases) and import it with
**Assets → Import Package → Custom Package**. Use one installation method per project.

</details>

Starting a new avatar project? Follow [VCC setup](https://vcc.docs.vrchat.com/guides/getting-started/)
and your avatar creator's import instructions. Uploading requires **New User**
rank or higher and a VRChat account; see the
[official requirements](https://creators.vrchat.com/avatars/creating-your-first-avatar/#requirements).

## Add captions

1. Open your avatar's saved scene, with Play mode off. Open
   **Tools → XRT-Avatar-Plugin → Caption Objects** and select your avatar.
2. Choose **Attach to** and a style: **Horizontal**, **Portrait**, or transparent
   **Board** text for your own sign. Click **+**. This also adds the translation menu.
3. Move, rotate and scale the object in the Scene view, then save.
4. Use **VRChat SDK → Show Control Panel → Builder** to upload for PC, then wear
   the uploaded avatar. Follow the [official upload steps](https://creators.vrchat.com/avatars/creating-your-first-avatar/)
   if this is your first upload.

<a id="3-add-the-menu"></a>
Only need the translation switch? Open **Tools → XRT-Avatar-Plugin → Avatar
Controls**, select the avatar and click **+** beside **Translation menu**, then
save and upload. Existing controls are kept; a full root menu is placed in an
**Avatar** submenu beside **XRT**.

## Start translation

1. In XRTranslate, configure your microphone and languages, then select
   **Game Plugins → VRChat** and turn on **Enable**.
2. In VRChat, turn on **OSC → Enabled** in the Action Menu.
3. When XRTranslate shows **Off**, turn on **Expressions → XRT → Translation**.
   **Active** means translation is running; completed translations appear on the board.

Turn the same switch off to stop. After an avatar change or reconnection, wait
for **Off** and start again. Other players see the text when your PC avatar and
its shaders are shown. [Official OSC instructions](https://docs.vrchat.com/docs/osc-overview#enabling-it).

## Adjust the display

The default display shows translations. In XRTranslate, select **Header**,
**Messages** or **Footer** in the preview to change content or formatting.
**Display content** hides or shows the whole display; the trash icon clears messages.
Stopping translation clears that task's messages and keeps independent header/footer content.

In Unity, edit the caption material for colors. Use **+** to add another object;
**Text object** selects an existing one and the magnifier locates it. Objects share
text and layout while keeping their placement and material. The installer chooses
a transfer size that fits the avatar's parameter space; smaller transfers fill the
same 128-character display more slowly. See [caption details](presentation.md).

## Update or remove

Update through VCC/ALCOM, or import the newer Unity package if you installed manually.
Keep the generated XRT folders. Existing objects and placement are retained.

In **Caption Objects**, select an object and click **−**. After removing all text
objects, **Avatar Controls → Translation menu → −** removes the control. Save and
upload again. Remove these contributions from each avatar before uninstalling the
package. Unity's **Edit → Undo / Redo** reverses add/remove operations.

Your added children, components and replacement meshes/materials are kept.
[Setup help](troubleshooting.md) · [Compatibility](compatibility.md)
