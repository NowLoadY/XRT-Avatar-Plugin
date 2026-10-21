# VRChat compatibility

The adapter provides a VPM package and a Unity asset package. Host requirements
are recorded in [`release/compatibility.json`](../../../release/compatibility.json).

## Verification status

Package checks cover archive contents, GUIDs, protocol bindings and download
hashes. On 2026-10-20, Unity **2022.3.22f1**, Avatar SDK **3.10.5** and Windows
**DX11** reproduced pink captions in **preview.7**. With the shared shader fix,
caption insertion, preview text and scene rendering worked on an existing avatar.
The same project verified the updated shared renderer: transparent presets,
Board background/border colors, SDF outlines, width-based wrapping, complete-line
clipping, empty-buffer hiding and uniform scaling. Preview.7 received local renderer/editor
patches; the current installer's other acceptance scenarios below, avatar upload
and game-client interaction remain unverified.
Linux/Proton, standalone headsets and mobile clients have no recorded runtime acceptance.

On 2026-10-21, the same Unity/SDK/DX11 project verified natural Board layout with
short, bilingual and full-buffer text, including narrow and wide surfaces.
Gesture Manager verified legacy direct writes, staged writes remaining invisible
until commit, complete-frame replacement and clearing both buffers. This checks
the plugin receiver and renderer, including static-only headers and footers.
Older hosts continue using direct writes. Multiplayer delivery has not been measured.

Left-hand holding and proximity-based two-hand alignment were verified in the
same Unity/SDK project on 2026-10-22, including saved scenes and reversible editing.
Multiplayer behavior remains unverified.

Local Test was checked in the same Unity/SDK project with Gesture Manager
**3.9.9** and XRTranslate's Windows debug builds dated **2026-10-20 18:47 / 21:09**
(backend **20:22**).
After fixing Windows commit exhaustion with a system-managed paging file and
reboot, both CUDA model servers started. A recorded ASR sample completed in
0.33 s. The real expression menu reached Ready → Translating → Ready. A typed
English sentence was translated to Chinese and reached the real Animator caption
cache through XRTranslate's OSC sender. A custom header was also checked visually
on the scene Board. Stop test closed the endpoint and exited Play mode; the saved
scene stayed unchanged. The 21:09 build also returned to Not connected without an
application error dialog after stopping the Unity test. Live microphone-to-caption
quality was not measured.
The preview.7 project also needed its legacy saved-scene build check skipped
during Play-mode preprocessing. This does not establish current-installer,
upload or multiplayer acceptance.

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
layout have an atlas-derived CPU proof; Unity rendering was checked as recorded above.
Follow the [official Unity version](https://creators.vrchat.com/sdk/upgrade/current-unity-version/)
and [SDK releases](https://creators.vrchat.com/releases/) when updating the target.

## Release acceptance

For each accepted configuration, record exact Unity, SDK, VRChat and XRTranslate
versions and verify the following:

1. Install on a copy of an Avatar, preserve original menu/parameters, reject
   collisions and retain a full root menu in a submenu. Add unrelated menu controls,
   parameters and animator layers after installation, then remove XRT without
   losing those edits. Check
   Undo/Redo, discarded scene changes and reinstallation after editing the originals.
   Cover default avatars, missing Parameters references, and existing MA/VRCFury
   menu components without changing their original assets.
   Duplicate an installed Avatar and use Save As with the original scene closed;
   add/remove objects and controls on either copy without changing the other's assets.
   Undo/Redo these edits and verify failed operations restore the previous references.
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
5. Add each caption preset and multiple text objects, preview mixed-language text,
   move/reparent each object and remove one while the others continue receiving,
   then upload. Check local and remote text, changed-chunk updates, independent
   message expiry and banners persisting after menu Off. Check 24/40/72-bit
   profiles, default/custom FX, original expressions, Undo and removal. Confirm
   material colors, replacement meshes, added components and children are preserved.
   Open a version-1 installation and verify its existing objects work without reinstallation.
   Rename, reparent and swap text-object names before uploading the copy; check
   that build-time bindings follow the correct renderers and leave source assets
   unchanged. Verify temporary build assets are removed after the build.
6. Record the compatible host version, check generated protocol consistency and
   package only tracked Unity source/assets plus the license. Publish a VPM
   listing only when its URL and checksum refer to a real release artifact.

[Caption architecture](presentation.md) records renderer and synchronization costs.
