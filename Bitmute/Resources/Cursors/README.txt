Custom image cursors, rendered as Skia overlays on the canvas.

Unlike the built-in system cursors set via UIElement.ProtectedCursor
(CanvasView.ApplyCursorShape), these are your own PNG art drawn at the
device-pixel pointer position -- the same technique as the rotate-ring
overlay. WinUI3 has no clean loader for a loose .cur file, so image
cursors are drawn, not set as an OS cursor. (This supersedes the older
Resources\MouseIcons approach for image cursors.)

Drop a cursor here as a PNG:

  Format   32-bit RGBA PNG, art on a transparent background.
  Size     64 x 64 px (2x the 32 px logical size, for crisp HiDPI; the
           draw code scales to the logical size by the display scale).
  Hotspot  Encoded in the filename as _<hotspotX>_<hotspotY>, in the
           PNG's own pixel space, so no companion metadata is needed.
           The hotspot is the point that lands on the true cursor
           position (for an eyedropper, the tip of the dropper).
  Name     lowercase, purpose first.

First cursor needed:

  eyedropper_<hx>_<hy>.png   e.g. eyedropper_2_62.png for a dropper whose
                             tip sits at x=2, y=62 in a 64x64 image
                             (bottom-left tip). Used by the Eyedropper
                             tool and by Alt-held colour pick on the
                             Brush / Pencil / Fill / Gradient tools.

The loader reads the two trailing integers as the hotspot and blits the
image so that point aligns to the pointer.
