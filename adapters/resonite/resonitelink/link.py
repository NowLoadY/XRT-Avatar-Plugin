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
CAPTION_TAG = "XRT-Avatar-Plugin/resonite-caption/v1"
BOOL_TYPE = "[FrooxEngine]FrooxEngine.ValueField<bool>"
STRING_TYPE = "[FrooxEngine]FrooxEngine.ValueField<string>"
TEXT_TYPE = "[FrooxEngine]FrooxEngine.TextRenderer"
COPY_TYPE = "[FrooxEngine]FrooxEngine.ValueCopy<string>"
TEXT_SOURCES = {"full": "Text", "header": "Header", "messages": "Messages", "footer": "Footer"}


def reference(target_id):
    return {"$type": "reference", "targetId": target_id}


async def add_value(link, parent_id, prefix, name, kind, value, component_type):
    slot_id = prefix + "_" + name
    component_id = slot_id + "_value"
    await link.request("addSlot", data={
        "id": slot_id, "parent": reference(parent_id), "name": field("string", name),
    })
    try:
        await link.request("addComponent", containerSlotId=slot_id, data={
            "id": component_id, "componentType": component_type,
            "members": {"Value": field(kind, value)},
        })
    except BaseException:
        await link.request("removeSlot", slotId=slot_id)
        raise
    return component_id


def template_members(component):
    """Reuse only the selected text object's asset references and appearance."""
    if component.get("componentType") != TEXT_TYPE:
        raise ValueError("Choose a TextRenderer from a working Basic text object as the template")
    members = component["members"]
    font = members.get("Font", {})
    materials = members.get("Materials", {})
    if (font.get("$type") != "reference" or not font.get("targetId")
            or materials.get("$type") != "list" or not materials.get("elements")
            or any(item.get("$type") != "reference" or not item.get("targetId")
                   for item in materials["elements"])):
        raise ValueError("The template needs an assigned font and text material")
    result = {
        "Font": reference(font["targetId"]),
        "Materials": {"$type": "list", "elements": [
            reference(item["targetId"]) for item in materials["elements"]]},
    }
    for name in ("Profile", "Color", "LineHeight", "HorizontalAlign", "VerticalAlign",
                 "BoundsAlignment", "AlignmentMode", "HorizontalAutoSize", "VerticalAutoSize"):
        member = members.get(name)
        if member and "value" in member:
            result[name] = {key: value for key, value in member.items()
                            if key not in ("id", "targetType")}
    return result


async def add_preset(link, parent_id, prefix, source_id, preset, profile, members):
    """Create a native editable renderer driven by the owned text field."""
    slot_id = prefix + "_caption"
    text_id = slot_id + "_renderer"
    driver_id = slot_id + "_copy"
    await link.request("addSlot", data={
        "id": slot_id, "parent": reference(parent_id),
        "name": field("string", "Caption (" + preset.title() + ")"),
        "tag": field("string", CAPTION_TAG),
    })
    await link.request("addComponent", containerSlotId=slot_id, data={
        "componentType": "[FrooxEngine]FrooxEngine.AvatarDestroyOnDequip",
    })
    members.update({
        "Text": field("string", ""), "ParseRichText": field("bool", False),
        "Size": field("float", profile["lineHeight"]),
        "Bounded": field("bool", True),
        "BoundsSize": field("float2", {"x": profile["width"], "y": profile["height"]}),
    })
    await link.request("addComponent", containerSlotId=slot_id, data={
        "id": text_id, "componentType": TEXT_TYPE, "members": members,
    })
    source = (await link.component(source_id))["members"]["Value"]
    target = (await link.component(text_id))["members"]["Text"]
    if source.get("$type") != "string" or target.get("$type") != "string" or not source.get("id") or not target.get("id"):
        raise ValueError("The text fields could not be resolved for the caption binding")
    await link.request("addComponent", containerSlotId=slot_id, data={
        "id": driver_id, "componentType": COPY_TYPE,
    })
    driver = (await link.component(driver_id))["members"]
    if any(driver.get(name, {}).get("$type") != "reference" for name in ("Source", "Target")):
        raise ValueError("This Resonite version does not expose the expected native text binding")
    await link.write(driver_id, Source=reference(source["id"]), Target=reference(target["id"]),
                     WriteBack=field("bool", False))


async def install(link, parent_id, preset=None, template_id=None, profile=None, source="full"):
    """Create one owned subtree; never modify existing Avatar components."""
    import uuid

    await link.slot(parent_id, 0)
    members = None
    if preset:
        if not template_id or profile is None:
            raise ValueError("Choose a TextRenderer template for the caption preset")
        members = template_members(await link.component(template_id))
    prefix = "XRT_" + uuid.uuid4().hex
    slot_id = prefix + "_root"
    await link.request("addSlot", data={
        "id": slot_id, "parent": reference(parent_id),
        "name": field("string", "XRT"), "tag": field("string", OWNER_TAG),
    })
    try:
        await link.request("addComponent", containerSlotId=slot_id, data={
            "componentType": "[FrooxEngine]FrooxEngine.AvatarDestroyOnDequip",
        })
        values = {}
        for name, kind, value, component_type in (
            ("Enabled", "bool", False, BOOL_TYPE),
            ("Status", "string", "Off", STRING_TYPE),
            *((name, "string", "", STRING_TYPE) for name in TEXT_SOURCES.values()),
        ):
            values[name] = await add_value(link, slot_id, prefix, name, kind, value, component_type)
        if preset:
            await add_preset(link, slot_id, prefix, values[TEXT_SOURCES[source]], preset, profile, members)
    except BaseException:
        # This subtree was created by this invocation and has not been handed off.
        await link.request("removeSlot", slotId=slot_id)
        raise
    return await link.slot(slot_id)


async def controls(link, slot_id, ensure_text=False):
    root = await link.slot(slot_id)
    if root.get("tag", {}).get("value") != OWNER_TAG:
        raise ValueError("Select the XRT slot created by the install command")
    result = {}
    for name, component_type in (("Enabled", BOOL_TYPE), ("Status", STRING_TYPE),
                                 *((name, STRING_TYPE) for name in TEXT_SOURCES.values())):
        children = [child for child in root.get("children", [])
                    if child.get("name", {}).get("value") == name]
        if name in TEXT_SOURCES.values() and not children:
            if ensure_text:
                import uuid
                result[name] = await add_value(link, slot_id, "XRT_" + uuid.uuid4().hex,
                                                name, "string", "", STRING_TYPE)
            continue
        if len(children) != 1:
            raise ValueError("XRT control slot is missing or ambiguous: " + name)
        components = [component for component in children[0].get("components", [])
                      if component.get("componentType") == component_type]
        if len(components) != 1:
            raise ValueError("XRT control component is missing or incompatible: " + name)
        result[name] = components[0]["id"]
    return result
