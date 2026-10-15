#!/usr/bin/env python3
"""Generate shared authoring windows for each Unity SDK package."""

import argparse
import json
from pathlib import Path
from string import Template

REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
TEMPLATES = REPOSITORY_ROOT / "authoring/unity"


def outputs():
    result = {}
    for profile in sorted((REPOSITORY_ROOT / "adapters").glob("*/unity-editor.json")):
        adapter = profile.parent.name
        binding = json.loads(profile.read_text(encoding="utf-8"))
        captions = binding.pop("captions")
        binding["window_title"] = "XRT · " + binding["platform"]
        binding["guide_url"] = (
            "https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/"
            + adapter + "/installation.md"
        )
        for name, values in (("AvatarControlsWindow", binding),
                             ("CaptionObjectsWindow", binding | captions)):
            values = values.copy()
            output = profile.parent / values.pop("output")
            for key in ("platform", "window_title", "menu_path", "guide_url", "in_game"):
                values[key] = json.dumps(values[key], ensure_ascii=False)
            template = Template((TEMPLATES / (name + ".cs.template")).read_text(encoding="utf-8"))
            result[output] = template.substitute(values)
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--check", action="store_true", help="Check without writing")
    args = parser.parse_args()
    stale = []
    for path, content in outputs().items():
        if args.check:
            if not path.exists() or path.read_text(encoding="utf-8") != content:
                stale.append(str(path.relative_to(REPOSITORY_ROOT)))
        else:
            path.write_text(content, encoding="utf-8")
    if stale:
        raise SystemExit("Regenerate Unity editor windows: " + ", ".join(stale))


if __name__ == "__main__":
    main()
