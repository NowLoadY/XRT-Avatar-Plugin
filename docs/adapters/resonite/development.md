# Resonite development

The [user setup guide](installation.md) covers preparing an Avatar and running
the bridge. This document covers integration and verification.

## Files and dependencies

The bridge entry point is
[`bridge.py`](../../../adapters/resonite/resonitelink/bridge.py), with the serialized
ResoniteLink client in [`link.py`](../../../adapters/resonite/resonitelink/link.py).
It needs Python 3.10+ and the version range in
[`requirements.txt`](../../../adapters/resonite/resonitelink/requirements.txt).
Keep these paths together when distributing a source checkout:

- `adapters/resonite/resonitelink/`
- `connectors/websocket/python/`
- `presentation/python/`, `presentation/bubble-style.json` and `presentation/presets.json`

The shared connector owns pairing and reset handling. The shared `CaptionSource`
retains display regions, revision and per-message expiry, with legacy caption
support. Native Resonite components own rendering and authored appearance.

## Native installation

The installer creates an XRT slot tagged
`XRT-Avatar-Plugin/resonitelink/v1`. Its children contain `Enabled` as a
`ValueField<bool>` and `Status`, `Text`, `Header`, `Messages` and `Footer` as
`ValueField<string>`. An older owned installation gains missing text-source
children when the bridge starts. Existing children must remain unambiguous and
have the expected type.

An optional preset copies the selected TextRenderer's font and material
references into a new Caption slot, initializes size and bounds from the shared
profile, preserves the template's native auto-sizing settings, and creates
`ValueCopy<string>` from the selected source's Value to the
new renderer's Text field. `--source` defaults to `full` (`Text`). References use
the fields' returned IDs, not component IDs. The installer checks native member
types before wiring them. Failure removes only the subtree created by that attempt.

Font and material providers remain referenced assets. Keep them with the saved
Avatar. The XRT and Caption slots carry `AvatarDestroyOnDequip`; deleting a moved
caption during manual removal remains the user's responsibility.

## Custom text bindings

`XRT/Text.Value` supplies the complete display as plain text. `Header.Value`,
`Messages.Value` and `Footer.Value` expose independent regions. Messages retain
line breaks and expire individually; a header or footer persists until its source
is cleared. Native `ValueCopy<string>` bindings can feed text components.
ProtoFlux can read it through an `ObjectValueSource<string>` and the appropriate
GlobalReference to the Value field. In FrooxEngine's data model, string uses the
value-field family; in ProtoFlux it uses the object nodes.

For a direct unbound renderer, pass `--text TEXT_RENDERER_COMPONENT_ID` to `run`.
Select its region with `--source full|header|messages|footer`.
The bridge temporarily manages only `Text` and `ParseRichText`. It preserves the
font, material, color, size and position, and restores the original managed
values on exit only when they still match its last writes. Do not also drive the
same Text field with ValueCopy. All owned source fields remain available in this
mode.

Sources retain all current candidate messages. The template's native font and
auto-sizing settings, together with the object's bounds, control rendering;
custom bindings retain their authored layout.

Publication serializes source and direct-renderer writes, resolving the latest
snapshot after waiting for the native connection. The running bridge enforces
message expiry and accepts only newer display revisions. Translation Stop and
control reset preserve independent regions; the host removes the translation's
messages in its next snapshot. Disconnect clears owned fields. A forcibly
terminated process or unavailable ResoniteLink connection requires clearing the
source fields and any directly managed TextRenderer in Resonite. Scene IDs are
session-local and must be rediscovered after a saved Avatar is loaded.

## Verification

The integration follows the official ResoniteLink models, currently documented
as beta. Source checks do not establish a Resonite client acceptance result.
A focused in-game check should confirm:

- Installation preserves existing Avatar components and creates working field-level bindings.
- Each preset renders using the selected font and material; later appearance edits survive caption updates.
- The four source fields work without `--text`, including Unicode and line breaks.
- Starting and stopping report the actual state; stopping preserves independent banners.
- Each message expires independently; older snapshots cannot restore it.
- Reconnection starts Off, and removal or Avatar unequip cleans up the created objects.
- Direct rendering restores unchanged managed properties while preserving user edits.

## Official references

- [ResoniteLink activation](https://wiki.resonite.com/ResoniteLink)
- [ResoniteLink protocol and models](https://github.com/Yellow-Dog-Man/ResoniteLink)
- [Data model and field wiring](https://wiki.resonite.com/Tutorial:Data_Model_Flow)
- [ValueField](https://wiki.resonite.com/Component:ValueField)
- [ValueCopy](https://wiki.resonite.com/Component:ValueCopy)
- [ButtonValueSet](https://wiki.resonite.com/Component:ButtonValueSet)
- [TextRenderer](https://wiki.resonite.com/Component:TextRenderer)
- [AvatarDestroyOnDequip](https://wiki.resonite.com/Component:AvatarDestroyOnDequip)
