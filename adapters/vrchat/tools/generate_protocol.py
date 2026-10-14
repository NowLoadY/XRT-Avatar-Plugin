#!/usr/bin/env python3
"""Compatibility entry point for the shared protocol generator."""

import runpy
from pathlib import Path

if __name__ == "__main__":
    runpy.run_path(str(Path(__file__).resolve().parents[3] / "tools/generate_protocol.py"), run_name="__main__")
