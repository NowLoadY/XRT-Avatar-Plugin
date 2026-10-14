# VRChat adapter

Provides `Expressions → XRT → Translation` through the official Avatar SDK.
XRTranslate handles the desktop session and shared OSC/OSCQuery transport.
See the [adapter guide](../../docs/adapters/vrchat/README.md) for installation
and compatibility.

| Path | Responsibility |
|---|---|
| `packages/dev.nowloady.xrt-avatar/` | Unity editor package for menu installation and removal |
| `protocol/avatar-parameters.json` | VRChat parameter names, types, defaults and status codes |
| `control/` | I/O-free lifecycle core and generated Rust protocol bindings |
| `examples/lifecycle.rs` | Executable lifecycle scenarios |
| `tools/generate_protocol.py` | Rust and C# bindings generated from the manifest |

From the repository root, regenerate bindings with
`python3 adapters/vrchat/tools/generate_protocol.py`; add `--check` to verify
without writing. Run the existing lifecycle example with
`cargo run -p xrt-avatar-control --example lifecycle`.

The Rust package name `xrt-avatar-control`, VPM package ID
`dev.nowloady.xrt-avatar` and protocol parameter names are stable adapter identities.
