# Install for VRChat

Use your existing VRChat Avatar project in Creator Companion (VCC), with Unity
2022.3.22f1 and Avatar SDK 3.10.x. Download the companion
[XRTranslate 0.3.4-preview.1](https://github.com/NowLoadY/XRTranslate/releases/tag/v0.3.4-preview.1)
for Windows or Linux. XRTranslate 0.3.3 does not contain the Avatar connector.

## Import the Unity package

1. Download [XRT-Avatar-Plugin-VRChat-0.1.0-preview.1.unitypackage](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.1/XRT-Avatar-Plugin-VRChat-0.1.0-preview.1.unitypackage).
2. Open your Avatar project through VCC. In Unity, choose **Assets → Import
   Package → Custom Package**, select the downloaded file and import its contents.
3. Open **Tools → XRT-Avatar-Plugin → Avatar Controls**.

The package contains the editor tool. You do not need to clone this repository,
compile Rust or copy source folders. Keep the imported files together under
`Assets/XRT-Avatar-Plugin/VRChat`.

## Install through VCC

VCC manages package updates and dependencies. Use this path when managing your
Avatar tools through Creator Companion:

1. In VCC, open **Settings → Packages → Add Repository** and enter:

   ```text
   https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/main/vpm/index.json
   ```

2. Add and enable the **XRT-Avatar-Plugin** repository. Enable **Show Pre-Release
   Packages** to see the preview.
3. Open **Manage Project** for your Avatar project and add
   **XRT-Avatar-Plugin (VRChat)**.
4. Open the project and choose **Tools → XRT-Avatar-Plugin → Avatar Controls**.

VCC can migrate the tool folder from the Unity-package installation. Generated
Avatar assets and restore records are stored separately and remain in the project.
After moving to VCC, use VCC for updates.

## Add controls to your avatar

1. Open and save the scene containing your avatar. Select the avatar in the
   Hierarchy, then click **Use Selected Avatar** in the plugin window, or drag
   its Avatar Descriptor into the **Avatar** field.
2. Review the status shown in the window. If the avatar needs expression setup,
   follow the displayed instructions in its SDK **Expressions** panel.
3. Click **Add Translation Menu**. Existing controls are preserved; a full menu
   is retained in an **Avatar** submenu beside the new **XRT** submenu.
4. Save the scene, then upload your avatar using the VRChat SDK's normal build
   and upload panel.

The tool creates an `XRT → Translation` toggle and local control parameters.
It copies the menu and parameter assets and keeps a restore record. Repeating
installation checks the existing installation rather than adding another menu.

## Avatars that use default Expressions

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
4. Save the assets and return to the XRT window. Click **Check Installation**,
   then **Add Translation Menu**.

The custom menu becomes your avatar's Expressions menu. The installer preserves
its controls and does not replace the Action animator.

## Start translation in VRChat

1. Extract and start XRTranslate. Complete audio and model setup, then choose
   your input, languages and desired outputs.
2. In **Settings → Plugins → Avatar translation controls**, select **VRChat**
   and enable the plugin.
3. Wear the uploaded avatar and enable **OSC** in VRChat's Action Menu.
4. Wait for the host to show **Off**, then use **Expressions → XRT → Translation**.
   Turn it on to start, and off to stop its translation session.

The menu checkmark is your request. XRTranslate reports Starting until audio and
translation are ready, then Active. Connection changes reset the control to Off.
Configure OSC Chatbox, overlays and TTS in XRTranslate according to your needs.

## Check, update or remove

**Check Installation** checks the selected avatar without changing it. To remove
its controls, use **Remove Translation Menu**, save and upload the avatar again.
Keep the editor package installed until you have restored every avatar that uses
it. Then remove the VCC package or the imported `VRChat` tool folder.

If generated assets or descriptor references were edited after installation,
the window identifies the conflict instead of overwriting those edits. Restore
the expected references or use your project backup before removing the component.
Keep your generated asset folders and their installation records with the Avatar
project; removing the editor package alone does not restore the avatar.

## Troubleshooting

- **No menu in the game:** save and upload the modified avatar, then wear that
  uploaded version.
- **Control stays unavailable:** check that the correct XRTranslate version is
  running, the VRChat connector and OSC are enabled, and the installed avatar is
  selected. Read the diagnostic in XRTranslate's plugin settings.
- **Input is busy:** stop the task already using the selected microphone or
  system input before starting this avatar's session.
- **Compilation errors after import:** confirm the project is an Avatar project
  with the supported SDK. Use one installation method and resolve Console
  errors before uploading.

This preview has package and protocol checks; Unity import and game-client
acceptance remain pending. See [compatibility](compatibility.md) for version and
release criteria. The current package installs menu controls; custom Avatar
bubble rendering has a separate [presentation contract](presentation.md).

Official guides: [VCC community repositories](https://vcc.docs.vrchat.com/guides/community-repositories/),
[Unity package import](https://docs.unity3d.com/2022.3/Documentation/Manual/AssetPackagesImport.html),
[VRChat Avatar upload](https://creators.vrchat.com/avatars/creating-your-first-avatar/).
