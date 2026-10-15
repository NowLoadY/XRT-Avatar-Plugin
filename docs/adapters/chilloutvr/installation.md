# Install for ChilloutVR

Add **XRT Translation** to your avatar's Advanced Avatar Settings to start and
stop translation in XRTranslate.

## Before you start

Use ChilloutVR and XRTranslate on the same PC, and have an avatar Unity project
you can edit and upload. Make a backup of the project.

XRTranslate must include **Avatar translation controls**, planned for **0.3.4**.
The current **0.3.3** release cannot use this plugin. See
[XRTranslate's Avatar controls guide](https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md)
for host setup.

## 1. Prepare your avatar project

Use a **CCK 4** project. Follow the official
[Unity and CCK setup](https://docs.chilloutvr.net/cck/setup/) for its downloads,
recommended Unity version and import steps. Import your avatar using its creator's
instructions and open its scene.

Sign in to the CCK with your username and **CCK Master Key**, available in
**Community Hub → Profile settings → Access Key Management**.

Finish the avatar's normal animation setup before adding XRT. If XRT asks for an
animator, follow [animator setup](troubleshooting.md#animator-setup).

## 2. Import XRT

1. Download [XRT-Avatar-Plugin for ChilloutVR](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-ChilloutVR-0.1.0-preview.4.unitypackage).
2. In Unity, choose **Assets → Import Package → Custom Package**, select the
   downloaded `.unitypackage` and import all its contents.
3. Open **Tools → XRT-Avatar-Plugin → ChilloutVR → Avatar Controls**.

## 3. Add the menu and upload

1. Save your avatar scene and leave Unity's Play mode off. Drag the avatar from
   the Hierarchy into the **Avatar** field, or select it and click the avatar icon.
2. Click **+** beside **Translation menu**. When the status shows **Added**, click
   the save icon. Existing avatar controls and animations are kept.
3. In the CCK Control Panel, open **Builder**, select this avatar and complete
   its details. Resolve any validation errors, then build and upload it.
4. Open ChilloutVR and wear the uploaded avatar.

Hover over an icon in the XRT window to see its action. The help icon opens this
guide. CCK 4's [upload workflow](https://docs.chilloutvr.net/cck/setup/) runs in
the Builder panel; leave Unity Play mode off.

## 4. Start translation

1. Open XRTranslate and configure your audio input, languages and outputs.
2. In **Settings → Plugins → Avatar translation controls**, select **ChilloutVR**
   and enable the plugin.
3. In ChilloutVR, enable OSC under **Settings → Implementation → OSC**, as shown
   in the [official OSC guide](https://docs.chilloutvr.net/chilloutvr/game/osc/).
4. Wait for XRTranslate to show **Off**, then turn **XRT Translation** on in
   **Advanced Avatar Settings**. Turn it off to stop translation.

After changing avatars or reconnecting, wait for **Off** and turn translation on
again. Choose caption and speech outputs in XRTranslate; Chatbox is in its OSC
plugin.

## Remove or update

To remove XRT, select the avatar in the XRT window, click **−** beside
**Translation menu**, save and upload again. Keep XRT installed until you have
removed its menu from every avatar that uses it.

To update, import the newer Unity package and keep the project's generated XRT
folders. The refresh icon checks the selected avatar. Remove XRT before
regenerating its animator, then add it again after attaching the new controller.
If the window reports an edited setup, follow
[recovery](troubleshooting.md#recover-an-edited-setup) before continuing.

[Setup help](troubleshooting.md) · [Compatibility](compatibility.md)

This is a preview; Unity import, upload and in-game operation still need live
validation. The package currently adds translation menu controls.
