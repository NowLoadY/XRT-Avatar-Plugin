#!/usr/bin/env python3
"""Single-pair, in-memory exchange for the Second Life connector.

Place behind an HTTPS reverse proxy. Both peers initiate their connections.
"""
from __future__ import annotations

import hmac
import json
import os
import time
import uuid
from http.server import BaseHTTPRequestHandler, HTTPServer

MAX_BODY = 12_288
MAX_FRAME = 2_048
MAX_QUEUE = 16
MAX_BATCH = 4
PEER_TTL = 15.0


def encode(value: object) -> bytes:
    return json.dumps(value, separators=(",", ":"), ensure_ascii=False).encode("utf-8")


class Peer:
    def __init__(self) -> None:
        self.instance = ""
        self.seen = 0.0
        self.sequence = -1
        self.response: dict | None = None
        self.response_at = 0.0
        self.queue: list[tuple[dict, float | None]] = []


class Pair:
    def __init__(self) -> None:
        self.generation = str(uuid.uuid4())
        self.peers = {"client": Peer(), "host": Peer()}

    def reset(self) -> None:
        self.generation = str(uuid.uuid4())
        for peer in self.peers.values():
            peer.queue.clear()
            peer.response = None

    def exchange(self, role: str, request: dict, now: float) -> dict:
        instance = request.get("instance")
        sequence = request.get("sequence")
        messages = request.get("messages")
        if not isinstance(instance, str) or not 1 <= len(instance) <= 128:
            raise ValueError("Invalid instance")
        if type(sequence) is not int or not 0 <= sequence <= 2_147_483_647:
            raise ValueError("Invalid sequence")
        if not isinstance(messages, list) or len(messages) > MAX_BATCH:
            raise ValueError("Invalid message batch")
        if any(not isinstance(frame, dict) or len(encode(frame)) > MAX_FRAME for frame in messages):
            raise ValueError("Invalid frame")
        if not isinstance(request.get("generation"), str):
            raise ValueError("Invalid generation")
        if role == "host":
            for message in messages:
                if message.get("type") == "caption":
                    lifetime = message.get("expires_ms")
                    if type(lifetime) is not int or not 0 <= lifetime <= 15_000:
                        raise ValueError("Invalid caption expiry")

        for peer in self.peers.values():
            if peer.instance and now - peer.seen >= PEER_TTL:
                peer.instance = ""
                peer.sequence = -1
                self.reset()
        local = self.peers[role]
        other = self.peers["host" if role == "client" else "client"]
        if instance != local.instance:
            local.instance = instance
            local.sequence = -1
            self.reset()
        local.seen = now
        if sequence == local.sequence and local.response is not None:
            elapsed_ms = max(0, int((now - local.response_at) * 1000))
            replay = []
            for frame in local.response["messages"]:
                if role != "client" or frame.get("type") != "caption":
                    replay.append(frame)
                elif frame["expires_ms"] > elapsed_ms:
                    replay.append({**frame, "expires_ms": frame["expires_ms"] - elapsed_ms})
            return {**local.response, "messages": replay}
        if sequence <= local.sequence:
            # A new generation can invalidate a response before its retry arrives.
            # A mismatch is reconciled below; old traffic in this generation is rejected.
            if request["generation"] == self.generation:
                raise ValueError("Stale sequence")
        synchronized = request["generation"] == self.generation
        if synchronized and len(other.queue) + len(messages) > MAX_QUEUE:
            self.reset()
            synchronized = False
        if synchronized:
            for message in messages:
                expires = None
                if role == "host" and message.get("type") == "caption":
                    lifetime = message.get("expires_ms")
                    expires = now + lifetime / 1000
                    other.queue = [(frame, deadline) for frame, deadline in other.queue
                                   if frame.get("type") != "caption"]
                elif role == "host" and (message.get("type") in ("clear", "reset")
                        or message.get("type") == "state" and message.get("status") != "active"):
                    other.queue = [(frame, deadline) for frame, deadline in other.queue
                                   if frame.get("type") != "caption"]
                other.queue.append((message, expires))
            pending = local.queue[:MAX_BATCH]
            del local.queue[:MAX_BATCH]
            incoming = []
            for message, expires in pending:
                if expires is None:
                    incoming.append(message)
                elif expires > now:
                    incoming.append({**message, "expires_ms": int((expires - now) * 1000)})
        else:
            incoming = []
        response = {
            "generation": self.generation,
            "reset": not synchronized,
            "peer_online": bool(other.instance) and now - other.seen < PEER_TTL,
            "messages": incoming,
        }
        local.sequence = sequence
        local.response = response
        local.response_at = now
        return response


class ExchangeServer(HTTPServer):
    def __init__(self, address: tuple[str, int], tokens: dict[str, str]) -> None:
        self.tokens = tokens
        self.pair = Pair()
        super().__init__(address, ExchangeHandler)

    def get_request(self):
        connection, address = super().get_request()
        connection.settimeout(3)
        return connection, address


class ExchangeHandler(BaseHTTPRequestHandler):
    server: ExchangeServer
    server_version = "XRT-Relay/1"

    def log_message(self, _format: str, *args: object) -> None:
        # Requests can contain authorization data; the relay does not log them.
        pass

    def reply(self, status: int, value: dict) -> None:
        payload = encode(value)
        self.send_response(status)
        self.send_header("Content-Type", "application/json; charset=utf-8")
        self.send_header("Content-Length", str(len(payload)))
        self.send_header("Cache-Control", "no-store")
        self.send_header("Connection", "close")
        self.end_headers()
        self.wfile.write(payload)
        self.close_connection = True

    def do_POST(self) -> None:
        role = {"/v1/client/exchange": "client", "/v1/host/exchange": "host"}.get(self.path)
        if role is None:
            self.reply(404, {"error": "Unknown endpoint"})
            return
        expected = "Bearer " + self.server.tokens[role]
        if not hmac.compare_digest(self.headers.get("Authorization", "").encode("utf-8"), expected.encode("ascii")):
            self.reply(401, {"error": "Unauthorized"})
            return
        try:
            length = int(self.headers.get("Content-Length", "0"))
            if length <= 0 or length > MAX_BODY or self.headers.get("Transfer-Encoding"):
                raise ValueError("Invalid body length")
            body = self.rfile.read(length)
            if len(body) != length:
                raise ValueError("Incomplete body")
            request = json.loads(body)
            if not isinstance(request, dict):
                raise ValueError("Invalid request")
            response = self.server.pair.exchange(role, request, time.monotonic())
        except (ValueError, UnicodeError):
            self.reply(400, {"error": "Invalid exchange"})
            return
        self.reply(200, response)


def main() -> None:
    tokens = {role: os.environ.get(f"XRT_RELAY_{role.upper()}_TOKEN", "") for role in ("client", "host")}
    if any(len(token) < 32 or not token.isascii() for token in tokens.values()):
        raise SystemExit("Set two distinct ASCII relay tokens of at least 32 characters.")
    if hmac.compare_digest(tokens["client"], tokens["host"]):
        raise SystemExit("Client and host tokens must differ.")
    address = (os.environ.get("XRT_RELAY_BIND", "127.0.0.1"), int(os.environ.get("XRT_RELAY_PORT", "8788")))
    with ExchangeServer(address, tokens) as server:
        print(f"XRT relay listening on {address[0]}:{address[1]}; HTTPS proxy required.", flush=True)
        try:
            server.serve_forever()
        except KeyboardInterrupt:
            pass


if __name__ == "__main__":
    main()
