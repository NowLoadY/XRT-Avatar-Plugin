#!/usr/bin/env python3
"""ResoniteLink control and caption adapter for the XRT WebSocket connector."""

import argparse
import asyncio
import contextlib
import getpass
import json
from pathlib import Path
import sys
import time

from websockets.asyncio.client import connect

from link import Link, controls, field, install, loopback_url, STRING_TYPE, TEXT_TYPE

sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "connectors/websocket/python"))
from xrt_connector import Connector  # noqa: E402

sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "presentation/python"))
from bubble import PROFILES, CaptionSource  # noqa: E402


STATUS_LABELS = {
    "unavailable": "Disconnected", "off": "Off", "starting": "Starting…",
    "active": "On", "stopping": "Stopping…", "error": "Check XRTranslate",
}


class ResoniteAdapter:
    def __init__(self, link, components, text_id):
        self.link = link
        self.components = components
        self.text_id = text_id
        self.source_id = components.get("Text")
        self.last_source = None
        self.original_fields = {}
        self.applied_fields = {}
        self.captions = CaptionSource()
        self.requested = False
        self.stop_pending = False
        self.last_status = None
        self.status_label = None
        self.connector = None

    async def prepare(self):
        if self.source_id:
            component = await self.link.component(self.source_id)
            if (component.get("componentType") != STRING_TYPE
                    or component.get("members", {}).get("Value", {}).get("$type") != "string"):
                raise ValueError("The XRT Text source must be a string ValueField")
        if self.text_id:
            component = await self.link.component(self.text_id)
            if component.get("componentType") != TEXT_TYPE:
                raise ValueError("--text must identify a native TextRenderer component")
            members = component["members"]
            self.original_fields = {
                name: field(members[name]["$type"], members[name]["value"])
                for name in ("Text", "ParseRichText")
            }
            await self.write_text(Text=field("string", ""), ParseRichText=field("bool", False))
        await self.publish_text("")

    async def publish_text(self, text):
        if self.source_id and self.last_source != text:
            await self.link.write(self.source_id, Value=field("string", text))
            self.last_source = text
        if self.text_id and self.applied_fields.get("Text", {}).get("value") != text:
            await self.write_text(Text=field("string", text))

    async def write_text(self, **fields):
        await self.link.write(self.text_id, **fields)
        self.applied_fields.update(fields)

    async def reset(self, _message):
        self.requested = False
        self.stop_pending = False
        self.captions.reset()
        await self.link.write(self.components["Enabled"], Value=field("bool", False))
        await self.state({"status": "off"})
        await self.clear({})

    async def show_status(self):
        label = "Stopping…" if self.stop_pending else STATUS_LABELS.get(self.last_status, "Disconnected")
        if label != self.status_label:
            await self.link.write(self.components["Status"], Value=field("string", label))
            print("Translation:", label, flush=True)
            self.status_label = label

    async def state(self, message):
        status = message["status"]
        if status in ("off", "error", "unavailable"):
            self.stop_pending = False
        self.last_status = status
        await self.show_status()
        if status != "active":
            await self.clear({})

    async def caption(self, message):
        if not (self.source_id or self.text_id) or not self.requested or self.last_status != "active":
            return
        now = time.monotonic() * 1000
        if self.captions.accept(message, now):
            value = self.captions.snapshot(now)
            await self.publish_text(value["text"] if value else "")

    async def clear(self, _message):
        self.captions.clear()
        await self.publish_text("")

    async def poll(self):
        while True:
            if self.connector.ready:
                reset_sequence = self.connector.reset_sequence
                component = await self.link.component(self.components["Enabled"])
                enabled = component["members"]["Value"]["value"]
                if type(enabled) is not bool:
                    raise ValueError("Resonite Enabled field changed type")
                # A read started before reset cannot become a new start request.
                if (self.connector.ready and reset_sequence == self.connector.reset_sequence
                        and enabled != self.requested):
                    if enabled and (self.stop_pending or self.last_status == "stopping"):
                        await self.link.write(self.components["Enabled"], Value=field("bool", False))
                    else:
                        self.requested = enabled
                        if not enabled:
                            self.stop_pending = True
                            await self.clear({})
                            await self.show_status()
                        await self.connector.set_enabled(enabled)
            if self.captions.snapshot(time.monotonic() * 1000) is None:
                await self.publish_text("")
            await asyncio.sleep(0.25)

    async def finish(self):
        self.requested = False
        self.connector.ready = False
        closing = asyncio.create_task(self.connector.close())
        with contextlib.suppress(Exception):
            await self.link.write(self.components["Enabled"], Value=field("bool", False))
            await self.link.write(self.components["Status"], Value=field("string", "Disconnected"))
        if self.source_id and self.last_source is not None:
            with contextlib.suppress(Exception):
                current = (await self.link.component(self.source_id))["members"]["Value"]["value"]
                if current == self.last_source:
                    await self.link.write(self.source_id, Value=field("string", ""))
        if self.text_id and self.applied_fields:
            with contextlib.suppress(Exception):
                members = (await self.link.component(self.text_id))["members"]
                restore = {
                    name: self.original_fields[name]
                    for name, applied in self.applied_fields.items()
                    if members.get(name, {}).get("value") == applied["value"]
                }
                if restore:
                    await self.link.write(self.text_id, **restore)
        await closing


