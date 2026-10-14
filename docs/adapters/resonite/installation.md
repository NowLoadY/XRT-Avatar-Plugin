# Install in Resonite

Add translation controls and a live text source to your Avatar. Use a supplied
caption shape or connect the source to a text object you have already designed.

## Requirements

Use a Resonite session that exposes ResoniteLink. Host the session, have at least
Builder permissions and enable ResoniteLink in the Dash's **Session** tab. The
world's ResoniteLink permissions must allow access. Copy the local address shown.

The bridge runs on the same computer as Resonite and XRTranslate. Install its
Python 3.10+ dependency in a virtual environment:

```sh
python3 -m venv .venv
.venv/bin/python -m pip install -r adapters/resonite/resonitelink/requirements.txt
```

## Add the source and controls

1. Locate your Avatar's slot in the Inspector. To inspect IDs through the API:

   ```sh
   .venv/bin/python adapters/resonite/resonitelink/bridge.py \
     --link ws://127.0.0.1:PORT inspect --slot Root --depth 1
   ```

   Continue inspecting child slots until the intended Avatar is located.
2. Create the XRT source and controls under that slot:

   ```sh
   .venv/bin/python adapters/resonite/resonitelink/bridge.py \
     --link ws://127.0.0.1:PORT install --parent AVATAR_SLOT_ID
   ```

   The result includes the new XRT slot ID. Its fields are:

   | Field | Use |
   |---|---|
   | `Enabled → Value` | Start or stop translation |
   | `Status → Value` | Current translation status |
   | `Text → Value` | Current plain-text translation |

3. To make Avatar buttons, add `ButtonValueSet<bool>` to native TouchButton or
   UIX Button slots. Point each `TargetValue` at the **Value field** of `Enabled`.
   Use a play symbol with `SetValue = true` and a stop square with
   `SetValue = false`. You can also change the field directly in the Inspector.
4. Save the Avatar with `Enabled` off and `Text` empty.

Existing XRT installations keep their controls. The bridge adds the `Text` field
when it first runs against an installation that does not have one.

## Add a caption object

The three starting shapes are **Horizontal**, **Vertical** and **Board**.
Vertical is a narrow portrait layout. Board provides text that you can place on
your own board object. All three remain ordinary editable native text objects.

1. Under your Avatar, use **Create New → Text → Basic** to make a working text
   template. Choose its font, material, color and alignment in the Inspector.
   For a handwritten board, choose the handwritten font here.
2. Note that object's `TextRenderer` component ID and include it when installing:

   ```sh
   .venv/bin/python adapters/resonite/resonitelink/bridge.py \
     --link ws://127.0.0.1:PORT install --parent AVATAR_SLOT_ID \
     --preset horizontal --template TEXT_RENDERER_COMPONENT_ID
   ```

   Replace `horizontal` with `vertical` or `board` for the other shapes. Use this
   command in place of the source-only installation above.
3. Select the new **Caption** object in the Inspector. Move, rotate and scale it
   where you want, then adjust font, color, size, alignment or bounds directly.
   Its text is already connected to `XRT/Text` through a native `ValueCopy<string>`.
   Keep rich-text parsing off for translated content.

The preset reuses the template's font and material providers. Keep those providers
with the Avatar; their references remain editable. The template's displayed text
can be cleared. The bridge changes the text source, so it does not overwrite your
caption styling or position when a new translation arrives.

## Use your own object

Drag `XRT/Text → Value` onto a text object's **Text field** in the Inspector and
choose a one-way ValueCopy. Keep `WriteBack` off. You can connect several objects
to the same source, or read the Value field from ProtoFlux. Choose fonts, wrapping,
colors and placement on each object separately.

The source contains the current plain text without display wrapping.
Your object determines how to display it. Keep its rich-text parsing
off when binding translated text.

For an existing unbound TextRenderer, the bridge also supports the direct
`--text TEXT_RENDERER_COMPONENT_ID` option. It preserves the object's font,
material, color, size and placement. Do not also bind that same Text field with a
ValueCopy. On exit, the bridge restores the previous text and rich-text setting
if they have not been edited while it was running.

## Run

In XRTranslate, enable Avatar translation controls, select **Resonite**, and enable
**Send captions to the game component**. Then run:

```sh
.venv/bin/python adapters/resonite/resonitelink/bridge.py \
  --link ws://127.0.0.1:PORT run --slot XRT_SLOT_ID \
  --host ws://127.0.0.1:HOST_PORT/xrt
```

Enter the pairing token at the local prompt. The bridge resets `Enabled` to false
on connection. Press Start after **Off** appears. **Starting…** and **On** show
the current translation state. Stop clears the text source and cancels translation.
Wait for stopping to finish before starting again.

The source is empty between captions. Each result disappears within eight seconds,
and repeated delivery does not keep it visible. Captions are synchronized world
content: use objects whose visibility matches the intended audience.

Stop the bridge with Ctrl+C. It stops translation and clears its text source.
If the process is forcibly terminated or ResoniteLink is lost, clear `XRT/Text →
Value` in the Inspector, and the Text field of any directly connected `--text`
object. The bridge provides expiry while it is running.

ResoniteLink IDs belong to the current session. Inspect the current scene again
after loading a saved Avatar. To remove the installation, stop the bridge, delete
its XRT slot and any caption objects you moved elsewhere, then remove references
from your buttons or text bindings. Created XRT and Caption slots also carry
`AvatarDestroyOnDequip` for Avatar unequip cleanup.

## Compatibility

The bridge uses the official ResoniteLink API, currently documented as beta.
Native object installation, field binding and rendering still need an acceptance
run in Resonite. Presets use native TextRenderer objects; add your own board or
background object when desired.

- [ResoniteLink activation](https://wiki.resonite.com/ResoniteLink)
- [Official data model and examples](https://github.com/Yellow-Dog-Man/ResoniteLink)
- [ValueField](https://wiki.resonite.com/Component:ValueField)
- [ValueCopy](https://wiki.resonite.com/Component:ValueCopy)
- [ButtonValueSet](https://wiki.resonite.com/Component:ButtonValueSet)
- [AvatarDestroyOnDequip](https://wiki.resonite.com/Component:AvatarDestroyOnDequip)
- [TextRenderer](https://wiki.resonite.com/Component:TextRenderer)
