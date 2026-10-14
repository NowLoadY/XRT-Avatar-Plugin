# XRT-Avatar-Plugin

Open-source game plugins connecting virtual social platforms to [XRTranslate](https://github.com/NowLoadY/XRTranslate).
Control translation through avatar menus, buttons or attachments, and display captions using native platform components.

## Features

- Start and stop translation in-game and view its current status.
- Use XRTranslate's configured languages, audio sources, recognition, translation and output settings.
- Keep translation sessions and their display lifecycle scoped to each connection.

## Platform connectors

| Platform | Game components | Setup |
|---|---|---|
| VRChat | Expressions menu and local avatar parameters | [Install](docs/adapters/vrchat/installation.md) |
| ChilloutVR | CCK avatar menu and local parameters | [Install](docs/adapters/chilloutvr/installation.md) |
| Overte | Avatar script, tablet controls and local captions | [Install](docs/adapters/overte/installation.md) |
| Resonite | ResoniteLink bridge for avatar controls and text components | [Install](docs/adapters/resonite/installation.md) |
| Second Life | LSL attachment controls, floating text and a connection bridge | [Install](docs/adapters/secondlife/installation.md) |

Development preview. See each platform guide for compatibility and in-game validation status.
ResoniteLink requires a session you host with the appropriate permissions; Second Life requires an HTTPS relay.

This project maintains game components and connector protocols. XRTranslate provides the audio and translation runtime,
and its OSC plugin provides OSC Chatbox output. See the [documentation](docs/README.md) for architecture and development guidelines.

## License

[AGPL-3.0-only](LICENSE). This is an independent community project.
