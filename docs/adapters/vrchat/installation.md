# Install for VRChat

Add **XRT → Translation** to your avatar's Expressions menu to start and stop
translation in XRTranslate.

## Before you start

Use VRChat and XRTranslate on the same PC, and have an avatar Unity project you
can edit and upload. Make a backup of the project.

## 1. Prepare your avatar project

Open your existing project in **VRChat Creator Companion (VCC)**. If you are
starting fresh, follow [VCC setup](https://vcc.docs.vrchat.com/guides/getting-started/),
create an **Avatar** project and import your avatar using its creator's instructions.
VCC helps install the Unity version required by VRChat.

Your account needs **New User** rank or higher to upload. Steam or Meta accounts
must first be linked to a VRChat account. See
[VRChat's upload requirements](https://creators.vrchat.com/avatars/creating-your-first-avatar/#requirements).

## 2. Install XRT

### Through VCC

1. Open **Settings → Packages → Add Repository** and paste:

   ```text
   https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/main/vpm/index.json
   ```

2. Confirm and enable **XRT-Avatar-Plugin**. Enable **Show Pre-Release Packages**
   to see this preview.
3. Open your project's **Manage Project**, add **XRT-Avatar-Plugin (VRChat)**,
   then open the project in Unity.

VCC manages later updates. These steps follow its
[community repository workflow](https://vcc.docs.vrchat.com/guides/community-repositories/).

### Or import the download

Download [XRT-Avatar-Plugin for VRChat](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.5/XRT-Avatar-Plugin-VRChat-0.1.0-preview.5.unitypackage).
In your avatar project, choose **Assets → Import Package → Custom Package**,
select the downloaded `.unitypackage` and import all its contents. Use one
installation method for the project.

## 3. Add the menu

1. Open and save the scene containing your avatar. Leave Unity's Play mode off.
2. Open **Tools → XRT-Avatar-Plugin → Avatar Controls**.
3. Select your avatar in the Hierarchy. The **Avatar** field follows your selection;
   you can also drag an avatar into it.
4. Click **+** beside **Translation menu**. When the status shows **Added**, click
   the save icon.

You can also open the window from the Avatar Descriptor component's context menu.
Existing menu controls are kept. If the menu was full, they appear in an
**Avatar** submenu beside **XRT**. Hover over any icon to see its action.
For a default avatar, **+** creates its XRT menu and parameters automatically;
**−** restores VRChat's default menu. If the avatar has custom controls but is
missing their Parameters asset, see [Expressions setup](troubleshooting.md#expressions-setup).

## 4. Upload the avatar

Open **VRChat SDK → Show Control Panel**, sign in and select **Builder**. Select
your avatar, complete its details and resolve the listed errors. Choose the PC
platform and **Build & Publish**. See the
[official upload steps](https://creators.vrchat.com/avatars/creating-your-first-avatar/#step-6---going-to-the-build-tab--checking-if-the-avatar-is-ok)
for the SDK panel. Wear the uploaded avatar in VRChat.

## 5. Start translation

1. Open XRTranslate and configure your audio input, languages and outputs.
2. In **Settings → Plugins → Avatar translation controls**, select **VRChat**
   and enable the plugin.
3. In VRChat's Action Menu, turn on **OSC → Enabled** as described in the
   [official OSC guide](https://docs.vrchat.com/docs/osc-overview#enabling-it).
4. Wait for XRTranslate to show **Off**, then open
   **Expressions → XRT → Translation**. Turn it on to start and off to stop.

XRTranslate shows **Starting** while preparing translation, then **Active**.
After changing avatars or reconnecting, wait for **Off** and turn translation on
again. Choose caption, overlay and speech outputs in XRTranslate; Chatbox is in
its OSC plugin.

## Remove or update

To remove XRT from an avatar, select it in the XRT window, click **−** beside
**Translation menu**, save and upload again. Keep XRT installed until you have
removed its menu from every avatar that uses it.

Use Unity's **Edit → Undo / Redo** to reverse an add or removal. After removing
XRT, you can edit the original menu and add XRT again.

For updates, use VCC if it manages your installation; otherwise import the newer
Unity package. Keep the project's generated XRT folders. The refresh icon checks
the selected avatar. If it reports an edited setup, follow
[recovery](troubleshooting.md#recover-an-edited-setup) before continuing.

[Setup help](troubleshooting.md) · [Compatibility](compatibility.md)
