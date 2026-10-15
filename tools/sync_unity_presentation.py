#!/usr/bin/env python3
"""Copy shared Unity caption assets into their installable game packages."""

import argparse
from pathlib import Path
import uuid


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "presentation/unity"
# Only adapters with a concrete renderer integration receive these resources.
PACKAGES = (
    ROOT / "adapters/vrchat/packages/dev.nowloady.xrt-avatar",
    ROOT / "adapters/chilloutvr/Assets/XRT-Avatar-Plugin",
)


def outputs():
    for package in PACKAGES:
        for source, target in (
            (SOURCE / "Runtime/Caption", package / "Runtime/Caption"),
            (SOURCE / "Editor/Rendering", package / "Editor/Caption/Rendering"),
        ):
            if not source.is_dir() or not source.with_suffix(".meta").is_file():
                raise SystemExit(f"Shared asset folder is incomplete: {source}")
            expected = {target.with_suffix(".meta"): source.with_suffix(".meta").read_bytes()}
            expected.update({target / path.relative_to(source): path.read_bytes()
                             for path in sorted(source.rglob("*")) if path.is_file()})
            yield target, expected
        target = package / "Editor/Caption/Authoring"
        guid = uuid.uuid5(uuid.NAMESPACE_URL, "xrt-avatar/shared-caption-authoring").hex
        expected = {target.with_suffix(".meta"): (
            f"fileFormatVersion: 2\nguid: {guid}\nfolderAsset: yes\nDefaultImporter:\n"
            "  externalObjects: {}\n  userData:\n  assetBundleName:\n  assetBundleVariant:\n").encode()}
        for name in ("CaptionAuthoring.cs", "AssetCopies.cs"):
            for suffix in ("", ".meta"):
                path = ROOT / ("authoring/unity/" + name + suffix)
                expected[target / path.name] = path.read_bytes()
        yield target, expected


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check without writing")
    args = parser.parse_args()
    stale = []
    for target, expected in outputs():
        for path, content in expected.items():
            if path.exists() and path.read_bytes() == content:
                continue
            if args.check:
                stale.append(str(path.relative_to(ROOT)))
            else:
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_bytes(content)
        extras = [path for path in target.rglob("*") if path.is_file() and path not in expected]
        if extras:
            # Never silently remove edits from a generated package copy.
            raise SystemExit("Unexpected shared package files: " + ", ".join(str(p) for p in extras))
    if stale:
        raise SystemExit("Run tools/sync_unity_presentation.py: " + ", ".join(stale))


if __name__ == "__main__":
    main()
