# Interaction

Each adapter exposes one translation switch. A switch expresses the user's
request; status reports whether translation is actually ready. Controls remain
in the platform's usual menu, toolbar or owner dialog.

## Controls

Use familiar icons for selection, add, remove, refresh, save and help. Every
icon has a short action tooltip and a sufficient hit area; color is an additional
cue, never the only distinction. Label the controlled object or setting beside
the action instead of repeating a sentence on each button. Native text-only
menus use short action labels rather than simulated icon glyphs.

The Unity editor shows an Avatar field, one Translation menu row, installation
status and a compact tool row. Add and remove act on the selected avatar. Status
refresh is nonmutating. In-game setup instructions are collapsed; help opens the
full guide. Changes that need saving expose a save action.

## Caption objects

Object creation and text binding are separate from the translation switch.
Offer a small visual preset chooser and a picker for existing editable objects.
Creation selects the new object; binding changes only its text and explicitly
chosen visibility. Move, resize, rotate and decorate through the native editor.

An eye action toggles authoring sample text until it is dismissed or live
translation starts. Preview text never becomes a translation result. Unbinding
keeps the object and restores owned fields. Connection and status messages stay
outside the caption object.

Owned design surfaces use XRTranslate's light palette and shared icon treatment.
They must remain readable independently of the surrounding editor's theme.

## Translation state

| State | Presentation | Available action |
|---|---|---|
| Not connected | Connection icon or short connection status | Connect or retry |
| Off | Neutral start control | Start |
| Starting | Pending state, distinct from Active | Stop/cancel |
| Active | Active control with a restrained accent | Stop |
| Stopping | Pending stop, no repeated start request | Wait |
| Error | Brief explanation and retry action | Retry after resolving the cause |

Repeated clicks do not queue contradictory requests. A local Stop action clears
captions immediately; delayed feedback cannot turn that action back into Start.
A disconnect, context change or reset returns to Off and requires a new action.
Connection settings and pairing prompts appear when needed rather than on each
ordinary toggle.

## Copy

Window titles identify the tool. Field labels name the object or setting; status
text describes its current condition. Messages state the next useful action.
Avoid promotional headings, subtitles, implementation terminology, protocol
explanations and test-status paragraphs in the control surface. Preserve full
diagnostics in developer logs and keep compatibility information in platform
documentation.

Caption surfaces contain translated text only. Status, errors, branding and
controls are outside the bubble. See [presentation](presentation-architecture.md)
for the shared visual and lifetime policy.
