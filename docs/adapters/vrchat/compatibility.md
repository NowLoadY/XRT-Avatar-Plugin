# VRChat compatibility

The adapter provides a VPM package and a Unity asset package. Host requirements
are recorded in [`release/compatibility.json`](../../../release/compatibility.json).

## Verification status

Package checks cover archive contents, GUIDs, protocol bindings and download
hashes. Unity import and compilation, Avatar upload and game-client interaction
remain unverified. Linux/Proton, standalone headsets and mobile clients have no
recorded runtime acceptance.

| Component | Current status |
|---|---|
| Protocol and generated bindings | Defined in source |
| Rust control core | Implemented; no transport or inference dependencies |
| Unity editor installer | Implemented |
| XRTranslate host adapter | Implemented in `plugins::avatar` and `avatar_host` |
| Shared OSCQuery transport | Implemented in `plugins::osc::transport` |
| In-game enable/disable | Runtime path implemented |
| Avatar caption object | Shared SDF assets, VRChat receiver and XRTranslate sender implemented |

## Targets

The integration target is VRChat on Windows with XRTranslate running on the
same PC, in desktop or PCVR mode.

The editor source targets Unity 2022.3.22f1 and the Avatar SDK 3.10.x API family.
The default-parameter API and its editor assembly reference have been checked
against the official SDK 3.10.0 and 3.10.5 source packages. Caption Parameter
Driver fields, default FX identity and build callbacks were checked against the
3.10.x SDK source and assemblies. The bundled font atlas and proportional glyph
layout have an atlas-derived CPU proof; this does not establish Unity rendering.
Follow the [official Unity version](https://creators.vrchat.com/sdk/upgrade/current-unity-version/)
and [SDK releases](https://creators.vrchat.com/releases/) when updating the target.

## Release acceptance

For each accepted configuration, record exact Unity, SDK, VRChat and XRTranslate
versions and verify the following:

1. Install on a copy of an Avatar, preserve original menu/parameters, reject
   collisions, retain a full root menu in a submenu, and restore an unchanged installation. An
   edited installation must produce a diagnostic without destructive removal. Check
   Undo/Redo, discarded scene changes and reinstallation after editing the originals.
   Cover default avatars, missing Parameters references, and existing MA/VRCFury
   menu components without changing their original assets.
2. Confirm control parameters are unsaved/unsynchronized and the caption bus
   uses its selected 24/40/72-bit profile; upload with the
   official SDK, enable OSC and observe values on the correct Avatar.
3. Validate the implemented [host integration](host-integration.md) against the
   real client: menu On starts one configured task, Active reflects actual
   capture/backend readiness, and Off releases its capture and pending output.
4. Verify startup cancellation, late callbacks, failure, duplicate requests,
   desktop cancellation, Avatar change, reconnect and lost UDP updates. Confirm
   overlapping input requests are rejected without changing the existing task;
   independent sessions and another OSC application continue operating.
5. Add each caption preset, preview mixed-language text, move/reparent the object,
   then upload. Check local and remote text, clear, replacement, 24/40/72-bit
   profiles, default/custom FX, original expressions, Undo and removal. Confirm
   material colors and customized objects are preserved.
6. Record the compatible host version, check generated protocol consistency and
   package only tracked Unity source/assets plus the license. Publish a VPM
   listing only when its URL and checksum refer to a real release artifact.

[Caption architecture](presentation.md) records renderer and synchronization costs.
