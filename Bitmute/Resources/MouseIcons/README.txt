Custom mouse cursor assets.

Current state (batch 17): the app sets cursors using built-in Windows system
cursors via CanvasView.ApplyCursorShape (UIElement.ProtectedCursor):

  Transform scale corners  -> SizeNWSE / SizeNESW
  Transform scale edges    -> SizeWE / SizeNS
  Transform move (inside)  -> SizeAll
  Transform rotate ring    -> Cross            (placeholder - no system rotate cursor)
  Guide hover (vertical)   -> SizeWE
  Guide hover (horizontal) -> SizeNS

To replace any of these with a custom image cursor, drop a 32x32 .cur (or .png
to be converted) here named for its purpose, e.g. rotate.cur, and wire it in
CanvasView.ApplyCursorShape by creating an InputDesktopResourceCursor / custom
InputCursor instead of the InputSystemCursor for that case. Rob to provide the
polished rotate cursor art.
