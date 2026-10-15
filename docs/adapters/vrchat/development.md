# VRChat development

For the ready-to-import package and VCC installation, use the
[installation guide](installation.md).

## Source layout

Paths are relative to `adapters/vrchat/`.

| Path | Responsibility |
|---|---|
| `packages/dev.nowloady.xrt-avatar/` | Unity editor package for menu installation and removal |
| `protocol/avatar-parameters.json` | Parameter names, types, defaults and status codes |
| `control/` | Generated Rust protocol bindings and shared lifecycle re-export |
| `examples/lifecycle.rs` | Executable lifecycle scenarios |
| `tools/generate_protocol.py` | Rust and C# binding generation from the manifest |

The Rust package name `xrt-avatar-control`, VPM package ID
`dev.nowloady.xrt-avatar` and protocol parameter names are stable adapter identities.
XRTranslate owns sessions and the shared OSC/OSCQuery transport; see
[host integration](host-integration.md) and [control protocol](control-protocol.md).

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
the [presentation contract](presentation.md) owns future Avatar text rendering.
