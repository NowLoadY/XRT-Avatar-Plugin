# VRChat installation and removal

This guide uses the editor package with the compatible XRTranslate host source.
Use an existing VRChat Avatar authoring environment; this guide does not install
Unity. The editor source and in-game interaction have not yet been validated in
Unity/VRChat. The desktop host adapter and OSCQuery transport are implemented in
XRTranslate; select a build containing **Settings → Plugins → Avatar translation
controls** from the [paired source revision](compatibility.md). Installing Avatar
assets does not install or launch XRTranslate.

## Load the editor package

1. Use an existing VCC Avatar project with its supported Unity and Avatar SDK.
   Consult the target versions in [compatibility](compatibility.md).
2. Clone this repository outside the Unity project. In VCC settings, add
   `adapters/vrchat/packages/dev.nowloady.xrt-avatar` as a **User Package**, then add it to the
   Avatar project. There is no hosted VCC listing in this source preview.
3. Open the existing project. Treat any import/compile error as an unverified
   compatibility issue; do not proceed to uploading an Avatar with broken scripts.

The package is Editor-only and uses the official Avatar SDK types. It does not
include SDK binaries, add runtime scripts to the Avatar, require Modular Avatar,
modify the FX controller or create meshes/materials.

Official packaging workflow: [VPM User Packages](https://vcc.docs.vrchat.com/vpm/packages/).

## Generate Avatar controls

1. Work on a copy of the Avatar in a saved scene. The preview requires Custom
   Expressions already enabled with an assigned, saved menu and parameter asset.
   For a default Avatar, first set up the base expressions you want with the
   official SDK; the installer does not replace implicit defaults with XRT-only
   controls. Select the Avatar root and open
   `Tools → XRT-Avatar-Plugin → Avatar Controls`.
2. Confirm the `Avatar` field points to that scene's descriptor. Direct editing
   of a persistent prefab asset is rejected; use a scene instance.
3. Choose `Install / Check Installation`. The installer clones the current
   root Expressions Menu and Expression Parameters, creates the XRT submenu,
   and points this descriptor at those generated copies. Original assets remain
   unchanged. Existing nested menus remain referenced, not rewritten.
4. Save the scene. Keep the installation record with the generated assets; it
   stores the original references needed for restoration.

Installation requires one free entry in the root menu and three unused parameter
names. A full menu or name collision is reported before changing the descriptor.
Organize the original menu manually if necessary; the installer does not silently
rearrange unrelated controls. Repeated installation of an intact record is a check,
not a second copy. A modified installation requires review before replacement.

## Connect to XRTranslate

1. In a compatible XRTranslate build, complete setup and choose the languages,
   microphone/system audio input and recognition settings. Configure the existing
   Chatbox, overlay and TTS outputs you want to use.
2. After checking the editor assets in your authoring project, upload the Avatar
   using the official SDK, select it in VRChat and enable VRChat OSC.
3. In XRTranslate, enable **Settings → Plugins → Avatar translation controls**.
   The shared OSC transport discovers the local VRChat endpoint and uses an
   advertised available receive port. It does not require another listener on 9001.
4. Wait for the Avatar settings status to report **Off** after synchronization.
   Open `Expressions → XRT → Translation` to request startup. **Active** means
   both capture and the backend session are ready; the menu tick alone is a request.
5. Turn the menu toggle off to stop its session. **Stopping** remains until owned
   work is released. Changing Avatar, losing the connection, disabling the plugin
   or using the desktop Stop control also ends that session. Reconnect starts Off.

Avatar control does not enable Chatbox output or TTS for you. OSC Chatbox output
retains its existing target/settings, and disabling that output does not disable
Avatar control. If another live task already uses the selected input, the Avatar
request is rejected with a diagnostic; that task continues. Separate inputs and
file/text tasks remain available. Change the source/language configuration before
starting a new Avatar session; see [host integration](host-integration.md) for
which settings remain live.

The menu and parameters travel with the uploaded Avatar across worlds. Every
Avatar requiring this control must be prepared separately; world plugins are not
required. Changing to an Avatar without the contract cancels the owned session.

## Restore and remove

Use `Restore Original Expressions` on the same saved-scene Avatar. Restoration
requires the descriptor and generated asset contents still to match the recorded
installation. If they were edited, the installer reports the conflict and leaves
them intact for manual merging. It must not overwrite author changes.

Restoration returns the descriptor to its original references. The record is
retained and installation state is inferred from those references, so an unsaved
scene cannot strand an installation. Generated assets are retained, because another
Avatar or asset may reference them. Remove that generated folder manually only
after confirming it is no longer referenced. Save the scene and re-upload if
removing controls from an already uploaded Avatar.

Reinstalling after restoration reuses the existing generated assets only if both
they and the original source assets remain unchanged. If the originals were
edited, preserve those edits and merge or regenerate the control assets manually.

Remove the editor package from the project only after restoring the desired
Avatars; otherwise its record type and restore command will be unavailable.
No desktop model, language setting or translation history is removed.

## Runtime diagnosis

| Symptom | Check |
|---|---|
| Menu exists but nothing starts | Use a build with Avatar translation controls, enable the plugin, and check its connection/status message |
| Connection unavailable | Enable VRChat OSC, use the installed Avatar, check version/parameter discovery and local endpoint |
| On returns to off | Read the host error: missing configuration, input already owned, cancellation still pending or connection reset |
| Works alone but not with another OSC app | Check local OSCQuery discovery and endpoint diagnostics; do not launch another fixed-port listener or replace another app's mappings |
| Custom bubble absent | No bubble renderer ships in this control preview |

For stale VRChat-generated OSC parameter configuration, use the official OSC
configuration/debug workflow; do not overwrite another tool's mappings blindly.
See [OSC Avatar Parameters](https://docs.vrchat.com/docs/osc-avatar-parameters).
