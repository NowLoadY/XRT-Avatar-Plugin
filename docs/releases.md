# Release packaging

Releases provide installable platform components with explicit XRTranslate
host requirements. The primary user entry is the root README and each platform's installation
guide. Source archives are for development.

## Downloads

| File | Purpose |
|---|---|
| `XRT-Avatar-Plugin-VRChat-<version>.unitypackage` | Import the VRChat editor tool into an existing Avatar project |
| `XRT-Avatar-Plugin-VRChat-VCC-<version>.zip` | VPM package consumed by VCC; `package.json` is at the archive root |
| `XRT-Avatar-Plugin-ChilloutVR-<version>.unitypackage` | Import the CCK editor tool into an existing ChilloutVR project |
| `XRT-Avatar-Plugin-Overte-<version>.zip` | Extract and load the included game script; keep the shared client beside it |

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
   `python3 tools/generate_protocol.py --check`.
2. Commit the source and create its version tag. Use a clean checkout to build:

   ```sh
   python3 tools/build_release.py --version 0.1.0-preview.1 \
     --output dist/release --require-clean
   ```

3. Push the tag and run **Build release packages** with that tag. The workflow
   verifies the checkout, builds the archives and attaches them to a draft
   prerelease. A rerun accepts identical assets; different bytes require a new
   version. Preserve published package bytes and stable package identities.
   When renaming an existing download, update its listing URL while retaining
   its checksum. Future package manifests use the product download filename.
4. Inspect the draft's files and checksums. State whether the compatible host
   is a released version or requires a development build. Record archive checks
   and actual Unity/game testing separately in the release notes. Publish the
   preview when the plugin downloads and stated compatibility are ready.
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

VCC hides prerelease packages by default; the installation guide explains how to
show them. Package ZIP hashes belong in listing manifests, not inside the ZIP's
own package manifest.

## Packaging rules

The builder reads tracked files from explicit platform subtrees and checks
metadata, paths and GUID uniqueness. It includes licenses and the Overte setup
guide. It excludes local runtime data and third-party SDK binaries. ZIP timestamps
and Unity archive metadata are deterministic so the same inputs produce identical
install packages.

The offline VRChat tool lives under `Assets/XRT-Avatar-Plugin/VRChat`; its VPM
`legacyFolders` entry identifies that folder only. Avatar-generated assets and
restore records are outside it. Keep the same script GUIDs across the VPM and
Unity-package formats so migration preserves their references.

Archive validation checks packaging. Unity import, SDK compilation, installation,
removal and in-game behavior are recorded against the environments in which they
were actually exercised. A preview must describe outstanding acceptance work.

References: [VPM packages](https://vcc.docs.vrchat.com/vpm/packages/),
[VPM repositories](https://vcc.docs.vrchat.com/vpm/repos/),
[official package release template](https://github.com/vrchat-community/template-package/blob/main/.github/workflows/release.yml).
