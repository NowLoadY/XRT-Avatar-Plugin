# VRChat adapter

Adds a translation toggle at `Expressions → XRT → Translation` on the player's
Avatar. The adapter uses the official Avatar SDK and OSC/OSCQuery; XRTranslate
owns audio, translation and output. Each Avatar using the controls needs its own
installation. No world component is required.

The adapter is a development preview. Review [compatibility](compatibility.md)
before following [installation and removal](installation.md).

## Contracts

- [Control protocol](control-protocol.md): parameters, reset confirmation and connection lifecycle.
- [Host integration](host-integration.md): XRTranslate modules, session ownership and settings.
- [Presentation](presentation.md): VRChat-specific limits for future Avatar rendering.

Source and developer commands are in [`adapters/vrchat`](../../../adapters/vrchat/README.md).
The [project architecture](../../architecture.md) defines shared ownership rules.
