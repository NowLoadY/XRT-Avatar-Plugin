#!/usr/bin/env python3
"""Build only Resonite's standalone connector; never modify the host runtime."""
import argparse
import os
from pathlib import Path
import platform
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--output", type=Path, default=ROOT / "dist/resonite")
    args = parser.parse_args()
    system = "win" if sys.platform == "win32" else "linux" if sys.platform.startswith("linux") else None
    if system is None or platform.machine().lower() not in ("amd64", "x86_64"):
        parser.error("Build on Windows x64 or Linux x64")
    name = "XRT-Resonite-" + system + "-x64"
    command = [sys.executable, "-m", "PyInstaller", "--noconfirm", "--clean", "--onefile",
               "--name", name, "--paths", str(ROOT),
               "--distpath", str(args.output.resolve()),
               "--workpath", str(ROOT / "target/resonite/build"),
               "--specpath", str(ROOT / "target/resonite")]
    if system == "win":
        command.append("--windowed")
    for source in ("presentation/bubble-style.json", "presentation/presets.json", "LICENSE"):
        destination = str(Path(source).parent)
        command += ["--add-data", str(ROOT / source) + os.pathsep + destination]
    command.append(str(ROOT / "adapters/resonite/resonitelink/launcher.py"))
    subprocess.run(command, cwd=ROOT, check=True)


if __name__ == "__main__":
    main()
