# Refactoring contract

Preserve installed Avatar assets, existing host behavior, settings and stable
identities unless a versioned change explicitly replaces them. Keep the current
working tree as the baseline and protect unrelated user changes.

The host composes capabilities and owns shared resources. Domain policy stays
with the control core or presentation adapter. UI emits typed actions and does
not reach into another plugin's state. Shared infrastructure remains independent
of game-specific implementations.

Keep one owner for each rule. Protocol values come from the manifest, SDK and
platform restrictions from their adapters, and language/provider policy from
XRTranslate. Generate cross-language constants instead of editing both copies.
Do not add generic frameworks for hypothetical games or renderers.

Avatar installation must be reversible and scoped. Never overwrite a source
menu, replace unrelated FX layers, rename protocol parameters automatically or
delete user-modified generated assets as a shortcut to uninstalling.

Read affected callers before choosing verification. Build or add tests only for
a concrete important uncertainty, then consolidate necessary checks. Distinguish
source review, host execution and Unity/VRChat verification. Passing a Rust check
does not establish Avatar compatibility.

Architecture documents describe current contracts. Update the section that owns
each rule. Releases contain tracked package assets and applicable license files
only; SDK binaries, credentials and workspace caches are not release inputs.
