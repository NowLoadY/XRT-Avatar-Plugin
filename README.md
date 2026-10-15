<h1 align="center">XRT-Avatar-Plugin</h1>

<p align="center">
  <a href="https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/tag/v0.1.0-preview.4">Download</a> ·
  <a href="#install">Install</a> ·
  <a href="docs/README.md">Guides</a>
</p>

Start and stop [XRTranslate](https://github.com/NowLoadY/XRTranslate) from your avatar's menu or in-game controls. Display translations on editable text objects in Overte and Resonite, or on attachments in Second Life.

<p align="center">
  <img src="docs/adapters/vrchat/images/board-placement-concept.jpg" alt="Kipfel, Mamehinata and Usasaki holding white translation boards, shown in full body" width="900" />
  <br />
  <sub>Board concept · <a href="https://mukumi.booth.pm/items/5813187">Kipfel</a> · <a href="https://mukumi.booth.pm/items/4340548">Mamehinata</a> · <a href="https://mukumi.booth.pm/items/3550881">Usasaki</a> by もち山金魚</sub>
</p>

## Preview

*AI-generated illustrations.*

**Unity · VRChat avatar setup**

Open **Tools → XRT-Avatar-Plugin → Avatar Controls** in your avatar project. Select your avatar and click **+** beside **Translation menu**, then save the scene and upload through the VRChat SDK.

<p align="center">
  <img src="assets/previews/vrchat-unity-illustration.jpg" alt="AI-generated illustration of the Unity avatar installer before installation, with the plus button enabled" width="900" />
</p>

**VRChat · Translation switch**

Open VRChat's Action Menu (**R** on desktop), then **Expressions → XRT → Translation**. Select **Translation** to turn it on or off.

<p align="center">
  <img src="assets/previews/vrchat-menu-illustration.jpg" alt="AI-generated illustration of the Translation toggle in VRChat's XRT radial submenu" width="900" />
</p>

**Overte · Caption objects**

Create a horizontal or portrait text object, or place the text on your own board or artwork. Use Overte's Create tools to position it and adjust its appearance.

<p align="center">
  <img src="assets/previews/overte-captions-illustration.jpg" alt="AI-generated illustration of three separate Overte caption layouts: horizontal, portrait and text on a custom board" width="900" />
</p>

## Install

**Requires XRTranslate > 0.3.3.**

Choose your game and follow its setup guide:

| Game | Features | Download | Setup |
|---|---|---|---|
| VRChat | Avatar translation menu | [Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-VRChat-0.1.0-preview.4.unitypackage) | [Unity / VCC](docs/adapters/vrchat/installation.md) |
| ChilloutVR | Avatar translation toggle | [Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-ChilloutVR-0.1.0-preview.4.unitypackage) | [CCK project](docs/adapters/chilloutvr/installation.md) |
| Overte | Tablet controls and caption objects | [Script bundle](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-Overte-0.1.0-preview.4.zip) | [Running Scripts](docs/adapters/overte/installation.md) |
| Resonite | Avatar controls and caption objects | [Source archive](https://github.com/NowLoadY/XRT-Avatar-Plugin/archive/refs/tags/v0.1.0-preview.4.zip) | [ResoniteLink](docs/adapters/resonite/installation.md) |
| Second Life | Attachment controls and captions | [Source archive](https://github.com/NowLoadY/XRT-Avatar-Plugin/archive/refs/tags/v0.1.0-preview.4.zip) | [Attachment / relay](docs/adapters/secondlife/installation.md) |

In XRTranslate, set your audio input, languages and outputs. Open **Settings → Plugins → Avatar translation controls**, select your game and enable the plugin. Follow your game's guide to connect it and use the translation switch.

## Help

The [game guides](docs/README.md#game-guides) cover setup, customization and removal. To report a problem, [open an issue](https://github.com/NowLoadY/XRT-Avatar-Plugin/issues) with your game, plugin version and the step that failed.

## License

[AGPL-3.0-only](LICENSE). Independent community project.
