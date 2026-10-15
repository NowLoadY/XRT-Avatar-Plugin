#!/usr/bin/env python3
"""Generate the shared Avatar Controls window for each Unity SDK package."""

import argparse
import json
from pathlib import Path
from string import Template

REPOSITORY_ROOT = Path(__file__).resolve().parents[1]
TEMPLATE = REPOSITORY_ROOT / "authoring/unity/AvatarControlsWindow.cs.template"


def outputs():
    template = Template(TEMPLATE.read_text(encoding="utf-8"))
    result = {}
    for profile in sorted((REPOSITORY_ROOT / "adapters").glob("*/unity-editor.json")):
        adapter = profile.parent.name
        binding = json.loads(profile.read_text(encoding="utf-8"))
        output = profile.parent / binding.pop("output")
        binding["window_title"] = "XRT · " + binding["platform"]
        binding["guide_url"] = (
            "https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/"
            + adapter + "/installation.md"
        )
        for key in ("platform", "window_title", "menu_path", "guide_url", "in_game"):
            binding[key] = json.dumps(binding[key], ensure_ascii=False)
        result[output] = template.substitute(binding)
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
