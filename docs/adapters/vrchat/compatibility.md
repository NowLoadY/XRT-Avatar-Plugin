# VRChat compatibility

This source preview implements protocol version 1 in the Avatar package, Rust
control core and companion XRTranslate host source. Use a host build containing
**Avatar translation controls** in Settings → Plugins. No completed Unity/VRChat
end-to-end validation or packaged VCC listing is claimed by this source delivery.

The paired host source is XRTranslate commit
[`401766d`](https://github.com/NowLoadY/XRTranslate/commit/401766d02499217a8b9a83c66465eeca589f6f2c),
available in [integration PR #24](https://github.com/NowLoadY/XRTranslate/pull/24).
Build that revision for this preview; an older main branch or release without
the Avatar settings entry does not provide this integration. Its Linux client
library test target compiles, and eight focused checks pass for loopback
OSCQuery/UDP, binding and reset lifecycle, startup cancellation and TTS isolation.

| Component | Current status |
|---|---|
| Protocol and generated bindings | Defined in source |
| Rust control core | Implemented; no transport or inference dependencies |
| Unity editor installer | Source implementation; Unity import/compilation not verified |
| XRTranslate host adapter | Implemented in `plugins::avatar` and `avatar_host` |
| Shared OSCQuery transport | Implemented in `plugins::osc::transport` |
| In-game enable/disable | Runtime path implemented; live VRChat validation outstanding |
| Custom floating Avatar bubble | Presentation boundary only; no renderer |

## Targets

The initial integration target is VRChat on Windows with XRTranslate running on
the same PC, in desktop or PCVR mode. Linux source support and local transport
checks do not establish VRChat/Proton compatibility. Standalone headsets, remote
audio acquisition and mobile clients are not validated integration targets.

The editor source targets Unity 2022.3.22f1 and the Avatar SDK 3.10.x API family.
These are target versions, not a tested compatibility claim. Confirm actual
SDK API and package constraints in an existing authoring environment before
publishing. Follow the [official Unity version](https://creators.vrchat.com/sdk/upgrade/current-unity-version/)
and [SDK releases](https://creators.vrchat.com/releases/) when updating the target.
No Unity installation is performed by this project or its source checks.

## Release acceptance

Before declaring a runnable release, record exact Unity, SDK, VRChat and XRTranslate
versions and verify the following in that environment:

1. Install on a copy of an Avatar, preserve original menu/parameters, reject
   collisions and a full root menu, and restore an unchanged installation. An
   edited installation must produce a diagnostic without destructive removal.
2. Confirm generated parameters are unsaved/unsynchronized; upload with the
   official SDK, enable OSC and observe values on the correct Avatar.
3. Validate the implemented [host integration](host-integration.md) against the
   real client: menu On starts one configured task, Active reflects actual
   capture/backend readiness, and Off releases its capture and pending output.
4. Verify startup cancellation, late callbacks, failure, duplicate requests,
   desktop cancellation, Avatar change, reconnect and lost UDP updates. Confirm
   overlapping input requests are rejected without changing the existing task;
   independent sessions and another OSC application continue operating.
5. Record the compatible host version, check generated protocol consistency and
   package only tracked Unity source/assets plus the license. Publish a VPM
   listing only when its URL and checksum refer to a real release artifact.

Source implementation and local checks do not substitute for these checks.
Future bubble rendering has its own [presentation boundary](presentation.md).
