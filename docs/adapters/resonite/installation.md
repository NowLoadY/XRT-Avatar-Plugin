# Set up Resonite

Add translation controls and an editable caption object to your Avatar.
This setup uses Python and a terminal alongside Resonite and XRTranslate.

## Before you start

Have XRTranslate ready with **Avatar translation controls** available and your
audio input and translation provider configured.

You need Python 3.10 or later and a Resonite session that you host, with at least
Builder permission. In the Dash, open **Session**, enable **ResoniteLink**, and
copy its local address. The world's permissions must allow ResoniteLink.
[Official activation instructions](https://wiki.resonite.com/ResoniteLink).

Download **Source code (zip)** from the
[plugin release](https://github.com/NowLoadY/XRT-Avatar-Plugin/releases), extract
it, and open a terminal in the extracted folder containing `adapters`,
`connectors` and `presentation`. Keep those folders together.

Install the Python dependency once:

**Windows PowerShell**

```powershell
py -3 -m venv .venv
./.venv/Scripts/python.exe -m pip install -r adapters/resonite/resonitelink/requirements.txt
```

**Linux**

```sh
python3 -m venv .venv
.venv/bin/python -m pip install -r adapters/resonite/resonitelink/requirements.txt
```

The commands below use the Windows Python path. On Linux, replace
`./.venv/Scripts/python.exe` with `.venv/bin/python`.

## 1. Select your Avatar

Replace `PORT` with the port shown by ResoniteLink:

```text
./.venv/Scripts/python.exe adapters/resonite/resonitelink/bridge.py --link ws://127.0.0.1:PORT inspect --slot Root --depth 1
```

Find the relevant child by its name in the printed results. Copy its `id`, replace
`Root` with that ID, and repeat until your Avatar is shown. Keep the Avatar's ID
for the next step. Use IDs returned by this command; inspect again after loading
an Avatar into a new session.

## 2. Add a caption

1. In Resonite's Inspector, select your Avatar and use **Create New → Text → Basic**
   to make a text template under it. Choose its font, material, color and alignment.
   For a handwritten board, choose the handwritten font here.
2. Inspect your Avatar again. Under the template's components, copy the `id` of
   the component whose type ends in `TextRenderer`.
3. Replace the two IDs below and run:

```text
./.venv/Scripts/python.exe adapters/resonite/resonitelink/bridge.py --link ws://127.0.0.1:PORT install --parent AVATAR_SLOT_ID --preset horizontal --template TEXT_RENDERER_COMPONENT_ID
```

Choose one starting shape:

| Preset | Use |
|---|---|
| `horizontal` | A wide caption |
| `vertical` | A narrow, portrait caption |
| `board` | Text to place on your own board object |

Keep the new **XRT** slot ID printed by the installer. Select the new
**Caption (Horizontal)** object in the Inspector to move, rotate or resize it.
Font, color, alignment and bounds remain editable. Keep the template's font and
material providers with the Avatar; you can clear its displayed text.

For controls and a text source without a new caption, omit both `--preset` and
`--template`. If an XRT installation already exists, use that installation's slot
ID when running the bridge; it will add a missing text source automatically.

## 3. Connect XRTranslate

In XRTranslate, enable Avatar translation controls, select **Resonite**, and enable
**Send captions to the game component**. Copy the displayed connector address.
Then replace `XRT_SLOT_ID` and `HOST_PORT` below:

```text
./.venv/Scripts/python.exe adapters/resonite/resonitelink/bridge.py --link ws://127.0.0.1:PORT run --slot XRT_SLOT_ID --host ws://127.0.0.1:HOST_PORT/xrt
```

Enter XRTranslate's pairing token at the prompt and leave the terminal open.
When **Off** appears, set `XRT/Enabled → Value` to true in the Inspector to start.
Set it to false to stop. **Starting…** and **On** show the current state; wait for
stopping to finish before starting again.

For Avatar buttons, add `ButtonValueSet<bool>` to native TouchButton or UIX Button
slots. Point each `TargetValue` at the **Value field** of `XRT/Enabled`. Use a play
symbol with `SetValue = true` and a stop square with `SetValue = false`.

## Customize your display

Drag `XRT/Text → Value` onto your text object's **Text field** and choose a one-way
ValueCopy. Keep `WriteBack` and rich-text parsing off. You can connect several
objects to this source and choose each object's font, wrapping, color and placement.
The source supplies the current translation without display wrapping.

Captions disappear within eight seconds and clear when translation stops.
They are synchronized world content, so choose objects visible to your intended
audience. Save your Avatar with `Enabled` off and `Text` empty.

## Stop or remove

Press Ctrl+C in the terminal to stop the bridge. If it was forcibly closed or lost
its ResoniteLink connection, clear `XRT/Text → Value` in the Inspector. To remove
the installation, stop the bridge, delete its XRT slot and any Caption objects you
moved elsewhere, and remove references from your buttons or text bindings.

For direct TextRenderer integration, source details and compatibility checks, see
the [developer guide](development.md).
