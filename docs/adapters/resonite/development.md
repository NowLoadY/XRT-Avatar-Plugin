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
retains result identity, revision and expiry. Native Resonite components own
rendering and authored appearance.

## Native installation

The installer creates an XRT slot tagged
`XRT-Avatar-Plugin/resonitelink/v1`. Its children contain `Enabled` as a
`ValueField<bool>` and `Status` and `Text` as `ValueField<string>`. An older owned
installation gains only the missing Text child when the bridge starts. Ambiguous
or incompatible existing children are rejected instead of replaced.

An optional preset copies the selected TextRenderer's font and material
references into a new Caption slot, initializes size and bounds from the shared
profile, and creates `ValueCopy<string>` from `Text.Value` to the new renderer's
Text field. References use the fields' returned IDs, not component IDs. The
installer checks the native member types before wiring them. Failure removes only
the subtree created by that installation attempt.

Font and material providers remain referenced assets. Keep them with the saved
Avatar. The XRT and Caption slots carry `AvatarDestroyOnDequip`; deleting a moved
caption during manual removal remains the user's responsibility.

## Custom text bindings

`XRT/Text.Value` supplies the current sanitized plain text without preset wrapping
or truncation. Native `ValueCopy<string>` bindings can feed text components.
ProtoFlux can read it through an `ObjectValueSource<string>` and the appropriate
GlobalReference to the Value field. In FrooxEngine's data model, string uses the
value-field family; in ProtoFlux it uses the object nodes.

For a direct unbound renderer, pass `--text TEXT_RENDERER_COMPONENT_ID` to `run`.
The bridge temporarily manages only `Text` and `ParseRichText`. It preserves the
font, material, color, size and position, and restores the original managed
values on exit only when they still match its last writes. Do not also drive the
same Text field with ValueCopy. The owned Text source is available in this mode.

Stop and reset clear the source. Expiry is enforced by the running bridge. A
forcibly terminated process or unavailable ResoniteLink connection cannot clear
native fields; recovery requires clearing the source and any directly managed
TextRenderer in Resonite. Scene IDs are session-local and must be rediscovered
after a saved Avatar is loaded.

## Verification

The integration follows the official ResoniteLink models, currently documented
as beta. Source checks do not establish a Resonite client acceptance result.
A focused in-game check should confirm:

- Installation preserves existing Avatar components and creates working field-level bindings.
- Each preset renders using the selected font and material; later appearance edits survive caption updates.
- The Text source works without `--text`, including Unicode and line breaks.
- Starting and stopping report the actual state; stopping clears every bound display.
- Duplicate or replaced results cannot restore an expired caption.
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
