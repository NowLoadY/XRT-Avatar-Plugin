# Compatibility and delivery status

This repository is a development baseline for protocol version 1. No binary,
VCC repository listing or end-to-end compatible XRTranslate release is published
by this initial source delivery.

| Component | Current status |
|---|---|
| Protocol and generated bindings | Defined in source |
| Rust control core | Implemented without transport or host runtime |
| Unity editor installer | Source implementation; Unity import/compilation not verified |
| XRTranslate host adapter | Specified; not implemented or integrated |
| OSCQuery transport integration | Specified; not implemented |
| In-game enable/disable | Requires host integration and live verification |
| Custom floating Avatar bubble | Presentation contract only; no renderer |

## Targets

The initial integration target is VRChat on Windows with XRTranslate running on
the same PC, in desktop or PCVR mode. Linux development of the pure Rust core is
independent of VRChat/Proton support. Standalone headsets, remote audio acquisition
and mobile clients are not validated integration targets.

The editor source targets Unity 2022.3.22f1 and the Avatar SDK 3.10.x API family.
These are target versions, not a tested compatibility claim. Confirm actual
SDK API and package constraints in an existing authoring environment before
publishing. Follow the [official Unity version](https://creators.vrchat.com/sdk/upgrade/current-unity-version/)
and [SDK releases](https://creators.vrchat.com/releases/) when updating the target.
No Unity installation is performed by this project or its source checks.

## Release acceptance

Before declaring a runnable release, record exact Unity, SDK, VRChat and XRTranslate
versions and verify the following in that environment:

1. Install on a disposable copy of an Avatar, preserve original menu/parameters,
   reject collisions and a full root menu, and restore an unchanged installation.
   An edited installation must produce a diagnostic without destructive removal.
2. Confirm generated parameters are unsaved/unsynchronized; upload using the
   official SDK, enable OSC and observe the control values on the correct Avatar.
3. Complete the shared transport and isolated host acquisition requirements in
   [host integration](host-integration.md). Menu on starts one configured live
   task; actual readiness is reported; off releases its capture and output work.
4. Verify off during startup, a late start callback, failure, duplicate requests,
   desktop cancellation, avatar change, reconnect and lost UDP updates. Existing
   meeting/media/host tasks and another OSC application remain unaffected.
5. Record the compatible host version, run the protocol consistency check, and
   package only tracked Unity source/assets plus the license. Publish a VPM listing
   only after its download URL and checksum refer to a real release artifact.

Do not interpret source delivery as completion of these checks. A later bubble
release has separate rendering, text, networking and platform acceptance criteria
in [presentation architecture](presentation-architecture.md).