async def run(args):
    token = getpass.getpass("XRTranslate pairing token: ").strip() if args.command == "run" else None
    async with connect(loopback_url(args.link), max_size=4 * 1024 * 1024,
                       max_queue=8, open_timeout=5, close_timeout=2, proxy=None) as socket:
        link = Link(socket)
        if args.command == "inspect":
            print(json.dumps(await link.slot(args.slot, args.depth), indent=2, ensure_ascii=False))
        elif args.command == "install":
            profile = PROFILES[args.preset] if args.preset else None
            print(json.dumps(await install(link, args.parent, args.preset, args.template, profile),
                             indent=2, ensure_ascii=False))
        else:
            adapter = ResoniteAdapter(link, await controls(link, args.slot, ensure_text=True), args.text)
            adapter.connector = Connector(
                args.host, token, "resonite", on_reset=adapter.reset, on_state=adapter.state,
                on_caption=adapter.caption, on_clear=adapter.clear,
            )
            tasks = []
            try:
                await adapter.prepare()
                await adapter.connector.open()
                tasks = [asyncio.create_task(adapter.connector.run()), asyncio.create_task(adapter.poll())]
                done, _ = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
                for task in done:
                    task.result()
            finally:
                for task in tasks:
                    task.cancel()
                await asyncio.gather(*tasks, return_exceptions=True)
                await adapter.finish()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--link", required=True, help="Local ResoniteLink ws:// address")
    commands = parser.add_subparsers(dest="command", required=True)
    inspect = commands.add_parser("inspect", help="Inspect current scene IDs")
    inspect.add_argument("--slot", default="Root")
    inspect.add_argument("--depth", type=int, choices=range(4), default=1)
    setup = commands.add_parser("install", help="Create an owned XRT control subtree")
    setup.add_argument("--parent", required=True)
    setup.add_argument("--preset", choices=("horizontal", "vertical", "board"),
                       help="Create an editable caption object")
    setup.add_argument("--template", help="TextRenderer component with the font and material to reuse")
    bridge = commands.add_parser("run", help="Run the control and optional caption bridge")
    bridge.add_argument("--slot", required=True, help="Current XRT slot ID")
    bridge.add_argument("--host", required=True, help="Local XRTranslate ws:// address")
    bridge.add_argument("--text", help="TextRenderer component ID for synchronized captions")
    args = parser.parse_args()
    if args.command == "install" and bool(args.preset) != bool(args.template):
        parser.error("Use --preset and --template together to create a caption object")
    try:
        asyncio.run(run(args))
    except KeyboardInterrupt:
        pass
    except Exception as error:
        # Protocol/library exceptions may include endpoint data. Keep credentials out of logs.
        print("XRT bridge stopped:", type(error).__name__, file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
