"""Small, serialized client for the official ResoniteLink data-model API."""

import asyncio
import itertools
from urllib.parse import urlsplit


def loopback_url(value):
    parsed = urlsplit(value)
    if (parsed.scheme != "ws" or parsed.hostname not in ("localhost", "127.0.0.1", "::1")
            or parsed.username or parsed.password or parsed.fragment):
        raise ValueError("Use a ws:// loopback URL shown by the local application")
    return value


def field(kind, value):
    return {"$type": kind, "value": value}


class Link:
    def __init__(self, websocket):
        self.websocket = websocket
        self.sequence = itertools.count(1)
        self.lock = asyncio.Lock()

    async def request(self, kind, **fields):
        import json

        async with self.lock:
            request_id = "xrt-" + str(next(self.sequence))
            await self.websocket.send(json.dumps({"$type": kind, "messageId": request_id, **fields}))
            response = json.loads(await asyncio.wait_for(self.websocket.recv(), timeout=5))
            if response.get("sourceMessageId") != request_id:
                raise RuntimeError("ResoniteLink response identity changed")
            if response.get("success") is not True:
                raise RuntimeError("ResoniteLink: " + str(response.get("errorInfo", "request failed")))
            return response

    async def slot(self, slot_id, depth=1):
        return (await self.request("getSlot", slotId=slot_id,
                                   includeComponentData=True, depth=depth))["data"]

    async def component(self, component_id):
        return (await self.request("getComponent", componentId=component_id))["data"]

    async def write(self, component_id, **members):
        await self.request("updateComponent", data={"id": component_id, "members": members})


OWNER_TAG = "XRT-Avatar-Plugin/resonitelink/v1"
BOOL_TYPE = "[FrooxEngine]FrooxEngine.ValueField<bool>"
STRING_TYPE = "[FrooxEngine]FrooxEngine.ValueField<string>"


async def install(link, parent_id):
    """Create one owned subtree; never modify existing Avatar components."""
    import uuid

    await link.slot(parent_id, 0)
    prefix = "XRT_" + uuid.uuid4().hex
    slot_id = prefix + "_root"
    await link.request("addSlot", data={
        "id": slot_id, "parent": {"$type": "reference", "targetId": parent_id},
        "name": field("string", "XRT"), "tag": field("string", OWNER_TAG),
    })
    try:
        await link.request("addComponent", containerSlotId=slot_id, data={
            "componentType": "[FrooxEngine]FrooxEngine.AvatarDestroyOnDequip",
        })
        for name, kind, value, component_type in (
            ("Enabled", "bool", False, BOOL_TYPE),
            ("Status", "string", "Off", STRING_TYPE),
        ):
            child_id = prefix + "_" + name
            await link.request("addSlot", data={
                "id": child_id, "parent": {"$type": "reference", "targetId": slot_id},
                "name": field("string", name),
            })
            await link.request("addComponent", containerSlotId=child_id, data={
                "componentType": component_type, "members": {"Value": field(kind, value)},
            })
    except BaseException:
        # This subtree was created by this invocation and has not been handed off.
        await link.request("removeSlot", slotId=slot_id)
        raise
    return await link.slot(slot_id)


async def controls(link, slot_id):
    root = await link.slot(slot_id)
    if root.get("tag", {}).get("value") != OWNER_TAG:
        raise ValueError("Select the XRT slot created by the install command")
    result = {}
    for name, component_type in (("Enabled", BOOL_TYPE), ("Status", STRING_TYPE)):
        children = [child for child in root.get("children", [])
                    if child.get("name", {}).get("value") == name]
        if len(children) != 1:
            raise ValueError("XRT control slot is missing or ambiguous: " + name)
        components = [component for component in children[0].get("components", [])
                      if component.get("componentType") == component_type]
        if len(components) != 1:
            raise ValueError("XRT control component is missing or incompatible: " + name)
        result[name] = components[0]["id"]
    return result
