#!/usr/bin/env python3
"""Explicit native text selection for the ResoniteLink adapter."""
import asyncio
import contextlib
from pathlib import Path
import queue
import sys
import threading
import tkinter as tk
from tkinter import ttk
import webbrowser

if not getattr(sys, "frozen", False):
    sys.path.insert(0, str(Path(__file__).resolve().parents[3]))
from adapters.resonite.resonitelink.bridge import ResoniteAdapter
from adapters.resonite.resonitelink.link import Link, OWNER_TAG, TEXT_TYPE, controls, field, install, loopback_url
from connectors.websocket.python.xrt_connector import Connector, parse_pairing
from websockets.asyncio.client import connect


async def bind_text(link, slot_id, text_id):
    """Only a deliberate Connect action may create the owned control subtree."""
    slot = await link.slot(slot_id)
    if not any(c.get("id") == text_id and c.get("componentType") == TEXT_TYPE
               for c in slot.get("components", [])):
        raise ValueError("The selected text changed. Refresh the session and select it again.")
    owned = [child for child in slot.get("children", [])
             if child.get("tag", {}).get("value") == OWNER_TAG]
    if len(owned) > 1:
        raise ValueError("This text has more than one XRT control. Keep one before connecting.")
    root = owned[0] if owned else await install(link, slot_id)
    return await controls(link, root["id"], ensure_text=True)


