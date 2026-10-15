# VRChat adapter

Adds a translation toggle at `Expressions → XRT → Translation` on the player's
Avatar. The adapter uses the official Avatar SDK and OSC/OSCQuery; XRTranslate
owns audio, translation and output. Each Avatar using the controls needs its own
installation. Caption boards show messages with an optional header and footer
on editable Avatar objects.

[Installation and removal](installation.md) · [Compatibility](compatibility.md)

## Contracts

- [Control protocol](control-protocol.md): parameters, reset confirmation and connection lifecycle.
- [Host integration](host-integration.md): XRTranslate modules, session ownership and settings.
- [Presentation](presentation.md): caption rendering, synchronization and installation ownership.

See [development](development.md) for source layout and developer commands.
The [project architecture](../../architecture.md) defines shared ownership rules.
