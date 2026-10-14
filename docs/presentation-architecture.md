# Presentation architecture

This contract reserves the integration boundary for a future Avatar-attached
translation bubble. It does not define or ship a renderer, font asset, shader,
text-paging protocol or new desktop output service.

## Result ownership

Translation produces semantic results independently of a game. A presentation
adapter consumes normalized events with their owner, message identity, revision,
source/target language and preview/final status. It preserves replacements and
cancellation. A renderer never starts translation or reconstructs a sentence
from visible UI text.

The presentation policy determines visibility, lifetime and a bounded text budget.
Transport encodes the chosen update; a game renderer draws it. Appearance assets
do not own audio, providers, conversation storage or the control state machine.

## Display capabilities

| Target | Audience | Capability boundary |
|---|---|---|
| Existing XRTranslate overlay | Local user | Rich local text; positioning only from available, supported tracking |
| VRChat Chatbox | Viewers allowed by VRChat | Official string endpoint, built-in appearance and limits |
| Future Avatar bubble | Local or explicitly shared | Preinstalled Avatar assets and a negotiated numeric text encoding |

The first Avatar bubble target is the wearer's own Avatar. It is not an API for
attaching objects to arbitrary nearby players. Shared parameter values do not
provide a different translation language for each viewer. No per-player audio
or identity mapping is inferred from a mixed system-audio stream.

Avatar parameters support numeric types, not arbitrary text. A custom bubble
therefore needs a bounded character encoding, font resources and an Avatar-side
rendering mechanism. OSC `/chatbox/input` does not update arbitrary Avatar text
components. Local overlays and native Chatbox remain valid separate outputs.

## Visibility and content

Audience is explicit: local presentation is the default, sharing is opt-in.
Recognized system audio must not become public merely because a bubble exists.
The initial shared policy should publish only the wearer's configured microphone
translations; changing source or audience requires a visible host setting.

Public presentation uses committed translations. Local presentation may show
replaceable previews if the target supports revisions. Dismissal, expiration,
replacement and owner cancellation remove pending content without resurfacing
superseded text after reconnect. Do not insert invisible padding or truncate
inside a grapheme cluster to meet a display budget.

## Renderer contract

A renderer declares its platform, audience, anchor, character repertoire,
character/line capacity, update budget and required Avatar parameters. The adapter
must be able to reject unsupported text explicitly or select a user-configured
fallback. Never advertise full multilingual support from a Latin-only font atlas.

A shared renderer must specify page identity/revision, complete-message commit,
clear, expiry, loss handling and late-viewer behavior before its wire protocol
is accepted. Partial pages must not mix two translations. A local OSC echo is
not acknowledgement that remote viewers received or rendered a message.

Rendering acceptance includes text legibility in VR, font fallback, stable
head-relative placement, distance scaling, mirrored views, occlusion and clean
expiry. A visual prefab must provide a simple size/position adjustment and avoid
unnecessary animations or decorative UI. The Avatar build reports its actual
parameter, texture, material and geometry cost.

PC custom shaders cannot be assumed compatible with standalone Android avatars.
Remote visibility also depends on Avatar and shader safety settings. A platform
fallback must be declared rather than silently claiming the custom bubble is
visible. The control package remains usable without any bubble resources.

Official references: [parameter types](https://creators.vrchat.com/avatars/animator-parameters/),
[OSC Chatbox](https://docs.vrchat.com/docs/osc-as-input-controller),
[Android Avatar restrictions](https://creators.vrchat.com/platforms/android/quest-content-limitations/),
[shader fallback](https://creators.vrchat.com/avatars/shader-fallback-system/).
