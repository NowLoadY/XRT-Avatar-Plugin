# Working contract

Keep ownership explicit: Avatar assets provide interaction, the control core
owns intent and lifecycle, and XRTranslate owns audio, translation and output.
Do not add another inference pipeline, OSC listener or client modification.

Architecture documents contain current ownership, dependency and lifecycle
rules. Put procedures in guides and wire fields in the protocol manifest.
Source commits and release archives contain project source, documentation and
required assets. Exclude credentials, local runtime data, generated files and
third-party SDK binaries.

Preserve user-authored Avatar assets and unrelated host sessions. Use stable
identities, explicit ownership and reversible editor operations. Do not claim
runtime compatibility from static checks alone.

Prefer source inspection and existing evidence. Add tests or run builds only
to answer a concrete, consequential uncertainty; consolidate necessary checks.
Update the document that owns a rule instead of duplicating the rule.
