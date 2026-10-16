#!/usr/bin/env python3
"""Build the Big Walk bridge and its installable package."""
import argparse
import os
from pathlib import Path
import subprocess
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--dotnet", default="dotnet")
    parser.add_argument("--bepinex-core", type=Path)
    parser.add_argument("--output", type=Path, default=ROOT / "dist/bigwalk")
    args = parser.parse_args()
    project = ROOT / "adapters/bigwalk/XRT.BigWalk.csproj"
    version = ET.parse(project).findtext("PropertyGroup/Version")
    build = ROOT / "target/bigwalk"
    command = [args.dotnet, "build", str(project), "-c", "Release", "--nologo", "-o", str(build)]
    if args.bepinex_core:
        command.append(f"-p:BepInExCore={args.bepinex_core.resolve()}")
    subprocess.run(command, check=True, cwd=ROOT,
                   env={**os.environ, "DOTNET_CLI_TELEMETRY_OPTOUT": "1"})
    args.output.mkdir(parents=True, exist_ok=True)
    archive = args.output / f"XRT-Avatar-Plugin-BigWalk-{version}.zip"
    files = {
        "BepInEx/plugins/XRT.BigWalk/XRT.BigWalk.dll": build / "XRT.BigWalk.dll",
        "LICENSE": ROOT / "LICENSE",
        "README.md": ROOT / "docs/adapters/bigwalk/installation.md",
    }
    with zipfile.ZipFile(archive, "w", compression=zipfile.ZIP_DEFLATED) as package:
        for name, source in sorted(files.items()):
            entry = zipfile.ZipInfo(name, date_time=(2026, 1, 1, 0, 0, 0))
            entry.compress_type = zipfile.ZIP_DEFLATED
            entry.external_attr = 0o100644 << 16
            package.writestr(entry, source.read_bytes())
    print(archive)


if __name__ == "__main__":
    main()
