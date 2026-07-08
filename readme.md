# Bitmute

A raster image editor for game development and general-purpose pixel work, built with .NET MAUI and SkiaSharp. Targets Windows (Linux as a future goal).

![Preview](Docs/preview.png)

## Philosophy

Bitmute is one thing: an image editor that doesn't require an installer, a 6 week onboarding period, or a subscription.  It's self contained, download it, run it, done.

This tool was designed with 3D game development needs in mind, and it does what it does while staying out of your way.

### Alpha State

This is early days for the editor and there are plenty of bugs and quirks to hammer out.  Feel free to log any bugs you find on the github issues tab and I'll try to get to them in a reasonable time.  While I'm not entirely against feature requests, understand the foundation of this tool is intended to capture the core needs of a developer rather than replace more advanced artist workflows that demand the bells and whistles other editors have to offer.

## Documentation

Full user guide: **[Docs/](Docs/README.md)** — workspace tour, tools, layers, filters, keyboard shortcuts, and more.

## Built for game development

- **8/16/32-bit color depth** — height, displacement, and normal-map sources kept at full precision, plus HDR (float) documents
- **Normal Map generator** — tangent-space normals from a grayscale height map, with selectable kernels and tiling (wrap) support
- **Offset with wrap-around** — shift a texture to fix its seam and author seamless, tiling patterns
- **TGA export** (24/32-bit, RLE) for direct use in game engines

## Features

- 27 layer blend modes — the full classic set, including Linear Burn, Vivid/Linear/Pin Light, Hard Mix, Darker/Lighter Color, and Dissolve
- Non-destructive layer styles: drop shadow, inner/outer glow, bevel & emboss, and stroke, with live preview and copy/paste between layers
- Non-destructive layer masks — paint black to hide, white to reveal
- Free Transform (Ctrl+T): scale, rotate, skew, distort, and perspective with a live warped preview
- Full brush engine — hardness, opacity, flow, spacing, stroke smoothing, airbrush, and per-stroke blend modes — shared across clone, heal, dodge/burn, sponge, smudge, color replacement, and more
- 36 filters across Blur, Distort, Noise, Pixelate, Render, Sharpen, Stylize, Video, Generate, and Other — plus adjustments, all with live on-canvas preview
- Editable text layers with WYSIWYG in-canvas editing and a full character panel: leading, kerning, tracking, scale, baseline shift
- Bézier pen and path tools with editable anchors and handles
- Selections as 8-bit coverage masks — feather, anti-alias, add/subtract/intersect modes, floating moves
- Channels panel with per-channel grayscale view — verify alpha mask integrity without exporting
- Import/export: PNG, JPEG, BMP, TGA (with RLE), WebP, GIF (read)
- Open `.bitmute` project format — a ZIP container, inspectable and partially recoverable, round-trips layers, editable text, and selections
- Canvas support up to 8K+ resolution with dirty-rect compositing and undo

## Building

```
dotnet build
```

## License

Apache 2.0
