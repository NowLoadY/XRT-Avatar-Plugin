# VRChat development

For the ready-to-import package and VCC installation, use the
[installation guide](installation.md).

## Source layout

Paths are relative to `adapters/vrchat/`.

| Path | Responsibility |
|---|---|
| `packages/dev.nowloady.xrt-avatar/` | Unity editor package for menu and caption installation |
| `protocol/avatar-parameters.json` | Control parameter names, types, defaults and status codes |
| `protocol/caption.json` | Buffered text bus, capabilities and layouts |
| `control/` | Generated Rust protocol bindings and shared lifecycle re-export |
| `examples/lifecycle.rs` | Executable lifecycle scenarios |
| `tools/generate_protocol.py` | Rust and C# binding generation from the manifest |

The Rust package name `xrt-avatar-control`, VPM package ID
`dev.nowloady.xrt-avatar` and protocol parameter names are stable adapter identities.
XRTranslate owns sessions and the shared OSC/OSCQuery transport; see
[host integration](host-integration.md) and [control protocol](control-protocol.md).

## Unity authoring

The shared window is generated from `authoring/unity/AvatarControlsWindow.cs.template`.
Edit that template and run `python3 tools/generate_unity_editor.py` from the repository
root. The installer remains here and uses the native Avatar Descriptor and
Expressions assets. Menu installation leaves playable layers and other tools' components in place.
Caption installation clones FX and appends its receiver and display layers.
Additional text objects share those layers, clips and synchronization parameters.
Records identify owned contributions directly. Placement changes update each
tracked renderer's material bindings; removal retains unrelated menu, parameter
and animator edits. Existing records are hydrated without saving during inspection
and persisted by an authoring operation.
Control parameters remain unsaved and unsynchronized; the caption bus uses the
budget declared in its manifest. When NDMF is present, its public parameter
providers supply the effective names and usage of build-time contributions for
capacity checks; SDK-only projects use their Expressions Parameters and defaults.

When the descriptor has no custom menu, installation creates one and records
the original references and `customExpressions` flag. The SDK's public
`VRCExpressionParametersEditor.GetDefaultParameters()` supplies missing defaults;
an existing menu's `Parameters` reference takes precedence. The installer rejects
undeclared custom menu parameters instead of guessing types. The SDK sample
`DefaultExpressionsMenu.asset` is empty and is not a copy of the client's default
menu.

This follows [Modular Avatar's menu composition](https://modular-avatar.nadena.dev/docs/reference/menu-installer)
and [VRCFury's prefab workflow](https://vrcfury.com/getting-started/) principles of
preserving author assets. Optional future integrations belong in this adapter;
external OSC names must remain global, as described by
[VRCFury](https://vrcfury.com/components/full-controller/#global-parameters) and
[Modular Avatar](https://modular-avatar.nadena.dev/docs/reference/parameters).

## Generated bindings and lifecycle example

Run from the repository root when changing the parameter contract:

```sh
python3 adapters/vrchat/tools/generate_protocol.py
```

Add `--check` to verify the bindings without writing. The existing lifecycle
example can exercise control-state scenarios:

```sh
cargo run -p xrt-avatar-control --example lifecycle
```

These checks do not establish Unity or game compatibility. The
[compatibility document](compatibility.md) owns target versions and acceptance;
the [presentation contract](presentation.md) owns Avatar text rendering.

Shared render assets live under `presentation/unity/`; run
`python3 tools/sync_unity_presentation.py` after editing them. Font generation is
documented in [Unity caption rendering](../../unity-caption-rendering.md). The package copies are generated release inputs.
