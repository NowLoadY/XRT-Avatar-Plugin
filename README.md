<h1 align="center">XRT-Avatar-Plugin</h1>

<p align="center">
  <a href="https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/tag/v0.1.0-preview.4">Download</a> ·
  <a href="#install">Install</a> ·
  <a href="docs/README.md">Help</a>
</p>

Start and stop [XRTranslate](https://github.com/NowLoadY/XRTranslate) from your avatar's menu or in-game controls. Choose your microphone, languages and translation output in XRTranslate, then use the controls while you play.

In Overte, create a caption object or connect one you have designed. Place it on a prop, a board or your avatar, and adjust its font, color and shape with the game's editing tools.

## Previews

Create a caption object, choose an existing one and preview its text in Overte's caption page.

<p align="center">
  <img src="assets/previews/caption-designer.jpg" alt="Overte caption page with horizontal, portrait and board presets and object selection controls" width="900" />
</p>

Horizontal layout:

<p align="center">
  <img src="assets/previews/caption-horizontal.jpg" alt="Horizontal caption layout in the positioning and text preview tool" width="900" />
</p>

Portrait layout. For a board, combine transparent lettering with your own fonts and artwork.

<p align="center">
  <img src="assets/previews/caption-portrait.jpg" alt="Portrait caption layout in the positioning and text preview tool" width="900" />
</p>

## Install

**Requires XRTranslate > 0.3.3.**

Choose the package for your game:

| Game | Download | What you get | Setup |
|---|---|---|---|
| VRChat | [Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-VRChat-0.1.0-preview.4.unitypackage) | Avatar translation menu | [Unity / VCC](docs/adapters/vrchat/installation.md) |
| ChilloutVR | [Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-ChilloutVR-0.1.0-preview.4.unitypackage) | Avatar translation toggle | [CCK project](docs/adapters/chilloutvr/installation.md) |
| Overte | [Script bundle](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.4/XRT-Avatar-Plugin-Overte-0.1.0-preview.4.zip) | Tablet controls and caption objects | [Load in Overte](docs/adapters/overte/installation.md) |

1. **Install in your game project.** For VRChat or ChilloutVR, import the Unity package, open **Tools → XRT-Avatar-Plugin**, add the controls to your avatar and upload it with the game's SDK. For Overte, extract the bundle and load its script through **Running Scripts**.
2. **Connect XRTranslate.** Open **Settings → Plugins → Avatar translation controls**, select your game and enable the plugin. Follow your game's setup guide to enable OSC or pair its controls.
3. **Start in the game.** Use **XRT → Translation** in VRChat, **XRT Translation** in ChilloutVR, or the tablet's play icon in Overte. Use the same control to stop.

[Resonite](docs/adapters/resonite/installation.md) and [Second Life](docs/adapters/secondlife/installation.md) have separate setup guides. Resonite needs a session with ResoniteLink enabled; Second Life needs a configured relay service.

## Help

See the [game guides](docs/README.md#player-guides) for setup, changing your objects and removing the plugin. For a problem, [open an issue](https://github.com/NowLoadY/XRT-Avatar-Plugin/issues) and include your game, plugin version and the step that failed.

## License

[AGPL-3.0-only](LICENSE). Independent community project.
