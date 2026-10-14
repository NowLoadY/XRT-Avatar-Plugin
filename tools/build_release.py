#!/usr/bin/env python3
"""Build deterministic, allowlisted install packages using Python's standard library."""

from __future__ import annotations

import argparse
import gzip
import hashlib
import io
import json
from pathlib import Path, PurePosixPath
import re
import subprocess
import tarfile
import zipfile


ROOT = Path(__file__).resolve().parents[1]
REPOSITORY = "NowLoadY/XRT-Avatar-Plugin"
PACKAGE = "dev.nowloady.xrt-avatar"
VRC_ROOT = f"adapters/vrchat/packages/{PACKAGE}"
CVR_ROOT = "adapters/chilloutvr/Assets"
LISTING_URL = f"https://raw.githubusercontent.com/{REPOSITORY}/main/vpm/index.json"
VRC_ASSETS = "Assets/XRT-Avatar-Plugin/VRChat"
# These identities are stable across releases and installation formats.
VRC_FOLDER_GUID = "f481224d1ee34d72a4c4f24f7c44e91f"
CVR_LICENSE_GUID = "58986b9148664c2f8874d2d255d6290a"
UNITY_SUFFIXES = {".cs", ".asmdef", ".json", ".md", ".txt", ".meta", ".asset", ".prefab", ".mat", ".shader", ".png", ".svg"}
SEMVER = re.compile(r"(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?\Z")
GUID = re.compile(rb"^guid: ([0-9a-f]{32})\s*$", re.MULTILINE)


def git(*args: str) -> bytes:
    return subprocess.check_output(["git", *args], cwd=ROOT)


def json_bytes(value: object) -> bytes:
    return (json.dumps(value, indent=2, ensure_ascii=False) + "\n").encode("utf-8")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def tracked_files() -> set[str]:
    return {name.decode("utf-8") for name in git("ls-files", "-z").split(b"\0") if name}


def read_source(name: str, tracked: set[str]) -> bytes:
    if name not in tracked:
        raise ValueError(f"Release input is not tracked: {name}")
    path = ROOT / name
    if path.is_symlink() or not path.is_file() or not path.resolve().is_relative_to(ROOT):
        raise ValueError(f"Release input must be a regular repository file: {name}")
    if path.stat().st_size > 16 * 1024 * 1024:
        raise ValueError(f"Unexpectedly large release input: {name}")
    return path.read_bytes()


def unity_sources(prefix: str, tracked: set[str]) -> dict[str, bytes]:
    untracked = git("ls-files", "--others", "--exclude-standard", "-z", "--", prefix)
    if untracked:
        raise ValueError(f"Stage the intended Unity package inputs before packaging: {prefix}")
    files = {}
    for name in sorted(tracked):
        if not name.startswith(prefix + "/"):
            continue
        relative = name[len(prefix) + 1:]
        path = PurePosixPath(relative)
        if path.suffix not in UNITY_SUFFIXES and path.name != "LICENSE":
            raise ValueError(f"Unexpected Unity package input: {name}")
        if any(part.startswith(".") for part in path.parts):
            raise ValueError(f"Hidden package input: {name}")
        files[relative] = read_source(name, tracked)
    if not files:
        raise ValueError(f"No tracked Unity sources under {prefix}")
    validate_unity_metadata(files)
    return files


def validate_unity_metadata(files: dict[str, bytes]) -> None:
    guids = set()
    for name, data in files.items():
        if name.endswith(".meta"):
            matches = GUID.findall(data)
            if len(matches) != 1 or matches[0] in guids:
                raise ValueError(f"Missing, invalid or repeated Unity GUID: {name}")
            guids.add(matches[0])
            asset = name[:-5]
            is_folder = b"folderAsset: yes" in data
            if is_folder:
                if not any(item.startswith(asset + "/") for item in files):
                    raise ValueError(f"Folder metadata has no packaged descendants: {name}")
            elif asset not in files:
                raise ValueError(f"Metadata has no packaged asset: {name}")
        elif name + ".meta" not in files:
            raise ValueError(f"Unity asset has no metadata: {name}")
        parent = PurePosixPath(name).parent
        while str(parent) != ".":
            if str(parent) + ".meta" not in files:
                raise ValueError(f"Unity folder has no metadata: {parent}")
            parent = parent.parent


