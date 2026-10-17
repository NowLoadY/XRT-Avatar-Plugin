# Big Walk

Send completed microphone translations to native game chat. Use XRTranslate and
Big Walk on the same Windows PC. This is a community mod bridge; multiplayer
acceptance is still pending.

## Install and start

1. Open **r2modman**, select **Big Walk**, and choose a profile. If needed, install
   `r2modman-Setup-….exe` from its [official downloads](https://github.com/ebkr/r2modmanPlus/releases/latest).
2. In **Online**, install **[BepInExPack IL2CPP](https://thunderstore.io/c/big-walk/p/BepInEx/BepInExPack_IL2CPP/)**.
   You only need to do this once per profile.
3. Download the **XRT-Avatar-Plugin-BigWalk** ZIP from the
   [release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases).
   Use **Settings → Import local mod** to import the XRT ZIP without extracting it.
4. Click **Start modded** and join a room.
5. In XRTranslate, select **Game Plugins → Big Walk**, turn on **Enable**, and
   start microphone translation when the page shows **Connected**.

Thunderstore Mod Manager can use the same profile/import workflow. XRT is not yet
published to Thunderstore; local ZIP imports need the loader installed separately.
The package follows the [Thunderstore format](https://wiki.thunderstore.io/mods/creating-a-package)
and the loader used by [BigTalk ConversationStarters](https://github.com/Gogogadgetjustice/bigwalkmods).

No port or bridge settings are needed. The defaults send your microphone's
translations in the target language. Choose a bilingual format or system audio
only if you want to change what is sent.

## Update or remove

Manage XRT in the same mod-manager profile: update/import the newer package,
disable it, or uninstall it. Turn off Big Walk in XRTranslate to stop sending.

<details>
<summary>Manual installation</summary>

With BepInEx 6 IL2CPP already installed, close the game, create
`BepInEx/plugins/XRT.BigWalk/` beside its executable and place `XRT.BigWalk.dll`
from the ZIP there. Remove this folder to uninstall a manual installation.
Do not install the same bridge both manually and through a manager.

</details>

## Connection help

- **Open Big Walk with the XRT bridge:** start the modded profile, not the normal launcher.
- **Join a game:** the bridge is running and waiting for your player.
- A chat-interface error can mean a game update changed the binding. Check
  `BepInEx/LogOutput.txt` for **XRTranslate · Big Walk**.

Long text is shortened to one message. Automatic sends are at least two seconds
apart; pending text is discarded when you leave or disconnect. The bridge uses
local TCP port **38647**. [Development builds and protocol](development.md).
