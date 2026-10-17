"""Shared XRT WebSocket v1 client. Platform callbacks own UI and rendering."""

import asyncio
import json
import re
import time
import uuid
from urllib.parse import urlsplit

from websockets.asyncio.client import connect


def parse_pairing(text, connector):
    """Read the host's clipboard payload without changing the wire protocol."""
    try:
        if not isinstance(text, str) or len(text) > 4096:
            raise ValueError()
        value = json.loads(text)
        if (not isinstance(value, dict) or type(value.get("protocol")) is not int
                or value["protocol"] != 1 or value.get("connector") != connector
                or not isinstance(value.get("token"), str)
                or not re.fullmatch(r"[\x21-\x7e]{32,512}", value["token"])):
            raise ValueError()
        address = value.get("address")
        if not isinstance(address, str) or not re.fullmatch(
                r"ws://(?:127\.0\.0\.1|localhost|\[::1\]):[0-9]{1,5}/xrt", address):
            raise ValueError()
        if not 1 <= urlsplit(address).port <= 65535:
            raise ValueError()
        return address, value["token"]
    except (ValueError, TypeError, KeyError):
        raise ValueError("Choose " + connector + " in XRTranslate and copy its connection info") from None


class Connector:
    def __init__(self, address, token, connector, *, on_reset, on_state, on_caption, on_clear, on_display=None):
        parsed = urlsplit(address)
        if (parsed.scheme != "ws" or parsed.hostname not in ("localhost", "127.0.0.1", "::1")
                or parsed.username or parsed.password or parsed.query or parsed.fragment):
            raise ValueError("XRT connector address must be a local ws:// URL")
        self.address = address
        self.token = token
        self.connector = connector
        self.callbacks = {"reset": on_reset, "state": on_state,
                          "caption": on_caption, "clear": on_clear, "display": on_display}
        self.socket = None
        self.session = None
        self.ready = False
        self.sequence = 0
        self.reset_sequence = 0
        self.received_at = time.monotonic()

    async def open(self):
        self.socket = await connect(self.address, max_size=65536, max_queue=16,
                                    open_timeout=5, close_timeout=2, proxy=None)
        try:
            await self.socket.send(json.dumps({
                "type": "hello", "protocol": 1, "connector": self.connector,
                "token": self.token, "client_id": str(uuid.uuid4()),
                "capabilities": ["captions", "display-v1"] if self.callbacks["display"] else ["captions"],
            }))
            self.token = None
            welcome = json.loads(await asyncio.wait_for(self.socket.recv(), 5))
            if (welcome.get("type") != "welcome" or welcome.get("protocol") != 1
                    or not isinstance(welcome.get("session"), str) or not welcome["session"]):
                raise RuntimeError("XRTranslate did not accept the connector handshake")
            self.session = welcome["session"]
            self.received_at = time.monotonic()
        except BaseException:
            await self.socket.close()
            raise

    async def send(self, kind, **fields):
        await self.socket.send(json.dumps({"type": kind, "session": self.session, **fields}))

    async def set_enabled(self, enabled):
        if not self.ready:
            return False
        self.sequence += 1
        await self.send("set_enabled", sequence=self.sequence, enabled=bool(enabled))
        return True

    async def receive(self):
        async for raw in self.socket:
            message = json.loads(raw)
            if message.get("session") != self.session:
                raise RuntimeError("XRTranslate connector session changed")
            self.received_at = time.monotonic()
            kind = message.get("type")
            if kind == "reset":
                reset = message.get("reset")
                if type(reset) is not int or reset <= self.reset_sequence:
                    continue
                self.ready = False
                await self.callbacks["reset"](message)
                await self.send("reset_ack", reset=reset, enabled=False)
                self.reset_sequence = reset
                self.ready = True
            elif kind in ("state", "caption", "clear", "display") and self.callbacks[kind]:
                await self.callbacks[kind](message)
            elif kind != "pong":
                raise RuntimeError("Unknown XRTranslate connector message")
        raise ConnectionError("XRTranslate connection closed")

    async def heartbeat(self):
        while True:
            await asyncio.sleep(3)
            if time.monotonic() - self.received_at >= 15:
                raise TimeoutError("XRTranslate heartbeat expired")
            await self.send("ping")

    async def run(self):
        tasks = [asyncio.create_task(self.receive()), asyncio.create_task(self.heartbeat())]
        try:
            done, _ = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
            for task in done:
                task.result()
        finally:
            self.ready = False
            for task in tasks:
                task.cancel()
            await asyncio.gather(*tasks, return_exceptions=True)

    async def close(self):
        if self.socket is not None:
            self.ready = False
            await self.socket.close()
        self.token = None
