# Install in Resonite

Add translation controls and an optional caption display to your Avatar.

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
   button's `SetValue` to true (start), and the other's to false (stop). Use a
   play symbol and a stop square on the two button surfaces.
4. For captions, use **Create New → Text → Basic** to create a complete native
   text object, position it on the Avatar and note its `TextRenderer` component
   ID. Keep the object close to your Avatar without covering its face or hands.
   Choose a readable font and adjust its scale to a compact caption width, about
   0.45–0.9 m at normal Avatar scale. The bridge preserves this placement and font.

## Run

In XRTranslate Avatar translation controls, select **Resonite** and enable the
plugin. Enable **Send captions to the game component** when using a text renderer,
then run:

```sh
.venv/bin/python adapters/resonite/resonitelink/bridge.py \
  --link ws://127.0.0.1:PORT run --slot XRT_SLOT_ID \
  --host ws://127.0.0.1:HOST_PORT/xrt --text TEXT_RENDERER_COMPONENT_ID
```

Enter the pairing token at the local prompt. Omit `--text` to use controls alone.
The bridge resets `Enabled` to false on connection. Press Start after it reports
**Off**. **Starting…** and **On** show the current translation state. Stop
cancels startup or active translation and clears captions.

## Captions

The display shows the latest translation in soft white text, with up to three
short lines. Long results end with an ellipsis. A caption disappears within eight
seconds; repeated delivery does not keep it visible. There is no idle label or
connection status above the Avatar.

TextRenderer captions are synchronized world content. Use a caption object whose
visibility matches the intended audience. On exit, the bridge restores the text,
color and rich-text setting it found, preserving edits made while it was running.

Expiry is performed by the bridge. If that process is forcibly terminated or the
ResoniteLink connection is lost, clear the TextRenderer's Text field in the
Inspector; the component does not provide an independent caption expiry timer.

Stop with Ctrl+C. The bridge cancels its owned host session and resets its control
fields. It exits on loss of either connection; it does not reuse old scene IDs
in another world. ResoniteLink IDs are session-local, so inspect the current
scene again after loading a saved Avatar. To remove the installation, delete its
XRT slot and remove references from any buttons you connected to it.

## Compatibility

The bridge uses the official ResoniteLink API, currently documented as beta.
The installed controls and rendering still require an acceptance run in Resonite.
TextRenderer supplies text color, font and placement; this integration uses its
native text surface. Background panels and animation are not added to the Avatar.

- [ResoniteLink activation](https://wiki.resonite.com/ResoniteLink)
- [Official protocol and data-model examples](https://github.com/Yellow-Dog-Man/ResoniteLink)
- [ValueField](https://wiki.resonite.com/Component:ValueField)
- [ButtonValueSet](https://wiki.resonite.com/Component:ButtonValueSet)
- [AvatarDestroyOnDequip](https://wiki.resonite.com/Component:AvatarDestroyOnDequip)
- [TextRenderer](https://wiki.resonite.com/Component:TextRenderer)
- [Color profiles](https://wiki.resonite.com/Type:ColorProfile)
