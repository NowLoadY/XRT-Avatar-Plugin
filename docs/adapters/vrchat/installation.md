# Install for VRChat

Add **XRT → Translation** to your avatar's Expressions menu to start and stop
translation in XRTranslate. Add a caption object to show text on
a board or another surface on your PC avatar.

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

Download [XRT-Avatar-Plugin for VRChat](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.7/XRT-Avatar-Plugin-VRChat-0.1.0-preview.7.unitypackage).
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

## 4. Add a caption object

1. Open **Tools → XRT-Avatar-Plugin → Caption Objects**.
2. Select the avatar. Set **Attach to** to a hand, chest, prop or another object
   inside it.
3. Choose **Horizontal**, **Portrait** or **Board**, then click **+**. Board creates
   transparent lettering for your own sign or artwork.
4. Position, rotate and scale the created object in the Scene view. Edit its
   material to change text and background colors. The preview text lets you
   inspect the font and layout.
5. Save the scene.

To add another text object, choose its **Attach to** parent under **New object**
and press **+** again. Use **Text object** to select an existing object, then the
magnifier to find it in the Scene view. Objects share the same text and layout;
each keeps its own placement, scale and material. Parent transparent Board text
to your own sign, clothing accessory or animated prop.

The installer chooses a transfer size that fits the avatar's remaining parameter
space. All sizes display up to 128 characters; smaller transfers fill the board
more slowly.

## 5. Upload the avatar

Open **VRChat SDK → Show Control Panel**, sign in and select **Builder**. Select
your avatar, complete its details and resolve the listed errors. Choose the PC
platform and **Build & Publish**. See the
[official upload steps](https://creators.vrchat.com/avatars/creating-your-first-avatar/#step-6---going-to-the-build-tab--checking-if-the-avatar-is-ok)
for the SDK panel. Wear the uploaded avatar in VRChat.

## 6. Start translation

1. Open XRTranslate and configure your audio input, languages and outputs.
2. Enable **Avatar** in **Settings → Plugins**, then open **Avatar** from the
   sidebar. Select **VRChat** and enable **Display content**. Click **Header**,
   **Messages** or **Footer** in the preview to choose what appears there.
3. In VRChat's Action Menu, turn on **OSC → Enabled** as described in the
   [official OSC guide](https://docs.vrchat.com/docs/osc-overview#enabling-it).
4. Wait for XRTranslate to show **Off**, then open
   **Expressions → XRT → Translation**. Turn it on to start and off to stop.

XRTranslate shows **Starting** while preparing translation, then **Active**.
Completed translations appear progressively on the caption object. Other players
can see them when your PC avatar and its shaders are shown.
Turning translation off removes that task's messages; the header and footer
keep displaying their selected content. The trash icon clears messages, and
**Display content** turns the entire display on or off.
After changing avatars or reconnecting, wait for **Off** and turn translation on
again. Overlay, speech and OSC Chatbox have their own output settings.

## Remove or update

Select a **Text object** and press **−** in **Caption Objects** to remove its XRT
text. Your added components, children and replacement meshes or materials are
kept. To remove the translation menu, remove all text objects first, then select
the avatar in **Avatar Controls** and click **−** beside
**Translation menu**, save and upload again. Keep XRT installed until you have
removed its menu from every avatar that uses it.

Use Unity's **Edit → Undo / Redo** to reverse an add or removal. After removing
XRT, you can continue editing the assigned menu and add XRT again.

For updates, use VCC if it manages your installation; otherwise import the newer
Unity package. Existing text objects and their placement are kept. Keep the
project's generated XRT folders. The refresh icon checks the selected avatar;
see [setup help](troubleshooting.md) for missing assets or duplicate setups.

[Setup help](troubleshooting.md) · [Compatibility](compatibility.md)
