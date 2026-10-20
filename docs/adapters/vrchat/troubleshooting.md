# VRChat setup help

Return to the [installation guide](installation.md) after completing these steps.

## Expressions setup

XRT creates missing menu and default parameter assets when you click **+**.
For an avatar with existing custom controls, it uses the Parameters assigned
in the Avatar Descriptor or the menu's own Parameters field.

If XRT asks for the original Parameters asset, select **VRC Avatar Descriptor →
Expressions** and assign the asset supplied with your avatar. Save the project
and click the refresh icon in the XRT window. This preserves the types and
settings required by the avatar's custom controls.

For a default avatar, adding XRT creates a custom Expressions menu in place of
VRChat's default menu. Click **−** to restore the original setup. To create your
own emote controls, follow VRChat's
[Expressions guide](https://creators.vrchat.com/avatars/expression-menu-and-controls/).

## Missing assets or duplicate setups

You can edit menus, add animator layers and move text objects after installation.
If a required asset is missing or more than one installation is assigned:

1. Save a copy of the project to keep your current changes.
2. Restore the avatar's XRT setup and its generated folder from a known working
   project backup. Keep the scene and assets from the same backup together.
3. Refresh the window and select the text object you want to manage. Use **−** to
   remove its XRT contribution while keeping your other content.

If adding another object is unavailable, check that Avatar Descriptor still uses
the caption FX controller and its caption parameters. Existing objects can still
be removed when that receiver assignment is incomplete.

## Caption is blank

Enable **Show captions** on XRTranslate's **Game Plugins** page. Select **Messages**
in the preview to check its content and audio-source settings, or choose content
for **Header** or **Footer**. For menu-controlled translation, turn on
**Expressions → XRT → Translation**. Wear the uploaded PC avatar and allow its
custom shaders. In Unity, the magnifier in Caption Objects selects the object
so you can check its placement and scale.

Messages expire individually. Stopping translation removes that task's messages;
the header and footer remain visible. The trash icon clears messages, while
disabling **Show captions** clears the whole surface.

## Connection or upload problems

- **XRT is missing in Unity:** finish importing the package and resolve errors in
  the Console. Check the [target Unity and SDK versions](compatibility.md#targets).
- **No menu in the game:** save and upload the modified avatar, then wear that
  uploaded version.
- **Connection unavailable:** enable OSC, select VRChat on XRTranslate's **Game Plugins**
  page, turn on **Enable**, and wear the avatar with XRT installed. Read the connection message there.
- **Input is busy:** stop the other XRTranslate task using the same audio input.
- **Cannot upload:** check VRChat's
  [account requirements and Builder messages](https://creators.vrchat.com/avatars/creating-your-first-avatar/#requirements).
