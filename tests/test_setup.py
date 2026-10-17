"""Check setup boundaries without connecting to a game or editing real assets."""
import asyncio
import copy
import json
import unittest
from unittest.mock import AsyncMock, patch

from adapters.resonite.resonitelink.bridge import ResoniteAdapter
from adapters.resonite.resonitelink.launcher import bind_text
from adapters.resonite.resonitelink.link import Link, OWNER_TAG, TEXT_TYPE, field
from connectors.websocket.python.xrt_connector import parse_pairing


class PairingTests(unittest.TestCase):
    def test_only_the_selected_local_connector_is_accepted(self):
        value = dict(protocol=1, connector="resonite", address="ws://127.0.0.1:43821/xrt", token="x" * 64)
        for host in ("127.0.0.1", "localhost", "[::1]"):
            valid = dict(value, address=f"ws://{host}:43821/xrt")
            self.assertEqual(parse_pairing(json.dumps(valid), "resonite"), (valid["address"], valid["token"]))
        invalid = [None, [], dict(value, protocol=True), dict(value, connector="overte"),
                   dict(value, token="short"), dict(value, token="x" * 63 + "\n")]
        invalid += [dict(value, address=address) for address in (
            "ws://example.org:43821/xrt", "wss://127.0.0.1:43821/xrt", "ws://127.0.0.1:0/xrt",
            "ws://localhost:65536/xrt", "ws://user@localhost:43821/xrt", "ws://localhost:43821/xrt?token=x")]
        for candidate in invalid:
            with self.subTest(candidate=candidate), self.assertRaises(ValueError):
                parse_pairing(json.dumps(candidate), "resonite")


class TextBindingTests(unittest.IsolatedAsyncioTestCase):
    async def test_disconnect_restores_a_write_cancelled_after_it_reached_resonite(self):
        members = {"Text": field("string", "Original"), "ParseRichText": field("bool", True)}
        applied = asyncio.Event()
        link = AsyncMock()

        async def component(_id):
            return {"componentType": TEXT_TYPE, "members": copy.deepcopy(members)}

        async def write(component_id, **fields):
            if component_id == "text":
                members.update(copy.deepcopy(fields))
                if fields.get("Text", {}).get("value") == "Live caption":
                    applied.set()
                    await asyncio.Event().wait()

        link.component.side_effect = component
        link.write.side_effect = write
        adapter = ResoniteAdapter(link, {"Enabled": "enabled", "Status": "status"}, "text")
        adapter.connector = AsyncMock()
        await adapter.prepare()
        pending = asyncio.create_task(adapter.write_text(Text=field("string", "Live caption")))
        await applied.wait()
        pending.cancel()
        with self.assertRaises(asyncio.CancelledError):
            await pending
        await adapter.finish()
        self.assertEqual(members["Text"]["value"], "Original")
        self.assertTrue(members["ParseRichText"]["value"])

    async def test_cancelled_poll_does_not_consume_a_restoration_response(self):
        sent = asyncio.Queue()
        received = asyncio.Queue()
        socket = AsyncMock()
        socket.send.side_effect = sent.put
        socket.recv.side_effect = received.get
        link = Link(socket)
        poll = asyncio.create_task(link.component("enabled"))
        read = json.loads(await sent.get())
        poll.cancel()
        restore = asyncio.create_task(link.write("text", Text=field("string", "Original")))
        await asyncio.sleep(0)
        self.assertTrue(sent.empty(), "Restore must wait for the cancelled read's response")
        await received.put(json.dumps({"sourceMessageId": read["messageId"], "success": True, "data": {}}))
        with self.assertRaises(asyncio.CancelledError):
            await poll
        write = json.loads(await sent.get())
        self.assertEqual(write["$type"], "updateComponent")
        await received.put(json.dumps({"sourceMessageId": write["messageId"], "success": True}))
        await restore

    async def test_stale_selection_and_duplicate_controls_never_create_objects(self):
        link = AsyncMock()
        link.slot.return_value = {"components": [{"id": "another", "componentType": TEXT_TYPE}]}
        with patch("adapters.resonite.resonitelink.launcher.install", new_callable=AsyncMock) as install:
            with self.assertRaises(ValueError):
                await bind_text(link, "slot", "selected")
            link.slot.return_value = {
                "components": [{"id": "selected", "componentType": TEXT_TYPE}],
                "children": [{"id": str(i), "tag": field("string", OWNER_TAG)} for i in range(2)],
            }
            with self.assertRaises(ValueError):
                await bind_text(link, "slot", "selected")
            install.assert_not_awaited()
            link.request.assert_not_awaited()

    async def test_reconnecting_reuses_existing_owned_controls(self):
        link = AsyncMock()
        link.slot.return_value = {
            "components": [{"id": "text", "componentType": TEXT_TYPE}],
            "children": [{"id": "owned", "tag": field("string", OWNER_TAG)}],
        }
        with patch("adapters.resonite.resonitelink.launcher.install", new_callable=AsyncMock) as install, \
             patch("adapters.resonite.resonitelink.launcher.controls", new_callable=AsyncMock) as controls:
            await bind_text(link, "slot", "text")
            install.assert_not_awaited()
            controls.assert_awaited_once_with(link, "owned", ensure_text=True)

    async def test_disconnect_restores_only_fields_still_owned_by_the_adapter(self):
        for user_edit in (False, True):
            with self.subTest(user_edit=user_edit):
                members = {"Text": field("string", "Original text"), "ParseRichText": field("bool", True),
                           "Size": field("float", 0.1), "Font": {"$type": "reference", "targetId": "user-font"}}
                original = copy.deepcopy(members)
                link = AsyncMock()

                async def component(_id):
                    return {"componentType": TEXT_TYPE, "members": copy.deepcopy(members)}

                async def write(component_id, **fields):
                    if component_id == "text":
                        members.update(copy.deepcopy(fields))
                link.component.side_effect = component
                link.write.side_effect = write
                adapter = ResoniteAdapter(link, {"Enabled": "enabled", "Status": "status"}, "text")
                adapter.connector = AsyncMock()
                await adapter.prepare()
                await adapter.write_text(Text=field("string", "Live caption"))
                if user_edit:
                    members["Text"] = field("string", "Edited by the user")
                await adapter.finish()
                self.assertEqual(members["Text"]["value"], "Edited by the user" if user_edit else "Original text")
                for key in ("ParseRichText", "Size", "Font"):
                    self.assertEqual(members[key], original[key])
                adapter.connector.close.assert_awaited_once()


if __name__ == "__main__":
    unittest.main()
