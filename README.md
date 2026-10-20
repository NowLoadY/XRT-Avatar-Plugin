<h1 align="center">XRT-Avatar-Plugin</h1>

<p align="center">
  <a href="https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/tag/v0.1.0">Download</a> ·
  <a href="#install">Install</a> ·
  <a href="docs/README.md">Documentation</a>
</p>

Control [XRTranslate](https://github.com/NowLoadY/XRTranslate) in your game and
show translations on your avatar. Big Walk sends translations to native chat.

<p align="center">
  <img src="docs/adapters/vrchat/images/board-placement-concept.jpg" alt="Kipfel, Mamehinata and Usasaki holding translation boards" width="900" />
  <br />
  <sub>Board concept · <a href="https://mukumi.booth.pm/items/5813187">Kipfel</a> · <a href="https://mukumi.booth.pm/items/4340548">Mamehinata</a> · <a href="https://mukumi.booth.pm/items/3550881">Usasaki</a> by もち山金魚</sub>
</p>

## Install

**Requires XRTranslate > 0.3.3.** Choose your game:

| Game | What you get | Setup |
|---|---|---|
| VRChat | Avatar menu and caption boards (PC) | [VCC / ALCOM](docs/adapters/vrchat/installation.md) |
| ChilloutVR | Avatar controls and caption boards | [Unity package](docs/adapters/chilloutvr/installation.md) |
| Overte | Tablet controls and caption objects | [Load script](docs/adapters/overte/installation.md) |
| Resonite | Native controls and text objects | [Connect with ResoniteLink](docs/adapters/resonite/installation.md) |
| Second Life | Attachment controls and floating captions | [Attachment and relay](docs/adapters/secondlife/installation.md) |
| Big Walk | Native chat translations | [Install mod](docs/adapters/bigwalk/installation.md) |

In XRTranslate, set your languages and audio input. Open **Game Plugins**, choose
your game, and turn on **Enable**. Follow its **Setup guide** to connect and start.
Translations appear by default; header, footer and formatting are optional.

## Help

Each setup guide covers use, updates and removal. For a problem, [open an issue](https://github.com/NowLoadY/XRT-Avatar-Plugin/issues)
with your game, plugin version and the step that failed.

## Acknowledgments

[VRCBitmapLed](https://github.com/lolosiax/VRCBitmapLed) by 洛洛希雅Lolosia inspired
the text buffer and parameter synchronization. Caption fonts use [Noto](https://github.com/notofonts/noto-cjk).
[BigTalk ConversationStarters](https://github.com/Gogogadgetjustice/bigwalkmods/tree/main/The%20Mods/ConversationStarters)
demonstrated Big Walk's native chat integration.

## License

[AGPL-3.0-only](LICENSE). Independent community project.
