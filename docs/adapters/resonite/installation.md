# Resonite

The ResoniteLink bridge connects an Avatar's native control fields to the shared
XRTranslate connector. Resonite owns the Avatar components and their rendering;
the bridge translates control values and result messages.

## Requirements

Use a Resonite session that exposes ResoniteLink. The official enablement rules
require that you host the session, have at least Builder permissions and that
the world's ResoniteLink permissions allow access. Enable it in the Dash's
**Session** tab and copy the local address it displays.

The bridge runs on the same computer as Resonite and XRTranslate, using Python
3.10+ and `websockets`. Install its dependency in a virtual environment:

```sh
python3 -m venv .venv
.venv/bin/python -m pip install -r adapters/resonite/resonitelink/requirements.txt
```

## Install controls

1. Use the Inspector to locate the current Avatar slot. To inspect IDs through
   the API, run the `inspect` command with the local ResoniteLink URL:

   ```sh
   .venv/bin/python adapters/resonite/resonitelink/bridge.py \
     --link ws://127.0.0.1:PORT inspect --slot Root --depth 1
   ```

   Continue inspecting a child slot until the intended Avatar is located.
2. Create the XRT control subtree under that slot:

   ```sh
   .venv/bin/python adapters/resonite/resonitelink/bridge.py \
     --link ws://127.0.0.1:PORT install --parent AVATAR_SLOT_ID
   ```

   The installer creates `XRT/Enabled` with a `ValueField<bool>` and `XRT/Status`
   with a `ValueField<string>`. Its `AvatarDestroyOnDequip` component removes this
   owned control subtree when the Avatar is unequipped. It prints the new XRT
   slot ID. Save the Avatar with the controls off before using the bridge.
3. In the Inspector, set `XRT/Enabled → Value` to control translation. For Avatar
   buttons, add `ButtonValueSet<bool>` to each native TouchButton or UIX Button
   slot; point `TargetValue` at the `Enabled` component's **Value field**. Set one
   button's `SetValue` to true (start), and the other's to false (stop).
4. For captions, use **Create New → Text → Basic** to create a complete native
   text object, position it on the Avatar and note its `TextRenderer` component
   ID. This step supplies the platform's font and material configuration.

## Run

Enable the Avatar connector in XRTranslate, then run:

```sh
.venv/bin/python adapters/resonite/resonitelink/bridge.py \
  --link ws://127.0.0.1:PORT run --slot XRT_SLOT_ID \
  --host ws://127.0.0.1:HOST_PORT/xrt --text TEXT_RENDERER_COMPONENT_ID
```

Enter the pairing token at the local prompt. Omit `--text` to use controls alone.
The bridge resets `Enabled` to false on connection. Press Start after it reports
`off`. `starting` and `active` reflect the host's actual session state. Stop
cancels startup or active translation and clears captions.

TextRenderer output is ordinary synchronized world content. Use a caption object
whose visibility matches the intended audience. The bridge treats translated
text as plain text, clears expired results and restores the previous text settings
when it exits, provided they still match the bridge's last write.

Expiry is performed by the bridge. If that process is forcibly terminated or the
ResoniteLink connection is lost, clear the TextRenderer's Text field in the
Inspector; the component does not provide an independent caption expiry timer.

Stop with Ctrl+C. The bridge cancels its owned host session and resets its control
fields. It exits on loss of either connection; it does not reuse old scene IDs
in another world. ResoniteLink IDs are session-local, so inspect the current
scene again after loading a saved Avatar. To remove the installation, delete its
XRT slot and remove references from any buttons you connected to it.

## Platform boundary

`resonitelink/` contains the runnable bridge for sessions that expose the official
data-model API. Native ProtoFlux WebsocketClient integration is a separate
platform branch: it requires its HandlingUser to be the wearer, access approval
and in-game graph construction. No ProtoFlux package is distributed here.

The bridge follows the current official models and component contracts. A
Resonite client acceptance run is still required for the installed component
graph and rendering. ResoniteLink is currently documented as beta.

- [ResoniteLink activation](https://wiki.resonite.com/ResoniteLink)
- [Official protocol and data-model examples](https://github.com/Yellow-Dog-Man/ResoniteLink)
- [ValueField](https://wiki.resonite.com/Component:ValueField)
- [ButtonValueSet](https://wiki.resonite.com/Component:ButtonValueSet)
- [AvatarDestroyOnDequip](https://wiki.resonite.com/Component:AvatarDestroyOnDequip)
- [TextRenderer](https://wiki.resonite.com/Component:TextRenderer)
- [Native WebsocketClient](https://wiki.resonite.com/Component:WebsocketClient)
