# VRChat setup help

Return to the [installation guide](installation.md) after completing these steps.

## Expressions setup

If your avatar already has a custom menu, keep its assigned Menu and Parameters.
For an avatar using VRChat's implicit default menu, create the custom Expressions
that you want to retain before adding XRT:

1. In the Project window, create **VRChat → Avatars → Expressions Menu** from the
   **Assets → Create** menu. Select it and click **Create** beside Parameters.
   Keep the SDK's default parameters when using its default action animations.
2. Select the avatar's **VRC Avatar Descriptor → Expressions → Customize** and
   assign these saved Menu and Parameters assets.
3. Add the controls you want in the menu. For the SDK's default Action layer,
   the standard emote controls are **Button** controls using the `VRCEmote` Int
   parameter: Wave 1, Clap 2, Point 3, Cheer 4, Dance 5, Backflip 6, Sadness 7,
   and Die 8. Custom Action layers may use a different mapping; retain the
   avatar author's supplied assets for those avatars.
4. Save the assets and return to the XRT window. Click the refresh icon,
   then **+** beside **Translation menu**.

The custom menu becomes your avatar's Expressions menu. The installer preserves
its controls and does not replace the Action animator.

See VRChat's [Expressions setup guide](https://creators.vrchat.com/avatars/expression-menu-and-controls/)
for the SDK's menu and parameter fields.

## Recover an edited setup

If the window reports an edited or duplicate XRT setup:

1. Save a copy of the project to keep your current changes.
2. Restore the avatar's XRT setup and its generated folder from a known working
   project backup. Keep the scene and assets from the same backup together.
3. Refresh the window, remove XRT with **−**, then make your intended changes to
   the avatar's original Expressions. Add XRT again when those assets are saved.

For a setup that was removed before its original Expressions changed, keep a
project backup and inspect the retained `Installation.asset` in the generated
XRT folder. Remove that record only after checking the avatar uses the original
Menu and Parameters and that no avatar still uses the generated copies. The
next **+** operation creates a fresh copy of the current Expressions. Ask the
avatar author for help if you are unsure which assets are in use.

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
