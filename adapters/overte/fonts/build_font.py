#!/usr/bin/env python3
"""Build Overte's native MTSDF font with msdf-atlas-gen 1.4 and the shared Noto source."""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / "tools"))
from build_caption_font import SOURCE, SOURCE_SHA256, repertoire


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--font", required=True, type=Path)
    parser.add_argument("--generator", required=True, type=Path)
    args = parser.parse_args()
    if hashlib.sha256(args.font.read_bytes()).hexdigest() != SOURCE_SHA256:
        parser.error("Use the pinned Noto Sans CJK SC Regular 2.004 source")
    from fontTools.ttLib import TTFont
    font = TTFont(args.font)
    points = sorted(point for point in repertoire() & font.getBestCmap().keys() if point >= 0x20)
    output = Path(__file__).resolve().parent
    with tempfile.TemporaryDirectory() as directory:
        work = Path(directory)
        charset = work / "charset.txt"
        charset.write_text("\n".join(f"0x{point:04X}" for point in points) + "\n")
        subprocess.run([
            str(args.generator.resolve()), "-font", str(args.font.resolve()),
            "-charset", str(charset), "-type", "mtsdf", "-format", "png",
            "-size", "24", "-pxrange", "4", "-square4", "-threads", "4",
            "-overlap", "-scanline", "-arfont", str(output / "XrtNotoCaption.arfont"),
            "-json", str(work / "metrics.json"),
        ], check=True)
        metrics = json.loads((work / "metrics.json").read_text())
    atlas = metrics["atlas"]
    actual = sorted(glyph["unicode"] for glyph in metrics["glyphs"])
    if actual != points:
        raise SystemExit("Generated font coverage differs from the selected repertoire")
    resource = output / "XrtNotoCaption.arfont"
    manifest = {
        "font": "Noto Sans CJK SC Regular", "fontVersion": "2.004",
        "source": SOURCE, "sourceSha256": SOURCE_SHA256,
        "license": "OFL-1.1", "licenseFile": "OFL.txt",
        "generator": "https://github.com/Chlumsky/msdf-atlas-gen/tree/2ede254314a2512252a225fa6c975948d6af559a",
        "format": "Artery Font with PNG MTSDF", "atlas": atlas,
        "glyphCount": len(actual), "gpuBytes": atlas["width"] * atlas["height"] * 4,
        "resource": resource.name, "sha256": hashlib.sha256(resource.read_bytes()).hexdigest(),
        "coverage": ["Latin, Greek and Cyrillic present in Noto CJK", "GB2312", "Shift_JIS", "EUC-KR"],
        "codepoints": [f"{point:04X}" for point in actual],
    }
    (output / "font.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n")
    shutil.copyfile(ROOT / "presentation/unity/Runtime/Caption/OFL.txt", output / "OFL.txt")
    print(f"{len(actual)} glyphs; {atlas['width']} x {atlas['height']}; {resource.stat().st_size:,} bytes")


if __name__ == "__main__":
    main()
