# Interface and layout previews

Preview images live in `assets/previews/`. Capture the checked-in interfaces
with the shared light theme. Keep labels legible at the README's display width
and use ordinary sample text without personal information.

| Image | Source | Capture state |
|---|---|---|
| `caption-designer.jpg` | `adapters/overte/scripts/designer.html` | Initial page, before the game connection |
| `caption-horizontal.jpg` | `tools/preview-presentation.html` | English sample, Horizontal selected, Hold preview enabled |
| `caption-portrait.jpg` | `tools/preview-presentation.html` | Same sample, Portrait selected, Hold preview enabled |

Serve the repository root with a local static server and open the source pages
in a browser. Take ordinary viewport screenshots as JPEG at the default desktop
size. Replace an image when its visible interface changes, and review its README
caption at the same time.

The homepage uses the Overte designer image. It shows the actual HTML interface,
captured in a browser. The plugin opens this page from the tablet's **Caption
objects** button through `tablet.gotoWebScreen()`.

## Standalone layout tool

The two images below come from `tools/preview-presentation.html`, which uses the
shared caption profiles for layout development. This tool is not included in the
install packages and is not a Unity editor window.

### Horizontal

![Horizontal layout tool](../assets/previews/caption-horizontal.jpg)

### Portrait

![Portrait layout tool](../assets/previews/caption-portrait.jpg)

Unity's current **Avatar Controls** windows use IMGUI for Avatar selection, menu
installation and removal, saving and status feedback. Their implementation is
separate from these browser interfaces. Add Unity screenshots to the homepage
when captured from those windows in Unity.
