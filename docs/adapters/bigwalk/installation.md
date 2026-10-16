# Big Walk

Send translations to the game's chat while you talk.

## Install

1. Install **BepInExPack IL2CPP** for Big Walk through your mod manager and launch
   the game once. Use the same profile when you play with XRTranslate.
2. Open the mod manager's **Browse profile folder** action and extract the
   **XRT-Avatar-Plugin-BigWalk** ZIP there, preserving its
   `BepInEx/plugins/XRT.BigWalk/` folder. For a manual BepInEx
   installation, extract it beside the game's executable.
3. In XRTranslate, enable **Big Walk** in **Settings → Plugins**, then open
   **Big Walk** in the sidebar and enable chat output.
4. Join a game. When the page shows **Connected**, start microphone translation.

Development builds are available from the
[Big Walk bridge workflow](https://github.com/NowLoadY/XRT-Avatar-Plugin/actions/workflows/bigwalk.yml).
Open a successful run and download its package artifact.

## Chat

Each completed translation becomes one chat message. **Target only** keeps chat
compact; the bilingual formats include both languages. Microphone translations
are selected by default. Enable system audio only when you intend to relay it.

Messages follow the game's normal chat rules. Long text is shortened to one
readable message. The bridge spaces automatic messages by at least two seconds;
XRTranslate drops stale pending text when you leave or disconnect.

To remove the bridge, close the game and delete
`BepInEx/plugins/XRT.BigWalk/`. Disable **Big Walk** in XRTranslate.

## Connection help

- Launch the modded game profile and join a game before sending.
- Check `BepInEx/LogOutput.txt` for **XRTranslate · Big Walk**. A chat-interface
  error can mean that a game update changed the binding.
- Keep XRTranslate and the game on the same computer. The bridge uses local TCP
  port **38647** and accepts one XRTranslate connection.

This is a community mod bridge. Native multiplayer chat has not yet been
verified in a running Big Walk session.
