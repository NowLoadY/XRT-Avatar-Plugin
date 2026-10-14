# Install for ChilloutVR

## Add the translation menu

1. Open your ChilloutVR CCK project and finish the avatar's normal animator
   setup. The CCK's **Create Animator** and **Attach created Override to Avatar**
   controls provide this setup for an avatar using generated settings.
2. Download [the ChilloutVR Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.2/XRT-Avatar-Plugin-ChilloutVR-0.1.0-preview.2.unitypackage).
   In Unity, choose **Assets → Import Package → Custom Package** and import it.
3. Open **Tools → XRT-Avatar-Plugin → ChilloutVR → Avatar Controls**. Save the
   scene, then drag the avatar into the **Avatar** field. You can also select it
   in the Hierarchy and click the avatar icon beside the field.
4. Click **+** beside **Translation menu**. Your existing avatar controls and
   animation overrides are kept.
5. Click the save icon and upload through the CCK.

The status changes to **Added** when the avatar is ready. Hover over an icon for
its action. The help icon opens this guide; **In game** expands the connection
instructions.

## Start translation

1. Run an [XRTranslate development build](https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md)
   with Avatar controls, planned for XRTranslate 0.3.4. Configure the audio
   source, languages and translation services.
2. Enable OSC in **Settings → Implementation → OSC** in ChilloutVR.
3. In XRTranslate, enable Avatar translation controls and select **ChilloutVR**.
4. Wear the uploaded avatar and wait for the connection to show **Off**.
5. Turn **XRT Translation** on in Advanced Avatar Settings to start translation,
   and off to stop it.

After changing avatars or reconnecting, wait for **Off** and turn translation
on again. Configure Chatbox output in XRTranslate's OSC plugin.

## Check or remove

The refresh icon checks the selected avatar. Click **−** beside **Translation
menu** to remove XRT, then save and upload the avatar again. Keep the plugin and
its generated folders in the project until you have removed XRT from each avatar.

If you need to regenerate the avatar's animator, remove XRT first. Regenerate
and attach the controller in the CCK, then add XRT again.

## Recover an edited setup

If the window reports an edited or duplicate XRT setup:

1. Save a copy of the project to keep your current changes.
2. Restore the avatar's XRT setup and its generated folder from a known working
   project backup. Keep the scene and assets from the same backup together.
3. Refresh the window, remove XRT with **−**, then make your intended changes to
   the avatar. Add XRT again after finishing its animator setup.

Ask the avatar author for help if you are unsure which controller or settings to
restore. Removing the editor package alone does not remove the avatar controls.

## Troubleshooting

- **No toggle in the game:** save and upload the modified avatar, then wear
  that uploaded version.
- **Connection unavailable:** check OSC, the selected XRTranslate connector and
  the installed avatar. Keep ChilloutVR's default OSCQuery service prefix.
- **Audio source busy:** stop the task already using that source in XRTranslate.

See [compatibility](compatibility.md) for supported versions and verification.

Official guides: [CCK avatar settings](https://docs.chilloutvr.net/cck/components/cvr-avatar/),
[OSC settings](https://docs.chilloutvr.net/chilloutvr/game/osc/).
