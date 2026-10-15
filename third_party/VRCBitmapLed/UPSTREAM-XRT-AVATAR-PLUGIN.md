# VRCBitmapLed reference source

Source: https://github.com/lolosiax/VRCBitmapLed
Revision: `a4e860dc646d2f3db33422f4e0c33b62b3f4d3f3`
Retrieved: 2026-10-15
License: [MIT](LICENSE), copyright 洛洛希雅Lolosia.

This source snapshot preserves upstream source files and the reference Animator
controllers unchanged. `EXCLUDED.txt` lists omitted generated animation/texture
assets, precompiled native dependencies, a local database and a large web image.
The snapshot is for source reference; it is not an importable Unity package.

The addressed parameter bus, UTF-16BE byte pairs and Animator-local copy buffer
inform XRT's VRChat caption receiver. XRT implements its own installer, bounded
chunk protocol and SDF renderer. The original bitmap font and generated Animator
assets are not shipped in XRT install packages. The shared renderer uses Noto
under the SIL Open Font License; its notices travel with the render assets.

Platform synchronization lives in `adapters/vrchat/`; the SDK-independent glyph
renderer lives in `presentation/unity/`. Release packaging selects only the
adapter package roots and excludes this reference directory.
