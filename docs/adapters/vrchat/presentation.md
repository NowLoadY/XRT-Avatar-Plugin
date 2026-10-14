# VRChat presentation

A future Avatar-attached translation bubble is an independent presentation
adapter. The current package contains no bubble renderer, font atlas, shader or
text-paging protocol. Existing XRTranslate overlays and VRChat Chatbox remain the
available output paths.

## Ownership

The adapter consumes normalized translation results with their owner, message
identity, revision, language and preview/final status. It preserves replacement
and cancellation semantics. It does not start translation, own audio or models,
or reconstruct results from visible UI text.

Presentation policy owns audience, placement, lifetime and a bounded text budget.
The shared transport encodes accepted updates; installed Avatar assets render
those updates. Adding presentation does not change the control state machine or
create another OSC listener.

## Target and visibility

The initial bubble target is the wearer's own Avatar. It is not an API for
attaching objects to other players. Local visibility is the default; sharing
requires an explicit source and audience setting. Mixed system audio provides
neither per-player identity nor a different translation for each viewer.

Avatar parameters carry numeric values rather than arbitrary text. A renderer
therefore needs its own bounded encoding and declared character/font capability;
`/chatbox/input` cannot update an arbitrary Avatar text component. Text encoding,
resource cost and platform support must be established with that renderer before
its parameters become part of a public contract.

## Result lifetime and appearance

Committed text is suitable for shared output. Local output may use replaceable
previews when the renderer supports revisions. Replacement, expiry and owner
cancellation clear pending content; reconnect must not resurrect old text.

The visual package follows the [shared presentation style](../../presentation-architecture.md)
for typography, spacing, restrained color, placement and expiry. It declares actual Avatar resource cost
and its platform fallback. Neither PC shader compatibility nor remote visibility
can be inferred for standalone Android or another user's safety settings.

Official boundaries: [Avatar parameters](https://creators.vrchat.com/avatars/animator-parameters/),
[OSC Chatbox](https://docs.vrchat.com/docs/osc-as-input-controller),
[Android Avatar restrictions](https://creators.vrchat.com/platforms/android/quest-content-limitations/).
