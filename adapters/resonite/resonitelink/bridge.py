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

from link import Link, controls, field, install, loopback_url

sys.path.insert(0, str(Path(__file__).resolve().parents[3] / "connectors/websocket/python"))
from xrt_connector import Connector  # noqa: E402


class ResoniteAdapter:
    def __init__(self, link, components, text_id):
        self.link = link
        self.components = components
        self.text_id = text_id
        self.original_text = None
        self.original_rich_text = None
        self.last_text = None
        self.caption_deadline = 0
        self.revisions = {}
        self.requested = False
        self.last_status = None
        self.connector = None

    async def prepare(self):
        if self.text_id:
            component = await self.link.component(self.text_id)
            if component.get("componentType") != "[FrooxEngine]FrooxEngine.TextRenderer":
                raise ValueError("--text must identify a native TextRenderer component")
            members = component["members"]
            self.original_text = members["Text"]["value"]
            self.original_rich_text = members["ParseRichText"]["value"]
            await self.link.write(self.text_id, ParseRichText=field("bool", False))
            await self.clear({})

    async def reset(self, _message):
        self.requested = False
        await self.link.write(self.components["Enabled"], Value=field("bool", False))
        await self.clear({})

    async def state(self, message):
        status = message["status"]
        if status != self.last_status:
            await self.link.write(self.components["Status"], Value=field("string", status))
            print("XRT:", status, flush=True)
            self.last_status = status

    async def caption(self, message):
        if not self.text_id or not self.requested:
            return
        key = (message["stream"], message["id"])
        revision = message["revision"]
        if revision <= self.revisions.get(key, -1):
            return
        self.revisions[key] = revision
        if len(self.revisions) > 64:
            del self.revisions[next(iter(self.revisions))]
        self.last_text = message["text"][:4096]
        self.caption_deadline = time.monotonic() + min(max(message["expires_ms"], 0), 15000) / 1000
        await self.link.write(self.text_id, Text=field("string", self.last_text))

    async def clear(self, _message):
        self.revisions.clear()
        self.caption_deadline = 0
        if self.text_id:
            self.last_text = ""
            await self.link.write(self.text_id, Text=field("string", ""))

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
                    self.requested = enabled
                    if not enabled:
                        await self.clear({})
                    await self.connector.set_enabled(enabled)
            if self.caption_deadline and time.monotonic() >= self.caption_deadline:
                await self.clear({})
            await asyncio.sleep(0.25)

    async def finish(self):
        self.requested = False
        self.connector.ready = False
        closing = asyncio.create_task(self.connector.close())
        with contextlib.suppress(Exception):
            await self.link.write(self.components["Enabled"], Value=field("bool", False))
            await self.link.write(self.components["Status"], Value=field("string", "unavailable"))
        if self.text_id and self.last_text is not None:
            with contextlib.suppress(Exception):
                members = (await self.link.component(self.text_id))["members"]
                restore = {}
                if members["Text"]["value"] == self.last_text:
                    restore["Text"] = field("string", self.original_text)
                if members["ParseRichText"]["value"] is False:
                    restore["ParseRichText"] = field("bool", self.original_rich_text)
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
            print(json.dumps(await install(link, args.parent), indent=2, ensure_ascii=False))
        else:
            adapter = ResoniteAdapter(link, await controls(link, args.slot), args.text)
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
    bridge = commands.add_parser("run", help="Run the control and optional caption bridge")
    bridge.add_argument("--slot", required=True, help="Current XRT slot ID")
    bridge.add_argument("--host", required=True, help="Local XRTranslate ws:// address")
    bridge.add_argument("--text", help="TextRenderer component ID for synchronized captions")
    args = parser.parse_args()
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
