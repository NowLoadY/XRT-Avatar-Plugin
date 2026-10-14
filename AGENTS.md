# Working contract

Keep ownership explicit: platform adapters provide Avatar interaction and
platform-specific control; XRTranslate owns sessions, audio, translation and
results. Share host capabilities through their existing owners.

Keep SDK integration, transport semantics and control policy under the relevant
`adapters/<platform>/` directory. Introduce shared abstractions when implemented
adapters have a concrete common contract. Platform details belong in
`docs/adapters/<platform>/`; top-level documents own repository and host rules.

Architecture documents describe ownership, dependency and lifecycle rules.
Put procedures in guides and wire fields in the adapter's protocol manifest.
Source commits and release archives contain project source, documentation and
required assets. Exclude credentials, local runtime data, generated files and
third-party SDK binaries.

Preserve user-authored assets and unrelated host sessions. Use stable identities,
explicit ownership and reversible installation operations. State compatibility
against the platform and host versions actually verified.

Prefer source inspection and existing evidence. Add tests or run builds only
to answer a concrete, consequential uncertainty; consolidate necessary checks.
Update the document that owns a rule instead of duplicating the rule.