class Window:
    def __init__(self, root):
        self.root = root
        self.events = queue.Queue()
        self.loop = asyncio.new_event_loop()
        self.thread = threading.Thread(target=self.loop.run_forever, daemon=True)
        self.thread.start()
        self.socket = None
        self.link = None
        self.adapter = None
        self.running = None
        self.stopped = None
        self.busy = 0
        self.disconnecting = False
        self.closing = False
        self.nodes = {}
        root.title("XRT · Resonite")
        root.minsize(620, 500)
        frame = ttk.Frame(root, padding=16)
        frame.pack(fill="both", expand=True)
        frame.columnconfigure(1, weight=1)
        frame.rowconfigure(3, weight=1)
        ttk.Label(frame, text="Enable ResoniteLink in a session you host, then paste its address.").grid(
            row=0, column=0, columnspan=3, sticky="w", pady=(0, 10))
        ttk.Label(frame, text="ResoniteLink").grid(row=1, column=0, sticky="w", padx=(0, 8))
        self.address = ttk.Entry(frame)
        self.address.grid(row=1, column=1, sticky="ew")
        self.address.bind("<Return>", lambda _event: self.open_session())
        self.open_button = ttk.Button(frame, text="Open session", command=self.open_session)
        self.open_button.grid(row=1, column=2, padx=(8, 0))
        ttk.Label(frame, text="Choose your text object. Expand an object to see its children.").grid(
            row=2, column=0, columnspan=3, sticky="w", pady=(14, 6))
        self.tree = ttk.Treeview(frame, show="tree", selectmode="browse", height=10)
        self.tree.grid(row=3, column=0, columnspan=2, sticky="nsew")
        scroll = ttk.Scrollbar(frame, orient="vertical", command=self.tree.yview)
        scroll.grid(row=3, column=2, sticky="nsw")
        self.tree.configure(yscrollcommand=scroll.set)
        self.tree.bind("<<TreeviewOpen>>", self.expand)
        self.tree.bind("<Button-1>", lambda _event: "break" if self.running else None)
        self.tree.bind("<KeyPress>", lambda _event: "break" if self.running else None)
        ttk.Label(frame, text="XRTranslate").grid(row=4, column=0, sticky="w", pady=(14, 0))
        self.pairing = ttk.Entry(frame, show="•")
        self.pairing.grid(row=4, column=1, columnspan=2, sticky="ew", pady=(14, 0))
        self.pairing.bind("<Return>", lambda _event: self.start())
        ttk.Label(frame, text="Paste Copy connection info from Game Plugins → Resonite.").grid(
            row=5, column=1, columnspan=2, sticky="w", pady=(4, 12))
        buttons = ttk.Frame(frame)
        buttons.grid(row=6, column=0, columnspan=3, sticky="ew")
        self.connect_button = ttk.Button(buttons, text="Connect", state="disabled",
                                        command=lambda: self.disconnect() if self.running else self.start())
        self.connect_button.pack(side="left")
        self.toggle_button = ttk.Button(buttons, text="Start translation", state="disabled", command=self.toggle)
        self.guide_button = ttk.Button(buttons, text="Guide", command=lambda: webbrowser.open(
            "https://github.com/NowLoadY/XRT-Avatar-Plugin/blob/main/docs/adapters/resonite/installation.md"
        ))
        self.guide_button.pack(side="right")
        self.status = tk.StringVar(value="Create a Basic text object in Resonite, or select one you already own.")
        ttk.Label(frame, textvariable=self.status, wraplength=590).grid(
            row=7, column=0, columnspan=3, sticky="w", pady=(12, 4))
        ttk.Label(frame, text="Connect adds XRT controls beneath the selected text. Move and style it in Resonite.",
                  wraplength=590).grid(row=8, column=0, columnspan=3, sticky="w")
        root.protocol("WM_DELETE_WINDOW", self.close)
        root.after(75, self.drain)

    def connection_controls(self):
        editing = not self.running and not self.busy
        self.open_button.configure(state="normal" if editing else "disabled")
        self.address.configure(state="normal" if editing else "disabled")
        self.pairing.configure(state="disabled" if self.running else "normal")
        self.tree.state(["disabled" if self.running else "!disabled"])
        self.connect_button.configure(text="Disconnect" if self.running else "Connect",
            state="normal" if not self.disconnecting and (self.running or editing and self.link) else "disabled")

    def submit(self, coroutine, callback):
        future = asyncio.run_coroutine_threadsafe(coroutine, self.loop)
        future.add_done_callback(lambda completed: self.events.put((callback, completed)))
        return future

    def drain(self):
        while True:
            try:
                callback, value = self.events.get_nowait()
            except queue.Empty:
                break
            callback(value)
        if not self.closing:
            self.root.after(75, self.drain)

    def result(self, future):
        try:
            return future.result()
        except Exception as error:
            self.status.set(str(error) if isinstance(error, ValueError) else
                            "Connection ended. Check both applications, then open the session again.")
            return None

    def open_session(self):
        if self.busy or self.running:
            return
        address = self.address.get().strip()
        self.busy = 1
        self.tree.delete(*self.tree.get_children())
        self.nodes.clear()
        self.connection_controls()
        self.status.set("Opening session…")

        async def opening():
            if self.socket:
                await self.socket.close()
            self.link = None
            self.socket = await connect(loopback_url(address), max_size=4 * 1024 * 1024,
                                        max_queue=8, open_timeout=5, close_timeout=2, proxy=None)
            self.link = Link(self.socket)
            return await self.link.slot("Root", 1)

        def opened(future):
            self.busy = 0
            slot = self.result(future)
            if slot:
                item = self.add_slot("", slot)
                self.populate(item, slot)
                self.tree.item(item, open=True)
                self.status.set("Select Text under the object you want to use.")
            self.connection_controls()
        self.submit(opening(), opened)

    def add_slot(self, parent, slot):
        item = self.tree.insert(parent, "end", text=slot.get("name", {}).get("value") or "Object")
        self.nodes[item] = ("slot", slot["id"])
        self.tree.insert(item, "end", text="Loading…")
        return item

    def populate(self, item, slot):
        for child in self.tree.get_children(item):
            self.forget(child)
        for component in slot.get("components", []):
            if component.get("componentType") == TEXT_TYPE:
                child = self.tree.insert(item, "end", text="Text")
                self.nodes[child] = ("text", slot["id"], component["id"])
        for child in slot.get("children", []):
            self.add_slot(item, child)

    def forget(self, item):
        for child in self.tree.get_children(item):
            self.forget(child)
        self.nodes.pop(item, None)
        self.tree.delete(item)

    def expand(self, _event):
        item = self.tree.focus()
        node = self.nodes.get(item)
        if not self.link or not node or node[0] != "slot" or self.running:
            return
        self.busy += 1
        self.connection_controls()

        def expanded(future):
            self.busy -= 1
            slot = self.result(future)
            if slot and self.tree.exists(item):
                self.populate(item, slot)
            self.connection_controls()
        self.submit(self.link.slot(node[1], 1), expanded)

    def start(self):
        if self.busy or self.running or not self.link:
            return
        selected = self.tree.selection()
        node = self.nodes.get(selected[0]) if selected else None
        if not node or node[0] != "text":
            self.status.set("Select Text under the object you want to use.")
            return
        try:
            address, token = parse_pairing(self.pairing.get(), "resonite")
        except ValueError as error:
            self.status.set(str(error))
            return
        self.status.set("Connecting…")

        async def serving():
            self.stopped = asyncio.Event()
            tasks = []
            try:
                components = await bind_text(self.link, node[1], node[2])
                self.adapter = ResoniteAdapter(self.link, components, node[2], on_status=lambda label:
                    self.events.put((self.translation_state, (label, self.adapter.requested))))
                adapter = self.adapter
                adapter.connector = Connector(address, token, "resonite", on_reset=adapter.reset,
                    on_state=adapter.state, on_caption=adapter.caption, on_clear=adapter.clear,
                    on_display=adapter.display)
                await adapter.prepare()
                await adapter.connector.open()
                tasks = [asyncio.create_task(adapter.connector.run()), asyncio.create_task(adapter.poll()),
                         asyncio.create_task(self.stopped.wait())]
                done, _ = await asyncio.wait(tasks, return_when=asyncio.FIRST_COMPLETED)
                for task in done:
                    task.result()
            finally:
                # Stop translation immediately, even if a scene read is still pending.
                if self.adapter:
                    with contextlib.suppress(Exception):
                        await self.adapter.connector.close()
                for task in tasks:
                    task.cancel()
                await asyncio.gather(*tasks, return_exceptions=True)
                if self.adapter:
                    with contextlib.suppress(Exception):
                        await asyncio.wait_for(self.adapter.finish(), 8)
                self.adapter = None
                self.stopped = None

        def finished(future):
            self.running = None
            self.disconnecting = False
            self.status.set("Disconnected. Connect again when ready; copy fresh info if XRTranslate restarted.")
            self.result(future)
            self.connection_controls()
            self.toggle_button.configure(state="disabled", text="Start translation")
            self.toggle_button.pack_forget()
        self.running = self.submit(serving(), finished)
        self.connection_controls()

    def translation_state(self, state):
        if self.disconnecting or self.closing or not self.running:
            return
        label, requested = state
        self.status.set(label)
        self.toggle_button.pack(side="left", padx=8, before=self.guide_button)
        self.toggle_button.configure(text="Stop translation" if requested else "Start translation",
                                     state="normal" if label in ("Off", "On", "Starting…", "Check XRTranslate") else "disabled")

    def toggle(self):
        self.toggle_button.configure(state="disabled")

        async def change():
            adapter = self.adapter
            if not adapter or not adapter.connector.ready:
                return
            enabled = not adapter.requested
            if enabled and (adapter.stop_pending or adapter.last_status == "stopping"):
                return
            await self.link.write(adapter.components["Enabled"], Value=field("bool", enabled))
        self.submit(change(), self.result)

    def disconnect(self):
        self.disconnecting = True
        self.connection_controls()
        self.status.set("Disconnecting…")
        self.toggle_button.configure(state="disabled")
        self.loop.call_soon_threadsafe(lambda: self.stopped.set() if self.stopped else None)

    def close(self):
        if self.closing:
            return
        self.closing = True
        self.root.withdraw()

        async def cleanup():
            if self.stopped:
                self.stopped.set()
            if self.running:
                with contextlib.suppress(Exception):
                    await asyncio.wait_for(asyncio.wrap_future(self.running), 12)
            # This private loop also owns in-flight tree reads and connection setup.
            pending = asyncio.all_tasks() - {asyncio.current_task()}
            for task in pending:
                task.cancel()
            await asyncio.gather(*pending, return_exceptions=True)
            if self.socket:
                await self.socket.close()
        future = asyncio.run_coroutine_threadsafe(cleanup(), self.loop)

        def wait_close():
            if future.done():
                self.loop.call_soon_threadsafe(self.loop.stop)
                self.root.destroy()
            else:
                self.root.after(75, wait_close)
        wait_close()


def main():
    if "--check" in sys.argv:
        # Also exercises bundled imports and presentation data without a display/server.
        from presentation.python.bubble import PROFILES
        assert "horizontal" in PROFILES
        print("Resonite connector resources ready")
        return
    root = tk.Tk()
    Window(root)
    root.mainloop()


if __name__ == "__main__":
    main()
