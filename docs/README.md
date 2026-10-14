# Documentation map

Architecture documents define current ownership, dependency direction, shared
contracts and resource lifetimes. Keep them short and update the owning rule
when a boundary changes. Wire fields belong in the protocol manifest; build,
installation and release procedures belong in operational guides. Do not put
design discussions, work logs or duplicated implementation details here.

## Contracts

- [Architecture](architecture.md): module ownership and dependency direction.
- [Control protocol](control-protocol.md): request, acknowledgement and lifecycle.
- [Presentation architecture](presentation-architecture.md): result consumption
  and the boundary for future Avatar bubbles.
- [Refactoring contract](refactoring-contract.md): compatibility and verification.

## Integration and operations

- [Host integration](host-integration.md): XRTranslate composition and required
  integration work.
- [Installation](installation.md): editor package, Avatar installation and removal.
- [Compatibility](compatibility.md): delivery status, supported targets and the
  requirements for a runnable release.
