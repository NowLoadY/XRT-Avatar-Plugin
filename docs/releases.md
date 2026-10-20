# Release packaging

Releases provide installable platform components with explicit XRTranslate
host requirements. The primary user entry is the root README and each platform's installation
guide. Players download only the files for their game; build tools are not player
prerequisites.

## Downloads

| File | Purpose |
|---|---|
| `XRT-Avatar-Plugin-VRChat-<version>.unitypackage` | Import the VRChat editor tool into an existing Avatar project |
| `XRT-Avatar-Plugin-VRChat-VCC-<version>.zip` | VPM package consumed by VCC; `package.json` is at the archive root |
| `XRT-Avatar-Plugin-ChilloutVR-<version>.unitypackage` | Import the CCK editor tool into an existing ChilloutVR project |
| Overte script URL in release notes | Load directly with Running Scripts → From URL |
| `XRT-Avatar-Plugin-Overte-<version>.zip` | Optional offline script and resources |
| `XRT-Avatar-Plugin-BigWalk-<bridge-version>.zip` | Import with a Mod manager into a BepInEx IL2CPP profile |
| `XRT-Resonite-win-x64.exe` / `XRT-Resonite-linux-x64` | Standalone text-selection window; includes its own Python runtime |
| `XRT-SecondLife-win-x64.exe` / `XRT-SecondLife-linux-x64` | Standalone local relay connector; includes Node.js, no Python |
| `XRT.lsl` | Second Life attachment script, paired through its touch menu |
| `XRT-TextDisplay.lsl` | Optional custom Second Life display script |

Only install packages are attached to GitHub releases. The builder writes them
to `dist/release/packages/`; VPM indexes, source manifests and checksum files go
to `dist/release/metadata/` and remain available in the Actions artifact. Publish
the VPM index at `vpm/index.json` and include checksums in the release notes.

`release/compatibility.json` owns the target host version, development revision
and setup guide. Host releases follow XRTranslate's own release cycle.
The Unity package's `package.json` owns its package version and SDK range. Release
tags use `v<package version>`. Connector-core crate versions follow their Rust API
lifecycle and remain independently pinned by the host.

## Build and publish

1. Update the Unity package version and release ZIP URL, compatibility metadata,
   download links and installation guides. Keep existing parameter and package
   identities stable. Check generated bindings with
   `python3 tools/generate_protocol.py --check`. The packager also checks the
   shared presentation binding and Unity windows with
   `tools/generate_presentation.py --check`, `tools/generate_unity_editor.py --check`
   and `tools/sync_unity_presentation.py --check`.
2. Commit the source and create its version tag. The release workflow builds
   Resonite and Second Life separately on Windows and Linux, and builds Big Walk
   with .NET 8. It passes those outputs to the platform packager. To check the
   Unity/Overte archives locally, use a clean checkout:

   ```sh
   python3 tools/build_release.py --version 0.1.0 \
     --output dist/release --require-clean
   ```

3. Push the tag and run **Build release packages** with that tag. The workflow
   verifies the checkout, builds the archives and attaches them to a draft
   release (marked prerelease only for a prerelease tag). The **Build game connectors** workflow can also be run independently
   to check executables before tagging. A rerun accepts identical assets; different bytes require a new
   version. Preserve published package bytes and stable package identities.
   When renaming an existing download, update its listing URL while retaining
   its checksum. Future package manifests use the product download filename.
4. Inspect the draft's files and checksums. Describe the package contents and link
   to the installation guides. Record archive checks and actual Unity/game testing
   in the platform compatibility documents. Publish when the downloads are ready.
5. Merge the generated `metadata/index.json` into `vpm/index.json` on `main`, retaining
   earlier versions. Publish the listing after its download URLs are available.
   The tool's `--listing-input` and `--listing-output` options support this update.
   It rejects an existing version whose metadata or ZIP hash changed.
   Store release provenance under `release/records/<tag>.json` when retaining it
   in the repository; metadata files do not belong in the player download list.

VCC uses this stable listing URL:

```text
https://raw.githubusercontent.com/NowLoadY/XRT-Avatar-Plugin/main/vpm/index.json
```

Package ZIP hashes belong in listing manifests, not inside the ZIP's own
package manifest.

## Packaging rules

The builder reads tracked files from explicit platform subtrees and checks
metadata, paths and GUID uniqueness. It includes licenses and the Overte setup
guide, shared text source, object designer and toolbar icons. It excludes local runtime data, reference repositories and third-party SDK binaries. ZIP timestamps
and Unity archive metadata are deterministic. Standalone executables also depend
on their native build environment; retain the original release artifacts.

`--companions <directory>` accepts exactly the four named executables above;
`--bigwalk <zip>` accepts the Mod manager package. No shared runtime directory is
collected. Python is embedded only by `adapters/resonite/build.py`. The Second Life
relay server uses Python on the operator's server and is not bundled in any player
download. Neither runtime is installed into XRTranslate or another game's package.

The offline VRChat tool lives under `Assets/XRT-Avatar-Plugin/VRChat`; its VPM
`legacyFolders` entry identifies that folder only. Avatar-generated assets and
restore records are outside it. Keep the same script GUIDs across the VPM and
Unity-package formats so migration preserves their references.

Archive validation checks packaging. Unity import, SDK compilation, installation,
removal and in-game behavior are recorded against the environments in which they
were actually exercised. Platform compatibility documents track acceptance status.

## Big Walk bridge

`python3 tools/build_bigwalk.py` builds the .NET bridge and writes one installable
ZIP under `dist/bigwalk/`. The release workflow attaches it to the same release as
the other game downloads; **Build Big Walk bridge** remains available for development.
The ZIP contains the plugin DLL, Thunderstore manifest, icon, installation guide
and license. Its manifest declares the BepInEx dependency, but local ZIP import
requires installing that dependency in the profile separately. BepInEx and game
assemblies remain in the game's installation. Thunderstore publication is separate
from preparing a compatible ZIP.
See the [adapter guide](adapters/bigwalk/development.md#build) for prerequisites.

References: [VPM packages](https://vcc.docs.vrchat.com/vpm/packages/),
[VPM repositories](https://vcc.docs.vrchat.com/vpm/repos/),
[official package release template](https://github.com/vrchat-community/template-package/blob/main/.github/workflows/release.yml).
