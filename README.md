# XRT-Avatar-Plugin

Control [XRTranslate](https://github.com/NowLoadY/XRTranslate) from your avatar's menus, buttons or attachments.
Game components share XRTranslate's audio, translation and output settings.

## Get started

1. Set up an [XRTranslate development build with Avatar controls](https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md). Support is planned for XRTranslate **0.3.4**; the current 0.3.3 release does not include this connector.
2. Install the component for your platform below.
3. In XRTranslate, open **Settings → Plugins → Avatar translation controls**, select your game and enable the plugin.

| Platform | Download | Setup |
|---|---|---|
| VRChat | [Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.3/XRT-Avatar-Plugin-VRChat-0.1.0-preview.3.unitypackage) | [Import or install through VCC](docs/adapters/vrchat/installation.md) |
| ChilloutVR | [Unity package](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.3/XRT-Avatar-Plugin-ChilloutVR-0.1.0-preview.3.unitypackage) | [Install on your avatar](docs/adapters/chilloutvr/installation.md) |
| Overte | [Script bundle](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/download/v0.1.0-preview.3/XRT-Avatar-Plugin-Overte-0.1.0-preview.3.zip) | [Load and pair](docs/adapters/overte/installation.md) |

For VRChat, import the package into your Avatar project, then open **Tools → XRT-Avatar-Plugin → Avatar Controls**.
Select your avatar and add its translation menu. [View all release files](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases/tag/v0.1.0-preview.3).

[Resonite](docs/adapters/resonite/installation.md) and [Second Life](docs/adapters/secondlife/installation.md) provide bridge-based connectors with separate setup.
Overte and Resonite provide editable caption objects and a text source for custom displays.
Choose a horizontal, portrait or board layout, or bind your own text object.
Second Life exposes a linkset text source and an optional hover-text receiver.
ResoniteLink requires a session you host with the appropriate permissions; Second Life requires an HTTPS relay.

This is a development preview. Package checks are complete; Unity import and game-client acceptance remain pending.
The VRChat package provides menu controls. Caption output is configured in XRTranslate; OSC Chatbox is provided by its OSC plugin.

## Development

See the [documentation](docs/README.md) for architecture, connector contracts and release packaging.

## License

[AGPL-3.0-only](LICENSE). This is an independent community project.
