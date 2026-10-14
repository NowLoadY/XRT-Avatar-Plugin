# Refactoring contract

Preserve installed assets, host behavior, settings and stable identities unless
a versioned change explicitly replaces them. Keep the current working tree as
the baseline and protect unrelated user changes.

The host composes capabilities and owns shared resources. Platform control and
presentation policy stay with their adapters. UI emits typed actions through
the owning interface. Shared translation infrastructure stays independent of
platform implementations.

Keep one owner for each rule. Protocol values come from the adapter's manifest,
SDK and platform restrictions from its documentation, and language/provider
policy from XRTranslate. Generate cross-language constants from their source.
Introduce shared abstractions only for concrete common contracts between
implemented adapters.

Installation changes must be reversible and scoped to owned assets. Preserve
user edits during both upgrades and removal. Version public protocol or package
identity changes together with their host compatibility requirements.

Read affected callers before choosing verification. Build or add tests only for
a concrete important uncertainty, then consolidate necessary checks. Record
source validation, host execution and platform runtime verification separately.

Architecture documents describe current contracts. Update the document that
owns each rule. Releases contain tracked package assets and applicable license
files only; SDK binaries, credentials and workspace caches are not release
inputs.