def folder_meta(guid: str) -> bytes:
    return (f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n"
            "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()


def text_meta(guid: str) -> bytes:
    return (f"fileFormatVersion: 2\nguid: {guid}\nTextScriptImporter:\n"
            "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()


def write_zip(path: Path, files: dict[str, bytes]) -> None:
    with zipfile.ZipFile(path, "w", compression=zipfile.ZIP_DEFLATED, compresslevel=9) as archive:
        for name, data in sorted(files.items()):
            info = zipfile.ZipInfo(name, date_time=(1980, 1, 1, 0, 0, 0))
            info.compress_type = zipfile.ZIP_DEFLATED
            info.create_system = 3
            info.external_attr = 0o100644 << 16
            archive.writestr(info, data)


def write_unitypackage(path: Path, files: dict[str, bytes]) -> None:
    # A custom asset package stores each asset, its original .meta and import
    # pathname together under the asset GUID. Folder entries contain no asset.
    validate_unity_metadata({name.removeprefix("Assets/"): data for name, data in files.items()})
    with path.open("wb") as output, gzip.GzipFile(filename="", mode="wb", fileobj=output, mtime=0) as compressed:
        with tarfile.open(fileobj=compressed, mode="w", format=tarfile.USTAR_FORMAT) as archive:
            for name, metadata in sorted(files.items()):
                if not name.endswith(".meta"):
                    continue
                asset = name[:-5]
                guid = GUID.search(metadata)[1].decode("ascii")
                members = {"asset.meta": metadata, "pathname": asset.encode("utf-8")}
                if asset in files:
                    members["asset"] = files[asset]
                for member, data in sorted(members.items()):
                    info = tarfile.TarInfo(f"{guid}/{member}")
                    info.size, info.mode, info.mtime = len(data), 0o644, 0
                    archive.addfile(info, io.BytesIO(data))


def make_listing(manifest: dict, archive: Path, listing_input: Path) -> dict:
    if listing_input.exists():
        listing = json.loads(listing_input.read_text("utf-8"))
        if listing.get("id") != "dev.nowloady.xrt-avatar.repository" or listing.get("url") != LISTING_URL:
            raise ValueError("Listing identity does not match this repository")
    else:
        listing = {"name": "XRT-Avatar-Plugin", "id": "dev.nowloady.xrt-avatar.repository",
                   "url": LISTING_URL, "author": "NowLoadY", "packages": {}}
    published_manifest = dict(manifest, zipSHA256=sha256(archive))
    versions = listing.setdefault("packages", {}).setdefault(PACKAGE, {"versions": {}})["versions"]
    existing = versions.get(manifest["version"])
    if existing is not None and existing != published_manifest:
        raise ValueError("This version is already listed with different contents; release a new version")
    versions[manifest["version"]] = published_manifest
    return listing


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--version", required=True, help="Package SemVer; release tag is v<version>")
    parser.add_argument("--output", type=Path, default=ROOT / "dist/release")
    parser.add_argument("--listing-input", type=Path, default=ROOT / "vpm/index.json")
    parser.add_argument("--listing-output", type=Path, help="Also write the merged listing here after publishing the release")
    parser.add_argument("--require-clean", action="store_true", help="Reject any uncommitted source before release packaging")
    args = parser.parse_args()
    if not SEMVER.fullmatch(args.version):
        parser.error("--version must be a SemVer without a v prefix or build metadata")
    prerelease = args.version.partition("-")[2]
    if any(part.isdecimal() and len(part) > 1 and part.startswith("0") for part in prerelease.split(".")):
        parser.error("Numeric SemVer prerelease identifiers must not have leading zeros")
    dirty = bool(git("status", "--porcelain", "--untracked-files=normal").strip())
    if args.require_clean and dirty:
        parser.error("The source checkout must be clean for release packaging")
    tracked = tracked_files()
    compatibility = json.loads(read_source("release/compatibility.json", tracked))
    host = compatibility["host"]
    if host.get("name") != "XRTranslate" or not SEMVER.fullmatch(host.get("minimum_version", "")):
        parser.error("Invalid minimum host version in release/compatibility.json")
    if not re.fullmatch(r"[0-9a-f]{40}", host.get("development_revision", "")):
        parser.error("Host development_revision must be a full Git commit ID")
    if host.get("documentation_url") != "https://github.com/NowLoadY/XRTranslate/blob/main/docs/avatar-connectors.md":
        parser.error("Host documentation_url must point to the Avatar connectors guide")
    vrc = unity_sources(VRC_ROOT, tracked)
    manifest = json.loads(vrc["package.json"])
    vpm_name = f"XRT-Avatar-Plugin-VRChat-VCC-{args.version}.zip"
    url = f"https://github.com/{REPOSITORY}/releases/download/v{args.version}/{vpm_name}"
    if manifest.get("name") != PACKAGE or manifest.get("version") != args.version:
        parser.error("Package name/version differs from the requested release")
    if manifest.get("url") != url:
        parser.error(f"package.json url must be {url}")
    if manifest.get("legacyFolders") != {VRC_ASSETS.replace("/", "\\"): VRC_FOLDER_GUID}:
        parser.error("legacyFolders must identify only the VRChat installer folder and its stable GUID")
    if "zipSHA256" in manifest:
        parser.error("zipSHA256 belongs in the VPM listing, not package.json")
    packages = args.output / "packages"
    metadata = args.output / "metadata"
    packages.mkdir(parents=True, exist_ok=True)
    metadata.mkdir(parents=True, exist_ok=True)
    paths = []

    def artifact(name: str) -> Path:
        result = packages / name
        paths.append(result)
        return result

    vpm_archive = artifact(vpm_name)
    write_zip(vpm_archive, vrc)
    shared_folder = read_source(f"{CVR_ROOT}/XRT-Avatar-Plugin.meta", tracked)
    vrc_assets = {f"{VRC_ASSETS}/{name}": data for name, data in vrc.items()
                  if name not in {"package.json", "package.json.meta"}}
    vrc_assets["Assets/XRT-Avatar-Plugin.meta"] = shared_folder
    vrc_assets[VRC_ASSETS + ".meta"] = folder_meta(VRC_FOLDER_GUID)
    write_unitypackage(artifact(f"XRT-Avatar-Plugin-VRChat-{args.version}.unitypackage"), vrc_assets)

    cvr_assets = {f"Assets/{name}": data for name, data in unity_sources(CVR_ROOT, tracked).items()}
    cvr_license = "Assets/XRT-Avatar-Plugin/Editor/XRT-ChilloutVR-LICENSE.txt"
    cvr_assets[cvr_license] = read_source("LICENSE", tracked)
    cvr_assets[cvr_license + ".meta"] = text_meta(CVR_LICENSE_GUID)
    write_unitypackage(artifact(f"XRT-Avatar-Plugin-ChilloutVR-{args.version}.unitypackage"), cvr_assets)

    overte = {name: read_source(name, tracked) for name in (
        "adapters/overte/scripts/xrt-avatar.js", "connectors/websocket/javascript/xrt_connector.js", "LICENSE")}
    overte["START-HERE.md"] = read_source("docs/adapters/overte/installation.md", tracked)
    write_zip(artifact(f"XRT-Avatar-Plugin-Overte-{args.version}.zip"), overte)

    listing = json_bytes(make_listing(manifest, vpm_archive, args.listing_input))
    (metadata / "index.json").write_bytes(listing)
    package_manifest = {"version": args.version, "tag": "v" + args.version,
                        "source_commit": git("rev-parse", "HEAD").decode().strip(),
                        "source_tree_dirty": dirty,
                        "host": host,
                        "connector_protocol": compatibility["connector_protocol"],
                        "artifacts": [{"name": path.name, "sha256": sha256(path), "bytes": path.stat().st_size}
                                      for path in paths]}
    (metadata / "release-manifest.json").write_bytes(json_bytes(package_manifest))
    (metadata / "SHA256SUMS").write_text("".join(f"{sha256(path)}  {path.name}\n" for path in paths), "utf-8")
    if args.listing_output:
        args.listing_output.parent.mkdir(parents=True, exist_ok=True)
        args.listing_output.write_bytes(listing)
    print(f"Built {len(paths)} install packages in {packages}; release metadata in {metadata}")
    print("Archive structure and GUIDs checked. Unity and game acceptance are separate checks.")


if __name__ == "__main__":
    try:
        main()
    except (ValueError, OSError, subprocess.CalledProcessError) as error:
        raise SystemExit(str(error)) from error
