#!/usr/bin/env python3
"""Copy shared Unity caption assets into their installable game packages."""

import argparse
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "presentation/unity"
PACKAGE = ROOT / "adapters/vrchat/packages/dev.nowloady.xrt-avatar"
# Only adapters with a concrete renderer integration receive these resources.
TARGETS = {
    SOURCE / "Runtime/Caption": PACKAGE / "Runtime/Caption",
    SOURCE / "Editor/Rendering": PACKAGE / "Editor/Caption/Rendering",
}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check without writing")
    args = parser.parse_args()
    stale = []
    for source, target in TARGETS.items():
        if not source.is_dir() or not source.with_suffix(".meta").is_file():
            raise SystemExit(f"Shared asset folder is incomplete: {source}")
        expected = {target.with_suffix(".meta"): source.with_suffix(".meta").read_bytes()}
        for path in sorted(source.rglob("*")):
            if path.is_file():
                expected[target / path.relative_to(source)] = path.read_bytes()
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
