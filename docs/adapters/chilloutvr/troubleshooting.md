# ChilloutVR setup help

Return to the [installation guide](installation.md) after completing these steps.

## Animator setup

If XRT asks you to configure an animator, select the avatar's **CVR Avatar**
component. For a generated Advanced Avatar Settings setup, use **Create Animator**
and then **Attach created Override to Avatar**. Keep an avatar author's custom
controller unless their instructions call for regeneration. See the
[CCK avatar settings](https://docs.chilloutvr.net/cck/components/cvr-avatar/#advanced-avatar-setting).

Remove XRT before regenerating an installed avatar's animator. Attach the new
controller, save the scene, and then add XRT again.

## Recover an edited setup

If the window reports an edited or duplicate XRT setup:

1. Save a copy of the project to keep your current changes.
2. Restore the avatar's XRT setup and its generated folder from a known working
   project backup. Keep the scene and assets from the same backup together.
3. Refresh the window, remove XRT with **−**, then make your intended changes to
   the avatar. Add XRT again after finishing its animator setup.

Ask the avatar author for help if you are unsure which controller or settings to
restore. Removing the editor package alone does not remove the avatar controls.

## Connection or upload problems

- **XRT is missing in Unity:** finish importing the package and resolve errors in
  the Console. Check the [target CCK version](compatibility.md).
- **No toggle in the game:** save and upload the modified avatar, then wear that
  uploaded version.
- **Connection unavailable:** enable OSC and select ChilloutVR in XRTranslate's
  Avatar translation controls. If you changed the OSCQuery launch settings,
  restore the default service prefix.
- **Audio source busy:** stop the other XRTranslate task using that audio input.
- **Cannot sign in to the CCK:** use your CCK Master Key from
  [Community Hub → Profile settings → Access Key Management](https://docs.chilloutvr.net/cck/setup/#signing-in-to-the-cck).
- **Upload blocked:** resolve the errors listed in the CCK Builder tab. The
  [official validation guide](https://docs.chilloutvr.net/cck/validations/) explains
  its messages.
