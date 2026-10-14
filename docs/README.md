# Documentation

XRT-Avatar-Plugin connects virtual social platforms to XRTranslate. Project
contracts define ownership and host integration; each platform adapter owns its
interaction, transport mapping, installation and compatibility documentation.

## Using releases

- [Release packaging](releases.md): artifact formats and the release workflow.
- [VRChat installation](adapters/vrchat/installation.md): install through Unity or VCC.

## Project contracts

- [Architecture](architecture.md): repository layout and component ownership.
- [Connector protocol](connector-protocol.md): pairing, reset, heartbeat and captions.
- [Host integration](host-integration.md): session acquisition, results and cleanup.
- [Presentation architecture](presentation-architecture.md): result rendering and visibility.
- [Refactoring contract](refactoring-contract.md): stable interfaces and verification.

## Platform adapters

| Adapter | Documentation | Source |
|---|---|---|
| VRChat | [Overview and setup](adapters/vrchat/README.md) | [`adapters/vrchat`](../adapters/vrchat/README.md) |
| ChilloutVR | [Setup](adapters/chilloutvr/installation.md) | [`adapters/chilloutvr`](../adapters/chilloutvr/README.md) |
| Overte | [Setup](adapters/overte/installation.md) | [`adapters/overte`](../adapters/overte/README.md) |
| Resonite | [Setup](adapters/resonite/installation.md) | [`adapters/resonite`](../adapters/resonite/README.md) |
| Second Life | [Setup](adapters/secondlife/installation.md) | [`adapters/secondlife`](../adapters/secondlife/README.md) |
