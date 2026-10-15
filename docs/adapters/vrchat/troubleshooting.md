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

## Recover an edited setup

If the window reports an edited or duplicate XRT setup:

1. Save a copy of the project to keep your current changes.
2. Restore the avatar's XRT setup and its generated folder from a known working
   project backup. Keep the scene and assets from the same backup together.
3. Refresh the window, remove XRT with **−**, then make your intended changes to
   the avatar's original Expressions. Add XRT again when those assets are saved.

## Connection or upload problems

- **XRT is missing in Unity:** finish importing the package and resolve errors in
  the Console. Check the [target Unity and SDK versions](compatibility.md#targets).
- **No menu in the game:** save and upload the modified avatar, then wear that
  uploaded version.
- **Connection unavailable:** enable OSC, select VRChat in XRTranslate's Avatar
  translation controls and wear the avatar with XRT installed. Read the message
  in XRTranslate's plugin settings.
- **Input is busy:** stop the other XRTranslate task using the same audio input.
- **Cannot upload:** check VRChat's
  [account requirements and Builder messages](https://creators.vrchat.com/avatars/creating-your-first-avatar/#requirements).
